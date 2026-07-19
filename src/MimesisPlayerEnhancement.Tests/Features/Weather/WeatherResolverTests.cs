using MimesisPlayerEnhancement.Features.Weather;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Weather
{
    public sealed class WeatherResolverTests
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
        [InlineData("Fixed", 1)]
        [InlineData("fixed", 1)]
        [InlineData("Cycle", 2)]
        [InlineData("cycle", 2)]
        [InlineData("Vanilla", 0)]
        [InlineData(null, 0)]
        [InlineData("bogus", 0)]
        public void ParseMode_maps_known_values(string? value, int expectedModeValue)
        {
            WeatherMode mode = WeatherResolver.ParseMode(value);

            Assert.Equal((WeatherMode)expectedModeValue, mode);
        }

        [Theory]
        [InlineData("Sunny", true)]
        [InlineData("heavyrain", true)]
        [InlineData(" Squall ", true)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData(null, false)]
        [InlineData("Typhoon", false)]
        public void TryParsePresetName_accepts_known_presets(string? value, bool expectedSuccess)
        {
            bool success = WeatherResolver.TryParsePresetName(value, out SkyAndWeatherSystem.eWeatherPreset preset);

            Assert.Equal(expectedSuccess, success);
            if (expectedSuccess && value != null)
            {
                Assert.Contains(value.Trim(), preset.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void GetMode_returns_Vanilla_when_feature_disabled()
        {
            WeatherSceneConfig config = Config(enabled: false, mode: WeatherMode.Fixed);

            WeatherMode mode = WeatherResolver.GetMode(config);

            Assert.Equal(WeatherMode.Vanilla, mode);
        }

        [Theory]
        [InlineData(1)] // Fixed
        [InlineData(2)] // Cycle
        public void GetMode_returns_configured_mode_when_enabled(int modeValue)
        {
            var expectedMode = (WeatherMode)modeValue;
            WeatherSceneConfig config = Config(enabled: true, mode: expectedMode);

            WeatherMode mode = WeatherResolver.GetMode(config);

            Assert.Equal(expectedMode, mode);
        }

        [Theory]
        [InlineData(true, 0, true, true)]   // Vanilla
        [InlineData(true, 1, true, false)]  // Fixed
        [InlineData(true, 0, false, false)] // Vanilla, disable off
        [InlineData(false, 0, true, false)] // disabled
        public void ShouldStripRandomWeather_requires_enabled_vanilla_and_disable_flag(
            bool enabled,
            int modeValue,
            bool disableRandomWeather,
            bool expected)
        {
            WeatherSceneConfig config = Config(enabled, (WeatherMode)modeValue, disableRandomWeather);

            bool result = WeatherResolver.ShouldStripRandomWeather(config);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetCycleDelayRange_clamps_negative_min_to_zero()
        {
            WeatherSceneConfig config = Config(cycleMinDelaySeconds: -10f, cycleMaxDelaySeconds: 100f);

            WeatherResolver.GetCycleDelayRange(config, out float minSeconds, out float maxSeconds);

            Assert.Equal(0f, minSeconds);
            Assert.Equal(100f, maxSeconds);
        }

        [Fact]
        public void GetCycleDelayRange_floors_max_to_min()
        {
            WeatherSceneConfig config = Config(cycleMinDelaySeconds: 300f, cycleMaxDelaySeconds: 100f);

            WeatherResolver.GetCycleDelayRange(config, out float minSeconds, out float maxSeconds);

            Assert.Equal(300f, minSeconds);
            Assert.Equal(300f, maxSeconds);
        }
    }
}
