using ECB.Currency.Converter.Core.Common;
using ECB.Currency.Converter.Core.Domain;
using ECB.Currency.Converter.Core.Features.GetExchangeRate;
using ECB.Currency.Converter.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace ECB.Currency.Converter.Tests.Features.GetExchangeRate
{
    public class GetExchangeRateQueryHandlerTests
    {
        private static readonly CurrencyEntity Eur = "EUR";
        private static readonly CurrencyEntity Gbp = "GBP";
        private static readonly CurrencyEntity Jpy = "JPY";
        private static readonly CurrencyEntity Usd = "USD";

        private readonly GetExchangeRateQueryHandler _handler;
        private readonly Mock<IExchangeRateProvider> _mockRateProvider;

        public GetExchangeRateQueryHandlerTests()
        {
            _mockRateProvider = new Mock<IExchangeRateProvider>();
            _handler = new GetExchangeRateQueryHandler(_mockRateProvider.Object);
        }

        [Fact]
        public void Constructor_WhenRateProviderIsNull_ThrowsArgumentNullException()
        {
            IExchangeRateProvider? nullProvider = null;
            Action act = () => new GetExchangeRateQueryHandler(nullProvider);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WhenRateProviderIsValid_DoesNotThrow()
        {
            _handler.Should().NotBeNull();
            GetExchangeRateQueryHandler instance = new GetExchangeRateQueryHandler(_mockRateProvider.Object);
            instance.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_WhenConvertingBetweenTwoNonEurCurrencies_CalculatesCorrectCrossRate()
        {
            GetExchangeRateQuery query = new GetExchangeRateQuery(Usd, Gbp);
            DateTimeOffset rateTimestamp = DateTimeOffset.UtcNow.AddHours(-3);
            decimal usdRateVsEur = 1.10m;
            decimal gbpRateVsEur = 0.90m;

            List<ExchangeRateEntity> rates = new List<ExchangeRateEntity>
            {
                ExchangeRateEntity.Create(Eur, Usd, usdRateVsEur, rateTimestamp).Value,
                ExchangeRateEntity.Create(Eur, Gbp, gbpRateVsEur, rateTimestamp).Value,
                ExchangeRateEntity.Create(Eur, Jpy, 130.0m, rateTimestamp).Value
            };

            _mockRateProvider.Setup(p => p.GetLatestRatesAsync())
                             .ReturnsAsync(Result<IEnumerable<ExchangeRateEntity>>.Success(rates));

            Result<ExchangeRateEntity> result = await _handler.Handle(query);

            decimal expectedCrossRate = gbpRateVsEur / usdRateVsEur;

            result.IsSuccess.Should().BeTrue();
            result.Value.BaseCurrency.Should().Be(Usd);
            result.Value.QuoteCurrency.Should().Be(Gbp);
            result.Value.Rate.Should().Be(expectedCrossRate);
            result.Value.Timestamp.Should().Be(rateTimestamp);

            _mockRateProvider.Verify(p => p.GetLatestRatesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenConvertingFromEurToOtherCurrency_CalculatesCorrectly()
        {
            GetExchangeRateQuery query = new GetExchangeRateQuery(Eur, Usd);
            DateTimeOffset rateTimestamp = DateTimeOffset.UtcNow.AddMinutes(-30);
            decimal usdRateVsEur = 1.15m;

            List<ExchangeRateEntity> rates = new List<ExchangeRateEntity>
            {
                ExchangeRateEntity.Create(Eur, Usd, usdRateVsEur, rateTimestamp).Value,
                ExchangeRateEntity.Create(Eur, Gbp, 0.88m, rateTimestamp).Value
            };

            _mockRateProvider.Setup(p => p.GetLatestRatesAsync())
                             .ReturnsAsync(Result<IEnumerable<ExchangeRateEntity>>.Success(rates));

            Result<ExchangeRateEntity> result = await _handler.Handle(query);

            result.IsSuccess.Should().BeTrue();
            result.Value.BaseCurrency.Should().Be(Eur);
            result.Value.QuoteCurrency.Should().Be(Usd);
            result.Value.Rate.Should().Be(usdRateVsEur);
            result.Value.Timestamp.Should().Be(rateTimestamp);

            _mockRateProvider.Verify(p => p.GetLatestRatesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenConvertingFromOtherCurrencyToEur_CalculatesCorrectly()
        {
            GetExchangeRateQuery query = new GetExchangeRateQuery(Usd, Eur);
            DateTimeOffset rateTimestamp = DateTimeOffset.UtcNow.AddMinutes(-45);
            decimal usdRateVsEur = 1.20m;

            List<ExchangeRateEntity> rates = new List<ExchangeRateEntity>
            {
                ExchangeRateEntity.Create(Eur, Usd, usdRateVsEur, rateTimestamp).Value,
                ExchangeRateEntity.Create(Eur, Gbp, 0.9m, rateTimestamp).Value
            };

            _mockRateProvider.Setup(p => p.GetLatestRatesAsync())
                             .ReturnsAsync(Result<IEnumerable<ExchangeRateEntity>>.Success(rates));

            Result<ExchangeRateEntity> result = await _handler.Handle(query);

            result.IsSuccess.Should().BeTrue();
            result.Value.BaseCurrency.Should().Be(Usd);
            result.Value.QuoteCurrency.Should().Be(Eur);
            result.Value.Rate.Should().Be(1.0m / usdRateVsEur);
            result.Value.Timestamp.Should().Be(rateTimestamp);

            _mockRateProvider.Verify(p => p.GetLatestRatesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenFromAndToCurrenciesAreTheSame_ReturnsRateOfOne()
        {
            GetExchangeRateQuery query = new GetExchangeRateQuery(Usd, Usd);
            DateTimeOffset expectedTimestamp = DateTimeOffset.UtcNow.AddMinutes(-5);

            _mockRateProvider.Setup(p => p.GetLastUpdateTimestamp()).Returns(expectedTimestamp);

            Result<ExchangeRateEntity> result = await _handler.Handle(query);

            result.IsSuccess.Should().BeTrue();
            result.Value.BaseCurrency.Should().Be(Usd);
            result.Value.QuoteCurrency.Should().Be(Usd);
            result.Value.Rate.Should().Be(1.0m);
            result.Value.Timestamp.Should().Be(expectedTimestamp);

            _mockRateProvider.Verify(p => p.GetLastUpdateTimestamp(), Times.Once);
            _mockRateProvider.Verify(p => p.GetLatestRatesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenFromAndToCurrenciesAreTheSameAndTimestampIsNull_ReturnsRateOfOneWithCurrentTimestamp()
        {
            GetExchangeRateQuery query = new GetExchangeRateQuery(Gbp, Gbp);
            DateTimeOffset beforeExecution = DateTimeOffset.UtcNow;

            _mockRateProvider.Setup(p => p.GetLastUpdateTimestamp()).Returns((DateTimeOffset?)null);

            Result<ExchangeRateEntity> result = await _handler.Handle(query);
            DateTimeOffset afterExecution = DateTimeOffset.UtcNow;

            result.IsSuccess.Should().BeTrue();
            result.Value.BaseCurrency.Should().Be(Gbp);
            result.Value.QuoteCurrency.Should().Be(Gbp);
            result.Value.Rate.Should().Be(1.0m);
            result.Value.Timestamp.Should().BeOnOrAfter(beforeExecution);
            result.Value.Timestamp.Should().BeOnOrBefore(afterExecution);

            _mockRateProvider.Verify(p => p.GetLastUpdateTimestamp(), Times.Once);
            _mockRateProvider.Verify(p => p.GetLatestRatesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenFromCurrencyIsNotFound_ReturnsRateNotFoundError()
        {
            GetExchangeRateQuery query = new GetExchangeRateQuery(Jpy, Usd);
            DateTimeOffset rateTimestamp = DateTimeOffset.UtcNow.AddHours(-1);

            List<ExchangeRateEntity> rates = new List<ExchangeRateEntity>
            {
                ExchangeRateEntity.Create(Eur, Usd, 1.1m, rateTimestamp).Value
            };

            _mockRateProvider.Setup(p => p.GetLatestRatesAsync())
                             .ReturnsAsync(Result<IEnumerable<ExchangeRateEntity>>.Success(rates));

            Result<ExchangeRateEntity> result = await _handler.Handle(query);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().NotBeNull();
            result.Error.Code.Should().Be(GetExchangeRateQueryHandler.RateNotFound.Code);
            result.Error.Message.Should().Contain(Jpy.Code);

            _mockRateProvider.Verify(p => p.GetLatestRatesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenFromCurrencyRateIsZero_ReturnsZeroRateError()
        {
            // Arrange
            GetExchangeRateQuery query = new(Usd, Gbp);
            DateTimeOffset rateTimestamp = DateTimeOffset.UtcNow;

            List<ExchangeRateEntity> rates =
            [
                ExchangeRateEntity.Create(Eur, Usd, 0.0m, rateTimestamp).Value,
                ExchangeRateEntity.Create(Eur, Gbp, 0.85m, rateTimestamp).Value
            ];

                _mockRateProvider.Setup(p => p.GetLatestRatesAsync())
                                 .ReturnsAsync(Result<IEnumerable<ExchangeRateEntity>>.Success(rates));

                // Act
                Result<ExchangeRateEntity> result = await _handler.Handle(query);

                // Assert
                result.IsSuccess.Should().BeFalse();
                result.Error.Should().NotBeNull();
                result.Error.Code.Should().Be("GetExchangeRate.InvalidStoredRate");
                result.Error.Message.Should().Contain(Usd.Code);

                _mockRateProvider.Verify(p => p.GetLatestRatesAsync(), Times.Once);
            }

        }
}