using ECB.Currency.Converter.Core.Common;
using ECB.Currency.Converter.Core.Domain;
using FluentAssertions;

namespace ECB.Currency.Converter.Tests.Domain
{
    public class MoneyEntityTests
    {
        private static readonly CurrencyEntity EUR = "EUR";
        private static readonly CurrencyEntity USD = "USD";

        [Fact]
        public void Add_With_Different_Currencies_Should_Return_Error()
        {
            MoneyEntity a = new MoneyEntity(10m, EUR);
            MoneyEntity b = new MoneyEntity(15m, USD);

            Result<MoneyEntity> result = MoneyEntity.Add(a, b);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Money.Mismatch");
            result.Error.Message.Should().Contain("different currencies");
        }

        [Fact]
        public void Add_With_Same_Currency_Should_Return_Summed_Money()
        {
            MoneyEntity a = new MoneyEntity(30.75m, USD);
            MoneyEntity b = new MoneyEntity(19.25m, USD);

            Result<MoneyEntity> result = MoneyEntity.Add(a, b);

            result.IsSuccess.Should().BeTrue();

            MoneyEntity sum = result.Value;
            sum.Amount.Should().Be(50.00m);
            sum.Currency.Should().Be(USD);
        }

        [Fact]
        public void Create_Should_Return_Successful_Result()
        {
            Result<MoneyEntity> result = MoneyEntity.Create(100.50m, EUR);

            result.IsSuccess.Should().BeTrue();

            MoneyEntity money = result.Value;
            money.Amount.Should().Be(100.50m);
            money.Currency.Should().Be(EUR);
        }

        [Fact]
        public void Equality_Should_Work_For_Identical_Instances()
        {
            MoneyEntity m1 = new MoneyEntity(100m, EUR);
            MoneyEntity m2 = new MoneyEntity(100m, EUR);

            m1.Should().Be(m2);
            (m1 == m2).Should().BeTrue();
        }

        [Fact]
        public void Inequality_Should_Work_For_Different_Instances()
        {
            MoneyEntity m1 = new MoneyEntity(100m, EUR);
            MoneyEntity m2 = new MoneyEntity(100m, USD);
            MoneyEntity m3 = new MoneyEntity(99.99m, EUR);

            m1.Should().NotBe(m2);
            m1.Should().NotBe(m3);
        }

        [Fact]
        public void ToString_Should_Use_Two_Decimals_And_Display_Currency()
        {
            MoneyEntity money = new MoneyEntity(123.456m, EUR);

            string output = money.ToString();

            output.Should().Be("123.46 EUR");
        }
    }
}