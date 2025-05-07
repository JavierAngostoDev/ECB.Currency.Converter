using ECB.Currency.Converter.Core.Common;
using ECB.Currency.Converter.Core.Domain;
using FluentAssertions;

namespace ECB.Currency.Converter.Tests.Domain
{
    public class CurrencyEntityTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData(" ")]
        [InlineData("US")]
        [InlineData("EURO")]
        [InlineData("usd1")]
        [InlineData("us!")]
        public void Create_With_InvalidIsoCode_Should_Return_ValidationError(string input)
        {
            Result<CurrencyEntity> result = CurrencyEntity.Create(input);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(CurrencyEntity.ValidationError);
        }

        [Theory]
        [InlineData("USD")]
        [InlineData("EUR")]
        [InlineData("JPY")]
        public void Create_With_ValidIsoCode_Should_Return_Success(string input)
        {
            Result<CurrencyEntity> result = CurrencyEntity.Create(input);

            result.IsSuccess.Should().BeTrue();
            result.Value.Code.Should().Be(input.ToUpperInvariant());
        }

        [Fact]
        public void Equality_Should_Work_For_Implicit_Conversions()
        {
            CurrencyEntity a = "usd";
            CurrencyEntity b = "USD";

            a.Should().Be(b);
            (a == b).Should().BeTrue();
        }

        [Fact]
        public void Implicit_CurrencyEntity_To_String_Should_Convert()
        {
            CurrencyEntity currency = CurrencyEntity.Create("JPY").Value;

            string result = currency;

            result.Should().Be("JPY");
        }

        [Fact]
        public void Implicit_String_To_CurrencyEntity_Should_Convert()
        {
            CurrencyEntity currency = "usd";

            currency.Code.Should().Be("USD");
        }

        [Fact]
        public void ToString_Should_Return_Code()
        {
            CurrencyEntity currency = CurrencyEntity.Create("GBP").Value;

            currency.ToString().Should().Be("GBP");
        }

        [Fact]
        public void ValidationError_Should_Have_Expected_Code_And_Message()
        {
            Error error = CurrencyEntity.ValidationError;

            error.Code.Should().Be("Currency.Validation");
            error.Message.Should().Contain("Invalid currency ISO code");
        }
    }
}