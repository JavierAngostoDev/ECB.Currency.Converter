using ECB.Currency.Converter.Core.Domain;

namespace ECB.Currency.Converter.Core.Features.GetExchangeRate;

internal record GetExchangeRateQuery(CurrencyEntity FromCurrency, CurrencyEntity ToCurrency);