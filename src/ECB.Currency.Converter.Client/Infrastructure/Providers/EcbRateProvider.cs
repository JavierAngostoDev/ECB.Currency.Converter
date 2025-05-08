using System.Globalization;
using System.Xml.Linq;
using ECB.Currency.Converter.Client.Core.Common;
using ECB.Currency.Converter.Client.Core.Domain;
using ECB.Currency.Converter.Client.Core.Interfaces;

namespace ECB.Currency.Converter.Client.Infrastructure.Providers
{
    internal class EcbRateProvider
    (
        HttpClient httpClient,
        TimeSpan? cacheDuration = null
    ) : IExchangeRateProvider
    {
        #region Properties

        public static readonly Error CurrencyParsingError = Error.Create("EcbProvider.CurrencyEntityParse", "Failed to parse CurrencyEntity attribute in ECB XML.");
        public static readonly Error HttpError = Error.Create("EcbProvider.Http", "Failed to fetch data from ECB URL.");
        public static readonly Error NoRatesFoundError = Error.Create("EcbProvider.NoRates", "No exchange rates found in the ECB XML data.");
        public static readonly Error RateParsingError = Error.Create("EcbProvider.RateParse", "Failed to parse rate attribute in ECB XML.");
        public static readonly Error XmlParsingError = Error.Create("EcbProvider.Xml", "Failed to parse ECB XML data.");
        private const string EcbUrl = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";
        private static readonly SemaphoreSlim _cacheLock = new(1, 1);
        private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromHours(4);
        private static readonly CurrencyEntity EuroBaseCurrency = "EUR";
        private static List<ExchangeRateEntity>? _cachedRates = null;
        private static DateTimeOffset _cacheTimestamp = DateTimeOffset.MinValue;
        private readonly TimeSpan _cacheDuration = cacheDuration ?? DefaultCacheDuration;
        private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        #endregion Properties

        #region Public

        public DateTimeOffset? GetLastUpdateTimestamp()
        {
            if (_cachedRates != null && DateTimeOffset.UtcNow - _cacheTimestamp < _cacheDuration)
                return _cacheTimestamp;

            return null;
        }

        public async Task<Result<IEnumerable<ExchangeRateEntity>>> GetLatestRatesAsync()
        {
            await _cacheLock.WaitAsync();
            try
            {
                if (_cachedRates != null && DateTimeOffset.UtcNow - _cacheTimestamp < _cacheDuration)
                    return Result<IEnumerable<ExchangeRateEntity>>.Success(new List<ExchangeRateEntity>(_cachedRates));
            }
            finally
            {
                _cacheLock.Release();
            }

            Result<IEnumerable<ExchangeRateEntity>> fetchResult = await FetchAndParseEcbDataAsync();

            if (fetchResult.IsFailure)
                return Result<IEnumerable<ExchangeRateEntity>>.Failure(fetchResult.Error);

            await _cacheLock.WaitAsync();
            try
            {
                _cachedRates = fetchResult.Value.ToList();
                _cacheTimestamp = DateTimeOffset.UtcNow;
                return Result<IEnumerable<ExchangeRateEntity>>.Success(new List<ExchangeRateEntity>(_cachedRates));
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        #endregion Public

        #region Private

        private async Task<Result<IEnumerable<ExchangeRateEntity>>> FetchAndParseEcbDataAsync()
        {
            string xmlContent;
            try
            {
                using HttpResponseMessage response = await _httpClient.GetAsync(EcbUrl);
                if (!response.IsSuccessStatusCode)
                    return Result<IEnumerable<ExchangeRateEntity>>.Failure(Error.Create(HttpError.Code, $"{HttpError.Message} Status code: {response.StatusCode}"));

                xmlContent = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                return Result<IEnumerable<ExchangeRateEntity>>.Failure(Error.Create(HttpError.Code, $"{HttpError.Message} Details: {ex.Message}"));
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<ExchangeRateEntity>>.Failure(Error.Create(HttpError.Code, $"{HttpError.Message} Unexpected error: {ex.Message}"));
            }

            try
            {
                XDocument xmlDoc = XDocument.Parse(xmlContent);
                XNamespace gesmes = "http://www.gesmes.org/xml/2002-08-01";
                XNamespace ecb = "http://www.ecb.int/vocabulary/2002-08-01/eurofxref";

                XElement? dailyCube = xmlDoc.Descendants(ecb + "Cube")
                                    .FirstOrDefault(c => c.Attribute("time") != null);

                if (dailyCube is null)
                    return Result<IEnumerable<ExchangeRateEntity>>.Failure(XmlParsingError);

                if (!DateTimeOffset.TryParse(dailyCube.Attribute("time")?.Value, out DateTimeOffset rateTimestamp))
                    rateTimestamp = DateTimeOffset.UtcNow.Date;

                List<ExchangeRateEntity> parsedRates = [];

                foreach (XElement rateCube in dailyCube.Elements(ecb + "Cube"))
                {
                    string? currencyCode = rateCube.Attribute("currency")?.Value ?? rateCube.Attribute("Currency")?.Value;
                    string? rateValueStr = rateCube.Attribute("rate")?.Value ?? rateCube.Attribute("Rate")?.Value;

                    if (string.IsNullOrWhiteSpace(currencyCode) || string.IsNullOrWhiteSpace(rateValueStr))
                    {
                        Console.WriteLine($"Warning: Skipping rate cube due to missing Currency or rate attribute. XML: {rateCube}");
                        continue;
                    }

                    Result<CurrencyEntity> currencyResult = CurrencyEntity.Create(currencyCode);
                    if (currencyResult.IsFailure)
                        return Result<IEnumerable<ExchangeRateEntity>>.Failure(Error.Create(CurrencyParsingError.Code, $"{CurrencyParsingError.Message} Code: {currencyCode}"));

                    CurrencyEntity quoteCurrency = currencyResult.Value;

                    if (!decimal.TryParse(rateValueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal rateDecimal))
                    {
                        return Result<IEnumerable<ExchangeRateEntity>>.Failure(Error.Create(RateParsingError.Code, $"{RateParsingError.Message} Code: {currencyCode}, Value: '{rateValueStr}'"));
                    }

                    Result<ExchangeRateEntity> rateResult = ExchangeRateEntity.Create
                    (
                        baseCurrency: EuroBaseCurrency,
                        quoteCurrency: quoteCurrency,
                        rate: rateDecimal,
                        timestamp: rateTimestamp
                    );

                    if (rateResult.IsFailure)
                        return Result<IEnumerable<ExchangeRateEntity>>.Failure(rateResult.Error);

                    parsedRates.Add(rateResult.Value);
                }

                if (!parsedRates.Any())
                    return Result<IEnumerable<ExchangeRateEntity>>.Failure(NoRatesFoundError);

                return Result<IEnumerable<ExchangeRateEntity>>.Success(parsedRates);
            }
            catch (System.Xml.XmlException ex)
            {
                return Result<IEnumerable<ExchangeRateEntity>>.Failure(Error.Create(XmlParsingError.Code, $"{XmlParsingError.Message} Details: {ex.Message}"));
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<ExchangeRateEntity>>.Failure(Error.Create(XmlParsingError.Code, $"{XmlParsingError.Message} Unexpected error: {ex.Message}"));
            }
        }

        #endregion Private
    }
}