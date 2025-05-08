using System.Net;
using System.Text;
using ECB.Currency.Converter.Client.Core.Common;
using ECB.Currency.Converter.Client.Core.Domain;
using ECB.Currency.Converter.Client.Infrastructure.Providers;
using FluentAssertions;
using Moq;
using Moq.Protected;

namespace ECB.Currency.Converter.Tests.Infrastructure.Providers
{
    public static class EcbRateProviderTestHelper
    {
        public static void ResetCache()
        {
            typeof(EcbRateProvider)
                .GetField("_cachedRates", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, null);

            typeof(EcbRateProvider)
                .GetField("_cacheTimestamp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.SetValue(null, DateTimeOffset.MinValue);
        }
    }

    public class EcbRateProviderTests
    {
        [Fact]
        public async Task GetLastUpdateTimestamp_Should_Reflect_Cache()
        {
            EcbRateProviderTestHelper.ResetCache();

            string xml = """
            <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01"
                             xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
              <Cube>
                <Cube time="2024-05-07">
                  <Cube Currency="USD" rate="1.00" />
                </Cube>
              </Cube>
            </gesmes:Envelope>
            """;

            HttpClient client = CreateMockHttpClient(HttpStatusCode.OK, xml);
            EcbRateProvider provider = new EcbRateProvider(client, TimeSpan.FromHours(1));

            DateTimeOffset? before = provider.GetLastUpdateTimestamp();
            before.Should().BeNull();

            await provider.GetLatestRatesAsync();

            DateTimeOffset? after = provider.GetLastUpdateTimestamp();
            after.Should().NotBeNull();
        }

        [Fact]
        public async Task GetLatestRatesAsync_Should_Default_To_UtcNow_When_Timestamp_Invalid()
        {
            // Arrange
            EcbRateProviderTestHelper.ResetCache();

            string xml = """
            <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01"
                             xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
              <Cube>
                <Cube time="not-a-valid-date">
                  <Cube Currency="USD" rate="1.1" />
                </Cube>
              </Cube>
            </gesmes:Envelope>
            """;

            HttpClient client = CreateMockHttpClient(HttpStatusCode.OK, xml);
            EcbRateProvider provider = new EcbRateProvider(client);

            // Act
            Result<IEnumerable<ExchangeRateEntity>> result = await provider.GetLatestRatesAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            ExchangeRateEntity rate = result.Value.First();
            rate.QuoteCurrency.Code.Should().Be("USD");
            rate.Rate.Should().Be(1.1m);
            rate.Timestamp.Date.Should().Be(DateTimeOffset.UtcNow.Date);
        }

        [Fact]
        public async Task GetLatestRatesAsync_Should_Return_CachedRates_When_StillValid()
        {
            // Arrange
            EcbRateProviderTestHelper.ResetCache();

            string xml = """
            <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01"
                             xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
              <Cube>
                <Cube time="2024-05-07">
                  <Cube Currency="USD" rate="1.10" />
                </Cube>
              </Cube>
            </gesmes:Envelope>
            """;

            Mock<HttpMessageHandler> handlerMock = new();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(xml)
                })
                .Verifiable();

            HttpClient httpClient = new HttpClient(handlerMock.Object);
            EcbRateProvider provider = new EcbRateProvider(httpClient, TimeSpan.FromMinutes(10));

            Result<IEnumerable<ExchangeRateEntity>> first = await provider.GetLatestRatesAsync();
            first.IsSuccess.Should().BeTrue();

            Result<IEnumerable<ExchangeRateEntity>> second = await provider.GetLatestRatesAsync();

            // Assert
            second.IsSuccess.Should().BeTrue();
            second.Value.Should().HaveCount(1);
            second.Value.First().QuoteCurrency.Code.Should().Be("USD");
            second.Value.First().Rate.Should().Be(1.10m);

            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task GetLatestRatesAsync_Should_Return_Error_When_Currency_Is_Invalid()
        {
            EcbRateProviderTestHelper.ResetCache();

            string xml = """
            <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01"
                             xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
              <Cube>
                <Cube time="2024-05-07">
                  <Cube Currency="INVALID" rate="1.2" />
                </Cube>
              </Cube>
            </gesmes:Envelope>
            """;

            HttpClient client = CreateMockHttpClient(HttpStatusCode.OK, xml);
            EcbRateProvider provider = new EcbRateProvider(client);

            Result<IEnumerable<ExchangeRateEntity>> result = await provider.GetLatestRatesAsync();

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(EcbRateProvider.CurrencyParsingError.Code);
        }

        [Fact]
        public async Task GetLatestRatesAsync_Should_Return_Error_When_Http_Fails()
        {
            EcbRateProviderTestHelper.ResetCache();

            HttpClient client = CreateMockHttpClient(HttpStatusCode.InternalServerError, "error");
            EcbRateProvider provider = new EcbRateProvider(client);

            Result<IEnumerable<ExchangeRateEntity>> result = await provider.GetLatestRatesAsync();

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(EcbRateProvider.HttpError.Code);
        }

        [Fact]
        public async Task GetLatestRatesAsync_Should_Return_Error_When_HttpRequestException_Thrown()
        {
            // Arrange
            EcbRateProviderTestHelper.ResetCache();

            Mock<HttpMessageHandler> handlerMock = new();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Simulated connection failure"));

            HttpClient client = new HttpClient(handlerMock.Object);
            EcbRateProvider provider = new EcbRateProvider(client);

            // Act
            Result<IEnumerable<ExchangeRateEntity>> result = await provider.GetLatestRatesAsync();

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(EcbRateProvider.HttpError.Code);
            result.Error.Message.Should().Contain("Simulated connection failure");
        }

        [Fact]
        public async Task GetLatestRatesAsync_Should_Return_Error_When_Rate_Is_Invalid()
        {
            EcbRateProviderTestHelper.ResetCache();

            string xml = """
            <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01"
                             xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
              <Cube>
                <Cube time="2024-05-07">
                  <Cube Currency="USD" rate="INVALID" />
                </Cube>
              </Cube>
            </gesmes:Envelope>
            """;

            HttpClient client = CreateMockHttpClient(HttpStatusCode.OK, xml);
            EcbRateProvider provider = new EcbRateProvider(client);

            Result<IEnumerable<ExchangeRateEntity>> result = await provider.GetLatestRatesAsync();

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(EcbRateProvider.RateParsingError.Code);
        }

        [Fact]
        public async Task GetLatestRatesAsync_Should_Return_Error_When_Rates_Are_Empty()
        {
            EcbRateProviderTestHelper.ResetCache();

            string xml = """
            <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01"
                             xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
              <Cube>
                <Cube time="2024-05-07">
                </Cube>
              </Cube>
            </gesmes:Envelope>
            """;

            HttpClient client = CreateMockHttpClient(HttpStatusCode.OK, xml);
            EcbRateProvider provider = new EcbRateProvider(client);

            Result<IEnumerable<ExchangeRateEntity>> result = await provider.GetLatestRatesAsync();

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(EcbRateProvider.NoRatesFoundError.Code);
        }

        [Fact]
        public async Task GetLatestRatesAsync_Should_Return_Error_When_Response_Is_Unparseable()
        {
            EcbRateProviderTestHelper.ResetCache();

            HttpClient client = CreateMockHttpClient(HttpStatusCode.OK, "<invalid><xml>");
            EcbRateProvider provider = new EcbRateProvider(client);

            Result<IEnumerable<ExchangeRateEntity>> result = await provider.GetLatestRatesAsync();

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(EcbRateProvider.XmlParsingError.Code);
        }

        [Fact]
        public async Task GetLatestRatesAsync_Should_Return_Error_When_Unexpected_Exception_Thrown()
        {
            // Arrange
            EcbRateProviderTestHelper.ResetCache();

            Mock<HttpMessageHandler> handlerMock = new();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("Unexpected internal failure"));

            HttpClient client = new HttpClient(handlerMock.Object);
            EcbRateProvider provider = new EcbRateProvider(client);

            // Act
            Result<IEnumerable<ExchangeRateEntity>> result = await provider.GetLatestRatesAsync();

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(EcbRateProvider.HttpError.Code);
            result.Error.Message.Should().Contain("Unexpected internal failure");
            result.Error.Message.Should().Contain("Unexpected error");
        }

        [Fact]
        public async Task GetLatestRatesAsync_Should_Return_Success_When_Valid_Xml_Provided()
        {
            EcbRateProviderTestHelper.ResetCache();

            string xml = """
            <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01"
                             xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
              <Cube>
                <Cube time="2024-05-07">
                  <Cube Currency="USD" rate="1.12" />
                </Cube>
              </Cube>
            </gesmes:Envelope>
            """;

            HttpClient client = CreateMockHttpClient(HttpStatusCode.OK, xml);
            EcbRateProvider provider = new EcbRateProvider(client);

            Result<IEnumerable<ExchangeRateEntity>> result = await provider.GetLatestRatesAsync();

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().ContainSingle();
            result.Value.First().QuoteCurrency.Code.Should().Be("USD");
            result.Value.First().Rate.Should().Be(1.12m);
        }

        [Fact]
        public async Task GetLatestRatesAsync_Should_Return_XmlParsingError_When_DailyCube_NotFound()
        {
            // Arrange
            EcbRateProviderTestHelper.ResetCache();

            string xml = """
            <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01"
                             xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
              <Cube>
                <!-- No Cube with time attribute -->
              </Cube>
            </gesmes:Envelope>
            """;

            HttpClient client = CreateMockHttpClient(HttpStatusCode.OK, xml);
            EcbRateProvider provider = new EcbRateProvider(client);

            // Act
            Result<IEnumerable<ExchangeRateEntity>> result = await provider.GetLatestRatesAsync();

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be(EcbRateProvider.XmlParsingError.Code);
        }

        [Fact]
        public async Task GetLatestRatesAsync_Should_Skip_Cubes_With_Missing_Attributes()
        {
            // Arrange
            EcbRateProviderTestHelper.ResetCache();

            string xml = """
            <gesmes:Envelope xmlns:gesmes="http://www.gesmes.org/xml/2002-08-01"
                             xmlns="http://www.ecb.int/vocabulary/2002-08-01/eurofxref">
              <Cube>
                <Cube time="2024-05-07">
                  <Cube Currency="USD" rate="1.1" />
                  <Cube rate="1.2" /> <!-- Missing Currency -->
                  <Cube Currency="GBP" /> <!-- Missing rate -->
                  <Cube Currency="" rate="1.3" /> <!-- Empty Currency -->
                  <Cube Currency="JPY" rate="" /> <!-- Empty rate -->
                </Cube>
              </Cube>
            </gesmes:Envelope>
            """;

            HttpClient client = CreateMockHttpClient(HttpStatusCode.OK, xml);
            EcbRateProvider provider = new EcbRateProvider(client);

            // Act
            Result<IEnumerable<ExchangeRateEntity>> result = await provider.GetLatestRatesAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().ContainSingle();
            result.Value.First().QuoteCurrency.Code.Should().Be("USD");
        }

        private static HttpClient CreateMockHttpClient(HttpStatusCode statusCode, string content)
        {
            Mock<HttpMessageHandler> handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content, Encoding.UTF8, "application/xml")
                });

            return new HttpClient(handler.Object);
        }
    }
}