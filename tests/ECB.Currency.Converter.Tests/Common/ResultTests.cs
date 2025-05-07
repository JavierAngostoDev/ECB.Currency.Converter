using ECB.Currency.Converter.Client.Core.Common;
using FluentAssertions;

namespace ECB.Currency.Converter.Tests.Common
{
    public class ResultTests
    {
        private static readonly Error CustomError = Error.Create("ERR_CODE", "Something went wrong");

        [Fact]
        public void Bind_When_Failure_Should_Propagate_Error()
        {
            Result<string> result = Result<string>.Failure(CustomError);

            Result<int> bound = result.Bind(s => Result<int>.Success(int.Parse(s)));

            bound.IsFailure.Should().BeTrue();
            bound.Error.Should().Be(CustomError);
        }

        [Fact]
        public void Bind_When_Success_Should_Execute_Func()
        {
            Result<string> result = Result<string>.Success("123");

            Result<int> bound = result.Bind(s => Result<int>.Success(int.Parse(s)));

            bound.IsSuccess.Should().BeTrue();
            bound.Value.Should().Be(123);
        }

        [Fact]
        public void Failure_Result_Should_Set_Error_And_IsFailure()
        {
            Result<string> result = Result<string>.Failure(CustomError);

            result.IsSuccess.Should().BeFalse();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(CustomError);
            Action act = () => _ = result.Value;

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Result is in failure state. Accessing Value is not permitted.");
        }

        [Fact]
        public void Failure_With_ErrorNone_Should_Throw()
        {
            Action act = () => Result<string>.Failure(Error.None);

            act.Should().Throw<ArgumentException>()
               .WithMessage("Cannot create a failure result with Error.None.*");
        }

        [Fact]
        public void GetValueOrDefault_When_Failure_Should_Return_Default()
        {
            Result<int> result = Result<int>.Failure(CustomError);

            result.GetValueOrDefault(999).Should().Be(999);
        }

        [Fact]
        public void GetValueOrDefault_When_Success_Should_Return_Value()
        {
            Result<int> result = Result<int>.Success(42);

            result.GetValueOrDefault(0).Should().Be(42);
        }

        [Fact]
        public void GetValueOrDefault_With_Fallback_Func_When_Failure_Should_Invoke_Func()
        {
            Result<int> result = Result<int>.Failure(CustomError);

            result.GetValueOrDefault(error => error.Code.Length).Should().Be(CustomError.Code.Length);
        }

        [Fact]
        public void GetValueOrDefault_With_Fallback_Func_When_Success_Should_Return_Value()
        {
            Result<int> result = Result<int>.Success(55);

            result.GetValueOrDefault(error => 999).Should().Be(55);
        }

        [Fact]
        public void Implicit_Error_Conversion_Should_Work()
        {
            Result<string> result = CustomError;

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be(CustomError);
        }

        [Fact]
        public void Implicit_Success_Conversion_Should_Work()
        {
            Result<string> result = "test";

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be("test");
        }

        [Fact]
        public void Map_When_Failure_Should_Propagate_Error()
        {
            Result<string> result = Result<string>.Failure(CustomError);

            Result<int> mapped = result.Map(s => s.Length);

            mapped.IsFailure.Should().BeTrue();
            mapped.Error.Should().Be(CustomError);
        }

        [Fact]
        public void Map_When_Success_Should_Transform_Value()
        {
            Result<string> result = Result<string>.Success("ok");

            Result<int> mapped = result.Map(s => s.Length);

            mapped.IsSuccess.Should().BeTrue();
            mapped.Value.Should().Be(2);
        }

        [Fact]
        public void Match_Generic_When_Failure_Should_Return_Error_Result()
        {
            Result<string> result = Result<string>.Failure(CustomError);

            var output = result.Match(
                onSuccess: s => s.ToUpper(),
                onFailure: e => e.Code
            );

            output.Should().Be(CustomError.Code);
        }

        [Fact]
        public void Match_Generic_When_Success_Should_Return_Success_Result()
        {
            Result<string> result = Result<string>.Success("hello");

            string output = result.Match(
                onSuccess: s => s.ToUpper(),
                onFailure: e => "error"
            );

            output.Should().Be("HELLO");
        }

        [Fact]
        public void Match_Void_When_Failure_Should_Invoke_OnFailure()
        {
            Result<string> result = Result<string>.Failure(CustomError);
            string captured = "";

            result.Match(
                onSuccess: _ => captured = "success",
                onFailure: e => captured = e.Code
            );

            captured.Should().Be(CustomError.Code);
        }

        [Fact]
        public void Match_Void_When_Success_Should_Invoke_OnSuccess()
        {
            Result<string> result = Result<string>.Success("hi");
            string captured = "";

            result.Match(
                onSuccess: v => captured = v,
                onFailure: _ => captured = "fail"
            );

            captured.Should().Be("hi");
        }

        [Fact]
        public void Success_Result_Should_Set_Value_And_IsSuccess()
        {
            Result<string> result = Result<string>.Success("ok");

            result.IsSuccess.Should().BeTrue();
            result.IsFailure.Should().BeFalse();
            result.Value.Should().Be("ok");
            result.Error.Should().Be(Error.None);
        }

        [Fact]
        public void Match_When_Success_Should_Invoke_OnSuccess_Action()
        {
            // Arrange
            Result<string> result = Result<string>.Success("hello");
            string capturedValue = string.Empty;
            bool failureInvoked = false;

            // Act
            result.Match(
                onSuccess: value => capturedValue = value,
                onFailure: error => failureInvoked = true
            );

            // Assert
            capturedValue.Should().Be("hello");
            failureInvoked.Should().BeFalse();
        }

        [Fact]
        public void Match_When_Failure_Should_Invoke_OnFailure_Action()
        {
            // Arrange
            Error customError = Error.Create("E01", "fail");
            Result<string> result = Result<string>.Failure(customError);
            bool successInvoked = false;
            string capturedCode = string.Empty;

            // Act
            result.Match(
                onSuccess: value => successInvoked = true,
                onFailure: error => capturedCode = error.Code
            );

            // Assert
            capturedCode.Should().Be("E01");
            successInvoked.Should().BeFalse();
        }

    }
}