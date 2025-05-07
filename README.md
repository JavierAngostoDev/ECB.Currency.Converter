# ECB.Currency.Converter 💱

[![NuGet version](https://img.shields.io/nuget/v/ECB.Currency.Converter.svg?style=flat-square)](https://www.nuget.org/packages/ECB.Currency.Converter/) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) [![codecov](https://codecov.io/gh/JavierAngostoDev/ECB.Currency.Converter/graph/badge.svg?token=C98ZKS3G2R)](https://codecov.io/gh/JavierAngostoDev/ECB.Currency.Converter)

**ECB.Currency.Converter** is a lightweight, reliable, and easy-to-use library for converting currencies using the official daily exchange rates published by the European Central Bank (ECB). Ideal for .NET applications that need to handle currencies without relying on paid services or API keys.

---

## 🤔 Why ECB.Currency.Converter?

Many applications need to handle different currencies or display up-to-date exchange rates. Getting this data can require paid API subscriptions or managing complex API keys.

However, for many use cases, the official daily reference rates provided for free by the **European Central Bank (ECB)** are sufficient and highly reliable.

**ECB.Currency.Converter** encapsulates the retrieval, parsing, and in-memory caching of this data, offering a **simple and robust API** to integrate into your .NET applications with no hassle and no cost.

---

## 🚀 Installation

Install the package via the .NET CLI:

```bash
dotnet add package ECB.Currency.Converter
```

Or via the NuGet Package Manager Console:

```powershell
Install-Package ECB.Currency.Converter
```

---

## ✨ Features

- ✔️ Based on official **ECB** exchange rates
- 🔁 Currency amount conversion
- 📈 Fetch exchange rate between two currencies
- 🧠 Automatically creates `CurrencyEntity` and `MoneyEntity`
- 🗓️ Retrieve last update timestamp
- 💾 In-memory cache to avoid unnecessary calls
- ☑️ Asynchronous API with `Result<T>` for safe error handling
- 🧪 Testable and injectable via `IExchangeRateProvider`
- 📦 No unnecessary external dependencies

---

## 🛠️ Usage Example

### Convert amount between currencies

```csharp
EcbConverterClient client = new EcbConverterClient();

Result<MoneyEntity> result = await client.ConvertAsync("USD", "GBP", 100m);

if (result.IsSuccess)
    Console.WriteLine($"Result: {result.Value.Amount} {result.Value.Currency}");
else
    Console.WriteLine($"Error: {result.Error.Code} - {result.Error.Message}");
```

### Get exchange rate

```csharp
EcbConverterClient client = new EcbConverterClient();

Result<ExchangeRateEntity> rateResult = await client.GetExchangeRateAsync("USD", "EUR");

if (rateResult.IsSuccess)
    Console.WriteLine($"Rate: {rateResult.Value.Rate} (Date: {rateResult.Value.Timestamp:yyyy-MM-dd})");
else
    Console.WriteLine($"Error: {rateResult.Error.Code} - {rateResult.Error.Message}");
```

### Get last update timestamp

```csharp
EcbConverterClient client = new EcbConverterClient();

DateTimeOffset? timestamp = client.GetLastRateUpdateTimestamp();

if (timestamp.HasValue)
    Console.WriteLine($"Last update: {timestamp.Value:yyyy-MM-dd HH:mm:ss}");
else
    Console.WriteLine("Last update: unknown");
```

---

## 🔄 Custom Provider Injection

You can use your own implementation of `IExchangeRateProvider`:

```csharp
IExchangeRateProvider myProvider = new EcbRateProvider(new HttpClient());
EcbConverterClient client = new EcbConverterClient(myProvider);
```

---

## 📜 License

Distributed under the MIT License. See the LICENSE file for more information.


---

## 🌍 Supported Currencies

This library supports conversions **between any of the following currencies**, using the Euro (EUR) as an internal reference base. That means you can convert from USD to JPY, GBP to AUD, and so on — not just to or from EUR.

Supported currencies (as published daily by the ECB):

- EUR (Euro)
- USD (US Dollar)
- JPY (Japanese Yen)
- BGN (Bulgarian Lev)
- CZK (Czech Koruna)
- DKK (Danish Krone)
- GBP (British Pound)
- HUF (Hungarian Forint)
- PLN (Polish Zloty)
- RON (Romanian Leu)
- SEK (Swedish Krona)
- CHF (Swiss Franc)
- ISK (Icelandic Krona)
- NOK (Norwegian Krone)
- TRY (Turkish Lira)
- AUD (Australian Dollar)
- BRL (Brazilian Real)
- CAD (Canadian Dollar)
- CNY (Chinese Yuan)
- HKD (Hong Kong Dollar)
- IDR (Indonesian Rupiah)
- ILS (Israeli Shekel)
- INR (Indian Rupee)
- KRW (South Korean Won)
- MXN (Mexican Peso)
- MYR (Malaysian Ringgit)
- NZD (New Zealand Dollar)
- PHP (Philippine Peso)
- SGD (Singapore Dollar)
- THB (Thai Baht)
- ZAR (South African Rand)

**Note**: These are the only currencies supported. All conversions are internally routed through EUR.

---

## 🙌 Author

Created by [Javier Angosto Barjollo](https://github.com/JavierAngostoDev)

---