using MimesisPlayerEnhancement.Features.Weather;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Weather
{
    public sealed class WeatherPresetListParserTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ParseOrderedPresets_returns_empty_for_blank_input(string? csv)
        {
            List<string> presets = WeatherPresetListParser.ParseOrderedPresets(csv);

            Assert.Empty(presets);
        }

        [Fact]
        public void ParseOrderedPresets_trims_whitespace_and_preserves_order()
        {
            List<string> presets = WeatherPresetListParser.ParseOrderedPresets(" Sunny , Rain , HeavyRain ");

            Assert.Equal(["Sunny", "Rain", "HeavyRain"], presets);
        }

        [Fact]
        public void ParseOrderedPresets_deduplicates_case_insensitively()
        {
            List<string> presets = WeatherPresetListParser.ParseOrderedPresets("Sunny,sunny,Rain");

            Assert.Equal(["Sunny", "Rain"], presets);
        }

        [Fact]
        public void ParseOrderedPresets_ignores_empty_csv_segments()
        {
            List<string> presets = WeatherPresetListParser.ParseOrderedPresets("Sunny,,Rain,  ,");

            Assert.Equal(["Sunny", "Rain"], presets);
        }
    }
}
