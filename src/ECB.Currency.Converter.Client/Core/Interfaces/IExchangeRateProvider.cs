using ECB.Currency.Converter.Client.Core.Common;
using ECB.Currency.Converter.Client.Core.Domain;

namespace ECB.Currency.Converter.Client.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for obtaining exchange rates from an external source.
    /// Implementations of this interface are responsible for fetching and potentially caching rates.
    /// </summary>
    public interface IExchangeRateProvider
    {
        /// <summary>
        /// Gets the latest available exchange rates from the provider.
        /// The result encapsulates success (with rates) or failure (with an error).
        /// Note: Rates provided might be against a specific base currency (e.g., EUR for ECB).
        /// </summary>
        /// <returns>A Result containing an enumerable of ExchangeRate on success, or an Error on failure.</returns>
        Task<Result<IEnumerable<ExchangeRateEntity>>> GetLatestRatesAsync();

        /// <summary>
        /// Gets the timestamp of the last successfully fetched or updated rates, if available.
        /// Returns null if no rates have been fetched yet or the timestamp is unknown.
        /// </summary>
        /// <returns>The timestamp of the last update, or null.</returns>
        DateTimeOffset? GetLastUpdateTimestamp();
    }
}