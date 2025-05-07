using System.Globalization;
using ECB.Currency.Converter.Client.Core.Common;

namespace ECB.Currency.Converter.Client.Core.Domain
{
    public readonly record struct MoneyEntity
    {
        public decimal Amount { get; }
        public CurrencyEntity Currency { get; }

        public MoneyEntity(decimal amount, CurrencyEntity currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public static Result<MoneyEntity> Create(decimal amount, CurrencyEntity currency) => Result<MoneyEntity>.Success(new MoneyEntity(amount, currency));
        public override string ToString() => $"{Amount.ToString("F2", CultureInfo.InvariantCulture)} {Currency}";
        public static Result<MoneyEntity> Add(MoneyEntity a, MoneyEntity b)
        {
            if (a.Currency != b.Currency)
                return Result<MoneyEntity>.Failure(Error.Create("Money.Mismatch", "Cannot add Money of different currencies."));

            return Create(a.Amount + b.Amount, a.Currency);
        }
    }
}