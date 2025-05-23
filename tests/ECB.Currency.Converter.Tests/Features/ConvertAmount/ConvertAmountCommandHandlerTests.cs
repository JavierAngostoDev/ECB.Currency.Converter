﻿using ECB.Currency.Converter.Client.Core.Common;
using ECB.Currency.Converter.Client.Core.Domain;
using ECB.Currency.Converter.Client.Core.Features.ConvertAmount;
using ECB.Currency.Converter.Client.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace ECB.Currency.Converter.Tests.Features.ConvertAmount
{
    public class ConvertAmountCommandHandlerTests
    {
        private readonly Mock<IExchangeRateProvider> _mockRateProvider;
        private readonly ConvertAmountCommandHandler _handler;

        private readonly CurrencyEntity _eur = CurrencyEntity.Create("EUR").Value;
        private readonly CurrencyEntity _usd = CurrencyEntity.Create("USD").Value;
        private readonly CurrencyEntity _jpy = CurrencyEntity.Create("JPY").Value;

        public ConvertAmountCommandHandlerTests()
        {
            _mockRateProvider = new Mock<IExchangeRateProvider>();
            _handler = new ConvertAmountCommandHandler(_mockRateProvider.Object);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenProviderIsNull()
        {
            Action act = () => new ConvertAmountCommandHandler(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("rateProvider");
        }

        [Fact]
        public async Task Handle_ShouldReturnZeroAmount_WhenSourceAmountIsZero()
        {
            Result<MoneyEntity> sourceMoneyResult = MoneyEntity.Create(0m, _usd);
            sourceMoneyResult.IsSuccess.Should().BeTrue();
            ConvertAmountCommand command = new(sourceMoneyResult.Value, _eur);
            Result<MoneyEntity> expectedResult = MoneyEntity.Create(0m, _eur);

            Result<MoneyEntity> result = await _handler.Handle(command);

            result.IsSuccess.Should().BeTrue();
            result.Value.Amount.Should().Be(0m);
            result.Value.Currency.Should().Be(_eur);
            result.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task Handle_ShouldReturnSourceMoney_WhenCurrenciesAreSame()
        {
            Result<MoneyEntity> sourceMoneyResult = MoneyEntity.Create(100m, _usd);
            sourceMoneyResult.IsSuccess.Should().BeTrue();
            ConvertAmountCommand command = new(sourceMoneyResult.Value, _usd);

            Result<MoneyEntity> result = await _handler.Handle(command);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(sourceMoneyResult.Value);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenGetExchangeRateFailsDueToProviderError()
        {
            Result<MoneyEntity> sourceMoneyResult = MoneyEntity.Create(100m, _usd);
            sourceMoneyResult.IsSuccess.Should().BeTrue();
            ConvertAmountCommand command = new(sourceMoneyResult.Value, _eur);
            Error providerError = Error.Create("Provider.Network", "Network error fetching rates.");

            _mockRateProvider
                .Setup(p => p.GetLatestRatesAsync())
                .ReturnsAsync(Result<IEnumerable<ExchangeRateEntity>>.Failure(providerError));

            Result<MoneyEntity> result = await _handler.Handle(command);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(ConvertAmountCommandHandler.GetRateFailedError.Code);
            result.Error.Message.Should().Contain(ConvertAmountCommandHandler.GetRateFailedError.Message);
            result.Error.Message.Should().Contain(providerError.Message);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenGetExchangeRateFailsDueToMissingRate()
        {
            Result<MoneyEntity> sourceMoneyResult = MoneyEntity.Create(100m, _usd);
            sourceMoneyResult.IsSuccess.Should().BeTrue();
            ConvertAmountCommand command = new(sourceMoneyResult.Value, _jpy);
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;

            List<ExchangeRateEntity> ratesList =
            [
                ExchangeRateEntity.Create(_eur, _usd, 1.1m, timestamp).Value
            ];

            _mockRateProvider
               .Setup(p => p.GetLatestRatesAsync())
               .ReturnsAsync(Result<IEnumerable<ExchangeRateEntity>>.Success(ratesList));
            _mockRateProvider
               .Setup(p => p.GetLastUpdateTimestamp())
               .Returns(timestamp);

            Result<MoneyEntity> result = await _handler.Handle(command);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(ConvertAmountCommandHandler.GetRateFailedError.Code);
            result.Error.Message.Should().Contain(ConvertAmountCommandHandler.GetRateFailedError.Message);
            result.Error.Message.Should().Contain("JPY");
        }

        [Fact]
        public async Task Handle_ShouldReturnConvertedAmount_WhenGetExchangeRateSucceeds()
        {
            // Arrange
            Result<MoneyEntity> sourceMoneyResult = MoneyEntity.Create(100m, _usd);
            sourceMoneyResult.IsSuccess.Should().BeTrue();
            CurrencyEntity targetCurrency = _jpy;
            ConvertAmountCommand command = new(sourceMoneyResult.Value, targetCurrency);
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;

            List<ExchangeRateEntity> ratesList =
            [
                ExchangeRateEntity.Create(_eur, _usd, 1.1m, timestamp).Value,
        ExchangeRateEntity.Create(_eur, _jpy, 130m, timestamp).Value
            ];

            _mockRateProvider
                .Setup(p => p.GetLatestRatesAsync())
                .ReturnsAsync(Result<IEnumerable<ExchangeRateEntity>>.Success(ratesList));
            _mockRateProvider
               .Setup(p => p.GetLastUpdateTimestamp())
               .Returns(timestamp);

            decimal rawRate = 130m / 1.1m;
            decimal rawAmount = 100m * rawRate;
            decimal expectedAmount = Math.Round(rawAmount, 2, MidpointRounding.AwayFromZero);

            // Act
            Result<MoneyEntity> result = await _handler.Handle(command);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Currency.Should().Be(targetCurrency);
            result.Value.Amount.Should().Be(expectedAmount);
        }

        [Fact]
        public async Task Handle_ShouldHandleConversionToEurCorrectly()
        {
            // Arrange
            Result<MoneyEntity> sourceMoneyResult = MoneyEntity.Create(110m, _usd);
            sourceMoneyResult.IsSuccess.Should().BeTrue();
            CurrencyEntity targetCurrency = _eur;
            ConvertAmountCommand command = new(sourceMoneyResult.Value, targetCurrency);
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;

            List<ExchangeRateEntity> ratesList =
            [
                ExchangeRateEntity.Create(_eur, _usd, 1.1m, timestamp).Value
            ];

            _mockRateProvider
                .Setup(p => p.GetLatestRatesAsync())
                .ReturnsAsync(Result<IEnumerable<ExchangeRateEntity>>.Success(ratesList));
            _mockRateProvider
                .Setup(p => p.GetLastUpdateTimestamp())
                .Returns(timestamp);

            decimal rawRate = 1m / 1.1m;
            decimal rawAmount = 110m * rawRate;
            decimal expectedAmount = Math.Round(rawAmount, 2, MidpointRounding.AwayFromZero);

            // Act
            Result<MoneyEntity> result = await _handler.Handle(command);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Currency.Should().Be(targetCurrency);
            result.Value.Amount.Should().Be(expectedAmount); 
        }

        [Fact]
        public async Task Handle_ShouldHandleConversionFromEurCorrectly()
        {
            Result<MoneyEntity> sourceMoneyResult = MoneyEntity.Create(100m, _eur);
            sourceMoneyResult.IsSuccess.Should().BeTrue();
            CurrencyEntity targetCurrency = _jpy;
            ConvertAmountCommand command = new(sourceMoneyResult.Value, targetCurrency);
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;

            List<ExchangeRateEntity> ratesList =
            [
                ExchangeRateEntity.Create(_eur, _jpy, 130m, timestamp).Value
            ];

            _mockRateProvider
               .Setup(p => p.GetLatestRatesAsync())
               .ReturnsAsync(Result<IEnumerable<ExchangeRateEntity>>.Success(ratesList));
            _mockRateProvider
              .Setup(p => p.GetLastUpdateTimestamp())
              .Returns(timestamp);

            decimal expectedRate = 130m;
            decimal expectedAmount = 100m * expectedRate;
            Result<MoneyEntity> expectedMoneyResult = MoneyEntity.Create(expectedAmount, targetCurrency);
            expectedMoneyResult.IsSuccess.Should().BeTrue();

            Result<MoneyEntity> result = await _handler.Handle(command);

            result.IsSuccess.Should().BeTrue();
            result.Value.Currency.Should().Be(targetCurrency);
            result.Value.Amount.Should().BeApproximately(13000m, 0.0001m);
        }
    }
}