namespace ECB.Currency.Converter.Client.Core.Common
{
    /// <summary>
    /// Represents the result of an operation, either success with a value or failure with an error.
    /// </summary>
    /// <typeparam name="TValue">The type of the value returned on success.</typeparam>
    public readonly struct Result<TValue>
    {
        private readonly Error _error;
        private readonly TValue? _value;

        private Result(TValue value)
        {
            IsSuccess = true;
            _value = value;
            _error = Error.None;
        }

        private Result(Error error)
        {
            if (error.Code == Error.None.Code)
                throw new ArgumentException("Cannot create a failure result with Error.None.", nameof(error));

            IsSuccess = false;
            _value = default;
            _error = error;
        }

        public Error Error => IsFailure ? _error : Error.None;
        public bool IsFailure => !IsSuccess;
        public bool IsSuccess { get; }
        public TValue Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("Result is in failure state. Accessing Value is not permitted.");

        public static Result<TValue> Failure(Error error) => new Result<TValue>(error);

        public static implicit operator Result<TValue>(TValue value) => Success(value);

        public static implicit operator Result<TValue>(Error error) => Failure(error);

        public static Result<TValue> Success(TValue value) => new Result<TValue>(value);

        public Result<TNewValue> Bind<TNewValue>(Func<TValue, Result<TNewValue>> func) =>
            IsSuccess ? func(Value) : Result<TNewValue>.Failure(Error);

        public TValue GetValueOrDefault(TValue defaultValue) => IsSuccess ? Value : defaultValue;

        public TValue GetValueOrDefault(Func<Error, TValue> fallback) => IsSuccess ? Value : fallback(Error);

        public Result<TNewValue> Map<TNewValue>(Func<TValue, TNewValue> func) =>
                                    IsSuccess ? Result<TNewValue>.Success(func(Value)) : Result<TNewValue>.Failure(Error);

        public void Match(Action<TValue> onSuccess, Action<Error> onFailure)
        {
            if (IsSuccess) onSuccess(Value);
            else onFailure(Error);
        }

        public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<Error, TResult> onFailure) =>
            IsSuccess ? onSuccess(Value) : onFailure(Error);
    }
}