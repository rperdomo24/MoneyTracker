using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MoneyTracker.Application.Services;
using System.Net;
using System.Text;

namespace MoneyTracker.Tests.Services
{
    public class TextImportServiceTests
    {
        private static AiTextImportService BuildService(HttpResponseMessage response)
        {
            var handler = new FakeHttpHandler(response);
            var http = new HttpClient(handler);
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AiProvider"] = "Google",
                    ["GoogleAiSettings:ApiKey"] = "test-key",
                    ["GoogleAiSettings:Model"] = "gemini-1.5-flash",
                    ["GoogleAiSettings:MaxTokens"] = "1024"
                })
                .Build();
            return new AiTextImportService(http, config);
        }

        [Fact]
        public async Task AnalyzeAsync_WhenTextIsEmpty_ReturnsFailResult()
        {
            var service = BuildService(new HttpResponseMessage(HttpStatusCode.OK));
            var result = await service.AnalyzeAsync("   ");
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Text is empty.");
        }

        [Fact]
        public async Task AnalyzeAsync_WhenAiReturnsValidTransaction_ReturnsOneParsedItem()
        {
            var fakeJson = """
                {
                  "candidates": [{
                    "content": {
                      "parts": [{
                        "text": "{\"items\":[{\"type\":\"Expense\",\"amount\":60.00,\"currency\":\"USD\",\"dateLocal\":\"2026-05-23T12:13:00\",\"merchant\":\"CUSCATLAN\",\"description\":\"Debito cuenta\",\"provider\":\"CUSCATLAN\",\"accountHint\":\"0488\",\"confidence\":0.95,\"warnings\":[]}]}"
                      }]
                    }
                  }]
                }
                """;

            var service = BuildService(OkResponse(fakeJson));
            var result = await service.AnalyzeAsync("Se hizo un Debito en su Cuenta B. CUSCATLAN 0488 por USD60.00 el dia 2026-05-23 12:13.");

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().ContainSingle();
            result.Data.Items[0].Amount.Should().Be(60.00m);
            result.Data.Items[0].AccountHint.Should().Be("0488");
            result.Data.Items[0].Confidence.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task AnalyzeAsync_WhenAiReturnsEmptyItems_ReturnsFailResult()
        {
            var fakeJson = """
                {
                  "candidates": [{
                    "content": {
                      "parts": [{ "text": "{\"items\":[]}" }]
                    }
                  }]
                }
                """;

            var service = BuildService(OkResponse(fakeJson));
            var result = await service.AnalyzeAsync("Por tu seguridad ingresa este codigo: 884436");

            result.Success.Should().BeFalse();
            result.Message.Should().Be("No recognizable transaction was found.");
        }

        private static HttpResponseMessage OkResponse(string json)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

        private sealed class FakeHttpHandler(HttpResponseMessage response) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
                => Task.FromResult(response);
        }
    }
}
