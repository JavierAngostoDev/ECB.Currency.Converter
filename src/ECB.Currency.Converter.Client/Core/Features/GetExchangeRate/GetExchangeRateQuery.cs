using ECB.Currency.Converter.Client.Core.Domain;

namespace ECB.Currency.Converter.Client.Core.Features.GetExchangeRate;

internal record GetExchangeRateQuery(CurrencyEntity FromCurrency, CurrencyEntity ToCurrency);