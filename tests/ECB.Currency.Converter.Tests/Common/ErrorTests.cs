using ECB.Currency.Converter.Core.Common;
using FluentAssertions;

namespace ECB.Currency.Converter.Tests.Common
{
    public class ErrorTests
    {
        [Fact]
        public void Create_Should_Set_Code_And_Message()
        {
            // Arrange
            string code = "ERR001";
            string message = "Something went wrong";

            // Act
            var error = Error.Create(code, message);

            // Assert
            error.Code.Should().Be(code);
            error.Message.Should().Be(message);
        }

        [Fact]
        public void Errors_With_Different_Values_Should_Not_Be_Equal()
        {
            Error e1 = Error.Create("ERR_X", "Oops");
            Error e2 = Error.Create("ERR_Y", "Different");

            e1.Should().NotBe(e2);
            e1.Equals(e2).Should().BeFalse();
        }

        [Fact]
        public void Errors_With_Same_Values_Should_Be_Equal()
        {
            Error e1 = Error.Create("ERR_X", "Oops");
            Error e2 = Error.Create("ERR_X", "Oops");

            e1.Should().Be(e2);
            e1.Equals(e2).Should().BeTrue();
            e1.GetHashCode().Should().Be(e2.GetHashCode());
        }

        [Fact]
        public void None_Should_Have_Empty_Code_And_Message()
        {
            Error error = Error.None;

            error.Code.Should().BeEmpty();
            error.Message.Should().BeEmpty();
        }

        [Fact]
        public void NullValue_Should_Have_Expected_Values()
        {
            Error error = Error.NullValue;

            error.Code.Should().Be("Error.NullValue");
            error.Message.Should().Be("Null value was provided.");
        }

        [Fact]
        public void ToString_Should_Contain_Code_And_Message()
        {
            Error error = Error.Create("E123", "Invalid input");

            string str = error.ToString();

            str.Should().Contain("E123").And.Contain("Invalid input");
        }
    }
}