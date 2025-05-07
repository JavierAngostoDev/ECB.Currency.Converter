using ECB.Currency.Converter.Client.Core.Common;
using ECB.Currency.Converter.Client.Core.Domain;
using FluentAssertions;

namespace ECB.Currency.Converter.Tests.Domain
{
    public class ExchangeRateEntityTests
    {
        private static readonly CurrencyEntity EUR = "EUR";
        private static readonly DateTimeOffset Timestamp = DateTimeOffset.UtcNow;
        private static readonly CurrencyEntity USD = "USD";

        [Fact]
        public void Create_Should_Return_Successful_Result_With_Expected_Properties()
        {
            Result<ExchangeRateEntity> result = ExchangeRateEntity.Create(USD, EUR, 1.15m, Timestamp);

            result.IsSuccess.Should().BeTrue();

            ExchangeRateEntity entity = result.Value;
            entity.BaseCurrency.Should().Be(USD);
            entity.QuoteCurrency.Should().Be(EUR);
            entity.Rate.Should().Be(1.15m);
            entity.Timestamp.Should().Be(Timestamp);
        }

        [Fact]
        public void Equality_Should_Work_For_ExchangeRateEntity()
        {
            ExchangeRateEntity a = ExchangeRateEntity.Create(USD, EUR, 1.25m, Timestamp).Value;
            ExchangeRateEntity b = ExchangeRateEntity.Create(USD, EUR, 1.25m, Timestamp).Value;

            a.Should().Be(b);
            (a == b).Should().BeTrue();
        }

        [Fact]
        public void Inequality_Should_Work_For_ExchangeRateEntity()
        {
            ExchangeRateEntity a = ExchangeRateEntity.Create(USD, EUR, 1.25m, Timestamp).Value;
            ExchangeRateEntity b = ExchangeRateEntity.Create(EUR, USD, 0.8m, Timestamp).Value;

            a.Should().NotBe(b);
            (a != b).Should().BeTrue();
        }

        [Fact]
        public void Invert_Should_Return_Inverted_Rate()
        {
            ExchangeRateEntity original = ExchangeRateEntity.Create(USD, EUR, 1.25m, Timestamp).Value;

            Result<ExchangeRateEntity> invertedResult = original.Invert();

            invertedResult.IsSuccess.Should().BeTrue();

            ExchangeRateEntity inverted = invertedResult.Value;
            inverted.BaseCurrency.Should().Be(EUR);
            inverted.QuoteCurrency.Should().Be(USD);
            inverted.Rate.Should().BeApproximately(0.8m, 0.0001m);
            inverted.Timestamp.Should().Be(Timestamp);
        }

        [Fact]
        public void Invert_When_Rate_Is_Zero_Should_Return_Error()
        {
            ExchangeRateEntity entity = ExchangeRateEntity.Create(USD, EUR, 0.0m, Timestamp).Value;

            Result<ExchangeRateEntity> inverted = entity.Invert();

            inverted.IsFailure.Should().BeTrue();
            inverted.Error.Code.Should().Be("ExchangeRate.ZeroRate");
            inverted.Error.Message.Should().Contain("invert");
        }

        [Fact]
        public void NonPositiveRateError_Should_Have_Expected_Values()
        {
            Error error = ExchangeRateEntity.NonPositiveRateError;

            error.Code.Should().Be("ExchangeRate.Validation");
            error.Message.Should().Contain("positive");
        }

        [Fact]
        public void SameCurrencyError_Should_Have_Expected_Values()
        {
            Error error = ExchangeRateEntity.SameCurrencyError;

            error.Code.Should().Be("ExchangeRate.Validation");
            error.Message.Should().Contain("Base and quote");
        }

        [Fact]
        public void ToString_Should_Return_HumanReadable_Format()
        {
            Result<ExchangeRateEntity> result = ExchangeRateEntity.Create(USD, EUR, 1.25m, Timestamp);
            string text = result.Value.ToString();

            text.Should().Contain("1 USD = 1.25 EUR");
            text.Should().Contain(Timestamp.ToString("O"));
        }
    }
}