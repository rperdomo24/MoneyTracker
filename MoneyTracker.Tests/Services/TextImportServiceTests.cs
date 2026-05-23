using FluentAssertions;
using MoneyTracker.Application.Services;

namespace MoneyTracker.Tests.Services
{
    public class TextImportServiceTests
    {
        private readonly TextImportService _service = new();

        [Fact]
        public async Task AnalyzeAsync_WhenTextIsEmpty_ReturnsFailResult()
        {
            var result = await _service.AnalyzeAsync("   ");

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Text is empty.");
        }

        [Fact]
        public async Task AnalyzeAsync_WhenSingleLineFormatIsValid_ReturnsOneParsedItem()
        {
            // This sample should be parsed by the highest-priority single-line parser.
            var text = "ANDA PAGO AUTOMATICO ZONA 10/02/2026 $2.62";

            var result = await _service.AnalyzeAsync(text);

            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Items.Should().ContainSingle();
            result.Data.Items[0].Amount.Should().Be(2.62m);
            result.Data.Items[0].Merchant.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task AnalyzeAsync_WhenTextIsUnrecognized_ReturnsFailResult()
        {
            var result = await _service.AnalyzeAsync("random sentence without a transaction pattern");

            result.Success.Should().BeFalse();
            result.Message.Should().Be("No recognizable transaction was found.");
        }
    }
}
