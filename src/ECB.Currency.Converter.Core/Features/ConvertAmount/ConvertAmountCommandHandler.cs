using ECB.Currency.Converter.Core.Common;
using ECB.Currency.Converter.Core.Domain;
using ECB.Currency.Converter.Core.Features.GetExchangeRate;
using ECB.Currency.Converter.Core.Interfaces;

namespace ECB.Currency.Converter.Core.Features.ConvertAmount
{
    internal class ConvertAmountCommandHandler
    {
        #region Properties

        public static readonly Error GetRateFailedError = Error.Create("ConvertAmount.GetRateFailed", "Failed to get exchange rate for conversion.");
        private readonly GetExchangeRateQueryHandler _getExchangeRateHandler;

        #endregion Properties

        #region Constructor

        public ConvertAmountCommandHandler(IExchangeRateProvider rateProvider)
        {
            ArgumentNullException.ThrowIfNull(rateProvider);
            _getExchangeRateHandler = new GetExchangeRateQueryHandler(rateProvider);
        }

        #endregion Constructor

        #region Public

        public async Task<Result<MoneyEntity>> Handle(ConvertAmountCommand command)
        {
            MoneyEntity sourceMoney = command.SourceMoney;
            CurrencyEntity targetCurrency = command.TargetCurrency;

            if (sourceMoney.Amount == 0)
                return MoneyEntity.Create(0m, targetCurrency);

            if (sourceMoney.Currency == targetCurrency)
                return Result<MoneyEntity>.Success(sourceMoney);

            GetExchangeRateQuery rateQuery = new(sourceMoney.Currency, targetCurrency);
            Result<ExchangeRateEntity> rateResult = await _getExchangeRateHandler.Handle(rateQuery);

            if (rateResult.IsFailure)
                return Result<MoneyEntity>.Failure(Error.Create(GetRateFailedError.Code, $"{GetRateFailedError.Message} Details: {rateResult.Error.Message}"));

            decimal exchangeRateValue = rateResult.Value.Rate;
            decimal convertedAmount = sourceMoney.Amount * exchangeRateValue;

            return MoneyEntity.Create(convertedAmount, targetCurrency);
        }

        #endregion Public
    }
}