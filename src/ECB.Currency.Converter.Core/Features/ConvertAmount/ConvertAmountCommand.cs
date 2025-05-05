using ECB.Currency.Converter.Core.Domain;

namespace ECB.Currency.Converter.Core.Features.ConvertAmount;

internal record ConvertAmountCommand(MoneyEntity SourceMoney, CurrencyEntity TargetCurrency);