using ECB.Currency.Converter.Client.Core.Domain;

namespace ECB.Currency.Converter.Client.Core.Features.ConvertAmount;

internal record ConvertAmountCommand(MoneyEntity SourceMoney, CurrencyEntity TargetCurrency);