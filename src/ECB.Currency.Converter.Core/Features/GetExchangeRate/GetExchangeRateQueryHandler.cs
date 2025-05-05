using ECB.Currency.Converter.Core.Common;
using ECB.Currency.Converter.Core.Domain;
using ECB.Currency.Converter.Core.Interfaces;

namespace ECB.Currency.Converter.Core.Features.GetExchangeRate
{
    internal class GetExchangeRateQueryHandler(IExchangeRateProvider rateProvider)
    {
        #region Properties

        public static readonly Error ProviderError = Error.Create("GetExchangeRate.ProviderError", "Failed to retrieve rates from the provider.");
        public static readonly Error RateNotFound = Error.Create("GetExchangeRate.NotFound", "Required currency rate not found in provider data.");
        private static readonly CurrencyEntity EuroCurrency = "EUR";
        private readonly IExchangeRateProvider _rateProvider = rateProvider ?? throw new ArgumentNullException(nameof(rateProvider));

        #endregion Properties

        #region Public

        public async Task<Result<ExchangeRateEntity>> Handle(GetExchangeRateQuery query)
        {
            if (query.FromCurrency == query.ToCurrency)
            {
                DateTimeOffset timestamp = _rateProvider.GetLastUpdateTimestamp() ?? DateTimeOffset.UtcNow;
                return ExchangeRateEntity.Create(query.FromCurrency, query.ToCurrency, 1.0m, timestamp);
            }

            Result<IEnumerable<ExchangeRateEntity>> providerResult = await _rateProvider.GetLatestRatesAsync();

            if (providerResult.IsFailure)
                return Result<ExchangeRateEntity>.Failure(Error.Create(ProviderError.Code, $"{ProviderError.Message} Details: {providerResult.Error.Message}"));

            Dictionary<CurrencyEntity, ExchangeRateEntity> ratesVsEur = providerResult.Value.ToDictionary(r => r.QuoteCurrency);
            DateTimeOffset rateTimestamp = ratesVsEur.Count != 0 ? ratesVsEur.First().Value.Timestamp : DateTimeOffset.UtcNow;

            Result<decimal> fromRateResult = GetRateVsEur(query.FromCurrency, ratesVsEur);
            Result<decimal> toRateResult = GetRateVsEur(query.ToCurrency, ratesVsEur);

            if (fromRateResult.IsFailure)
                return Result<ExchangeRateEntity>.Failure(fromRateResult.Error);

            if (toRateResult.IsFailure)
                return Result<ExchangeRateEntity>.Failure(toRateResult.Error);

            decimal fromRateVsEur = fromRateResult.Value;
            decimal toRateVsEur = toRateResult.Value;

            if (fromRateVsEur == 0)
                return Result<ExchangeRateEntity>.Failure(Error.Create("GetExchangeRate.ZeroRate", $"Rate for base currency '{query.FromCurrency}' is zero."));

            decimal crossRate = toRateVsEur / fromRateVsEur;

            return ExchangeRateEntity.Create(query.FromCurrency, query.ToCurrency, crossRate, rateTimestamp);
        }

        #endregion Public

        #region Private

        private static Result<decimal> GetRateVsEur(CurrencyEntity currency, Dictionary<CurrencyEntity, ExchangeRateEntity> ratesVsEur)
        {
            if (currency == EuroCurrency)
                return Result<decimal>.Success(1.0m);

            if (ratesVsEur.TryGetValue(currency, out ExchangeRateEntity rateInfo))
            {
                if (rateInfo.Rate <= 0)
                    return Result<decimal>.Failure(Error.Create("GetExchangeRate.InvalidStoredRate", $"Stored rate for '{currency}' is not positive."));

                return Result<decimal>.Success(rateInfo.Rate);
            }

            return Result<decimal>.Failure(Error.Create(RateNotFound.Code, $"{RateNotFound.Message} Currency: {currency}"));
        }

        #endregion Private
    }
}