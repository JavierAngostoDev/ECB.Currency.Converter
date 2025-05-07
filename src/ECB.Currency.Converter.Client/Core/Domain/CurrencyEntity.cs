using System.Text.RegularExpressions;
using ECB.Currency.Converter.Client.Core.Common;

namespace ECB.Currency.Converter.Client.Core.Domain
{
    public readonly record struct CurrencyEntity
    {
        private static readonly Regex IsoCodeRegex = new Regex("^[A-Z]{3}$", RegexOptions.Compiled);
        public static readonly Error ValidationError = Error.Create("Currency.Validation", "Invalid currency ISO code. Must be 3 uppercase letters.");

        public string Code { get; }

        private CurrencyEntity(string code) => Code = code;

        public static Result<CurrencyEntity> Create(string code)
        {
            if (string.IsNullOrWhiteSpace(code) || !IsoCodeRegex.IsMatch(code))
                return Result<CurrencyEntity>.Failure(ValidationError);

            return Result<CurrencyEntity>.Success(new CurrencyEntity(code.ToUpperInvariant()));
        }

        public override string ToString() => Code;
        public static implicit operator CurrencyEntity(string code) => new(code.ToUpperInvariant());
        public static implicit operator string(CurrencyEntity currency) => currency.Code;
    }
}