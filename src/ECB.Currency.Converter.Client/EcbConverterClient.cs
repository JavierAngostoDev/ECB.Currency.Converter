using ECB.Currency.Converter.Client.Core.Common;
using ECB.Currency.Converter.Client.Core.Domain;
using ECB.Currency.Converter.Client.Core.Features.ConvertAmount;
using ECB.Currency.Converter.Client.Core.Features.GetExchangeRate;
using ECB.Currency.Converter.Client.Core.Interfaces;
using ECB.Currency.Converter.Client.Infrastructure.Providers;

namespace ECB.Currency.Converter.Client
{
    public sealed class EcbConverterClient : IDisposable
    {
        #region Properties

        private readonly ConvertAmountCommandHandler _convertAmountHandler;
        private readonly bool _disposeHttpClient;
        private readonly GetExchangeRateQueryHandler _getExchangeRateHandler;
        private readonly HttpClient? _internalHttpClient;
        private readonly IExchangeRateProvider _rateProvider;

        #endregion Properties

        #region Constructor

        public EcbConverterClient()
        {
            _internalHttpClient = new HttpClient();
            _rateProvider = new EcbRateProvider(_internalHttpClient);
            _disposeHttpClient = true;

            _getExchangeRateHandler = new(_rateProvider);
            _convertAmountHandler = new(_rateProvider);
        }

        #endregion Constructor

        #region Public

        public EcbConverterClient(IExchangeRateProvider rateProvider)
        {
            _rateProvider = rateProvider ?? throw new ArgumentNullException(nameof(rateProvider));
            _disposeHttpClient = false;
            _internalHttpClient = null;

            _getExchangeRateHandler = new(_rateProvider);
            _convertAmountHandler = new(_rateProvider);
        }

        public async Task<Result<MoneyEntity>> ConvertAsync(string fromCurrencyCode, string toCurrencyCode, decimal amount)
        {
            Result<CurrencyEntity> fromResult = CurrencyEntity.Create(fromCurrencyCode);
            Result<CurrencyEntity> toResult = CurrencyEntity.Create(toCurrencyCode);

            if (fromResult.IsFailure)
                return Result<MoneyEntity>.Failure(fromResult.Error);
            if (toResult.IsFailure)
                return Result<MoneyEntity>.Failure(toResult.Error);

            Result<MoneyEntity> sourceMoneyResult = MoneyEntity.Create(amount, fromResult.Value);
            if (sourceMoneyResult.IsFailure)
                return Result<MoneyEntity>.Failure(sourceMoneyResult.Error);

            ConvertAmountCommand command = new(sourceMoneyResult.Value, toResult.Value);
            return await _convertAmountHandler.Handle(command);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<Result<ExchangeRateEntity>> GetExchangeRateAsync(string fromCurrencyCode, string toCurrencyCode)
        {
            Result<CurrencyEntity> fromResult = CurrencyEntity.Create(fromCurrencyCode);
            Result<CurrencyEntity> toResult = CurrencyEntity.Create(toCurrencyCode);

            if (fromResult.IsFailure)
                return Result<ExchangeRateEntity>.Failure(fromResult.Error);
            if (toResult.IsFailure)
                return Result<ExchangeRateEntity>.Failure(toResult.Error);

            GetExchangeRateQuery query = new(fromResult.Value, toResult.Value);
            return await _getExchangeRateHandler.Handle(query);
        }

        public DateTimeOffset? GetLastRateUpdateTimestamp() => _rateProvider.GetLastUpdateTimestamp();

        #endregion Public

        #region Private

        private bool _disposed = false;

        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing && _disposeHttpClient && _internalHttpClient != null)
                _internalHttpClient.Dispose();

            _disposed = true;
        }

        #endregion Private
    }
}