namespace ECB.Currency.Converter.Client.Core.Common
{
    /// <summary>
    /// Represents a standard error with a code and message.
    /// </summary>
    public readonly record struct Error
    {
        public string Code { get; }
        public string Message { get; }

        private Error(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public static Error Create(string code, string message) => new Error(code, message);

        public static readonly Error None = new(string.Empty, string.Empty);
        public static readonly Error NullValue = new("Error.NullValue", "Null value was provided.");
    }
}