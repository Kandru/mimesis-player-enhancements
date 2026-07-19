using MimesisPlayerEnhancement.Features.Weather;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Weather
{
    public sealed class WeatherTimeResolverTests
    {
        private static WeatherSceneConfig Config(
            bool enabled = true,
            WeatherMode mode = WeatherMode.Vanilla,
            bool disableRandomWeather = false,
            StartTimePreset startTimePreset = StartTimePreset.Vanilla,
            float cycleMinDelaySeconds = 300f,
            float cycleMaxDelaySeconds = 600f) =>
            new(
                enabled,
                mode,
                disableRandomWeather,
                startTimePreset,
                cycleMinDelaySeconds,
                cycleMaxDelaySeconds);

        [Theory]
        [InlineData("Vanilla", 0)]
        [InlineData("vanilla", 0)]
        [InlineData(null, 0)]
        [InlineData("Morning", 1)]
        [InlineData("noon", 2)]
        [InlineData("Dusk", 3)]
        [InlineData("Night", 4)]
        [InlineData("Midnight", 5)]
        [InlineData("bogus", 0)]
        public void ParseStartTimePreset_maps_known_values(string? value, int expectedPresetValue)
        {
            StartTimePreset preset = WeatherTimeResolver.ParseStartTimePreset(value);

            Assert.Equal((StartTimePreset)expectedPresetValue, preset);
        }

        [Theory]
        [InlineData(1, 8, true)]   // Morning
        [InlineData(2, 12, true)]  // Noon
        [InlineData(3, 18, true)]  // Dusk
        [InlineData(4, 21, true)]  // Night
        [InlineData(5, 0, true)]   // Midnight
        [InlineData(0, -1, false)] // Vanilla
        public void TryGetPresetHour_maps_presets_to_hours(int presetValue, int expectedHour, bool expectedSuccess)
        {
            var preset = (StartTimePreset)presetValue;
            bool success = WeatherTimeResolver.TryGetPresetHour(preset, out int hour);

            Assert.Equal(expectedSuccess, success);
            Assert.Equal(expectedHour, hour);
        }

        [Theory]
        [InlineData(false, 1, false)]
        [InlineData(true, 0, false)]
        [InlineData(true, 1, true)]
        [InlineData(true, 5, true)]
        public void UsesOverrideStartTime_requires_enabled_non_vanilla_preset(
            bool enabled,
            int startTimePresetValue,
            bool expected)
        {
            WeatherSceneConfig config = Config(enabled: enabled, startTimePreset: (StartTimePreset)startTimePresetValue);

            bool result = WeatherTimeResolver.UsesOverrideStartTime(config);

            Assert.Equal(expected, result);
        }
    }
}
