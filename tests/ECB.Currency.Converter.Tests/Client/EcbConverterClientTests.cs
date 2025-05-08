using ECB.Currency.Converter.Client;
using ECB.Currency.Converter.Client.Core.Common;
using ECB.Currency.Converter.Client.Core.Domain;
using ECB.Currency.Converter.Client.Core.Interfaces;
using FluentAssertions;
using Moq;

namespace ECB.Currency.Converter.Tests.Client
{
    public class EcbConverterClientTests
    {
        private static readonly CurrencyEntity EUR = "EUR";
        private static readonly DateTimeOffset Timestamp = DateTimeOffset.UtcNow;

        [Fact]
        public void Constructor_WithNullRateProvider_Should_Throw()
        {
            Action act = () => _ = new EcbConverterClient(null!);

            act.Should().Throw<ArgumentNullException>()
               .WithMessage("*rateProvider*");
        }

        [Fact]
        public void Constructor_WithRateProvider_Should_Initialize()
        {
            IExchangeRateProvider provider = Mock.Of<IExchangeRateProvider>();
            EcbConverterClient client = new EcbConverterClient(provider);

            client.Should().NotBeNull();
        }

        [Fact]
        public async Task ConvertAsync_Should_Fail_When_FromCurrency_IsInvalid()
        {
            EcbConverterClient client = new EcbConverterClient(Mock.Of<IExchangeRateProvider>());

            Result<MoneyEntity> result = await client.ConvertAsync("XX", "USD", 100m);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Currency.Validation");
        }

        [Fact]
        public async Task ConvertAsync_Should_Fail_When_ToCurrency_IsInvalid()
        {
            EcbConverterClient client = new EcbConverterClient(Mock.Of<IExchangeRateProvider>());

            Result<MoneyEntity> result = await client.ConvertAsync("USD", "123", 100m);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Currency.Validation");
        }

        [Fact]
        public async Task ConvertAsync_Should_Succeed_When_AllInputsAreValid()
        {
            Mock<IExchangeRateProvider> providerMock = new Mock<IExchangeRateProvider>();
            providerMock.Setup(p => p.GetLatestRatesAsync()).ReturnsAsync(
                Result<IEnumerable<ExchangeRateEntity>>.Success(new[]
                {
                    ExchangeRateEntity.Create("EUR", "USD", 1.2m, Timestamp).Value,
                    ExchangeRateEntity.Create("EUR", "EUR", 1.0m, Timestamp).Value
                })
            );

            EcbConverterClient client = new EcbConverterClient(providerMock.Object);

            Result<MoneyEntity> result = await client.ConvertAsync("USD", "EUR", 100m);

            result.IsSuccess.Should().BeTrue();
            result.Value.Currency.Should().Be(EUR);
        }

        [Fact]
        public void Dispose_Should_Not_Throw_When_Called_Multiple_Times()
        {
            EcbConverterClient client = new EcbConverterClient();

            client.Dispose();
            client.Dispose(); 
        }

        [Fact]
        public async Task GetExchangeRateAsync_Should_Fail_When_FromCurrency_Invalid()
        {
            EcbConverterClient client = new EcbConverterClient(Mock.Of<IExchangeRateProvider>());

            Result<ExchangeRateEntity> result = await client.GetExchangeRateAsync("!@#", "EUR");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Currency.Validation");
        }

        [Fact]
        public async Task GetExchangeRateAsync_Should_Succeed_When_Valid()
        {
            Mock<IExchangeRateProvider> providerMock = new Mock<IExchangeRateProvider>();
            providerMock.Setup(p => p.GetLatestRatesAsync()).ReturnsAsync(
                Result<IEnumerable<ExchangeRateEntity>>.Success(new[]
                {
                    ExchangeRateEntity.Create("EUR", "USD", 1.1m, Timestamp).Value,
                    ExchangeRateEntity.Create("EUR", "EUR", 1.0m, Timestamp).Value
                })
            );

            EcbConverterClient client = new EcbConverterClient(providerMock.Object);

            Result<ExchangeRateEntity> result = await client.GetExchangeRateAsync("USD", "EUR");

            result.IsSuccess.Should().BeTrue();
            result.Value.Rate.Should().BeApproximately(1.0m / 1.1m, 0.0001m);
        }

        [Fact]
        public void GetLastRateUpdateTimestamp_Should_Delegate_ToProvider()
        {
            DateTimeOffset expected = DateTimeOffset.UtcNow.AddMinutes(-30);
            Mock<IExchangeRateProvider> mock = new Mock<IExchangeRateProvider>();
            mock.Setup(p => p.GetLastUpdateTimestamp()).Returns(expected);

            EcbConverterClient client = new EcbConverterClient(mock.Object);
            DateTimeOffset? result = client.GetLastRateUpdateTimestamp();

            result.Should().Be(expected);
        }

        [Fact]
        public async Task GetExchangeRateAsync_Should_Fail_When_ToCurrency_IsInvalid()
        {
            // Arrange
            EcbConverterClient client = new EcbConverterClient(Mock.Of<IExchangeRateProvider>());

            // Act
            Result<ExchangeRateEntity> result = await client.GetExchangeRateAsync("EUR", "INVALID");

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Currency.Validation");
        }

    }
}