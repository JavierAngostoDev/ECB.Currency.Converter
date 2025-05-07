using ECB.Currency.Converter.Client.Core.Common;

namespace ECB.Currency.Converter.Client.Core.Domain
{
    public readonly record struct ExchangeRateEntity
    {
        public CurrencyEntity BaseCurrency { get; }
        public CurrencyEntity QuoteCurrency { get; }
        public decimal Rate { get; }
        public DateTimeOffset Timestamp { get; }

        public static readonly Error NonPositiveRateError = Error.Create("ExchangeRate.Validation", "Exchange rate must be positive.");
        public static readonly Error SameCurrencyError = Error.Create("ExchangeRate.Validation", "Base and quote currency cannot be the same.");

        private ExchangeRateEntity(CurrencyEntity baseCurrency, CurrencyEntity quoteCurrency, decimal rate, DateTimeOffset timestamp)
        {
            BaseCurrency = baseCurrency;
            QuoteCurrency = quoteCurrency;
            Rate = rate;
            Timestamp = timestamp;
        }

        public static Result<ExchangeRateEntity> Create(CurrencyEntity baseCurrency, CurrencyEntity quoteCurrency, decimal rate, DateTimeOffset timestamp)
        {
            return Result<ExchangeRateEntity>.Success(new ExchangeRateEntity(baseCurrency, quoteCurrency, rate, timestamp));
        }

        public override string ToString() => $"1 {BaseCurrency} = {Rate.ToString(System.Globalization.CultureInfo.InvariantCulture)} {QuoteCurrency} @ {Timestamp:O}";

        public Result<ExchangeRateEntity> Invert()
        {
            if (Rate == 0)
                return Result<ExchangeRateEntity>.Failure(Error.Create("ExchangeRate.ZeroRate", "Cannot invert a zero rate."));

            return Create(QuoteCurrency, BaseCurrency, 1.0m / Rate, Timestamp);
        }
    }
}