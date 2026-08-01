namespace MimesisPlayerEnhancement.Features.Weather
{
    internal readonly struct WeatherSceneConfig
    {
        internal WeatherSceneConfig(
            bool enableWeather,
            WeatherMode mode,
            bool disableRandomWeather,
            float cycleMinDelaySeconds,
            float cycleMaxDelaySeconds)
        {
            EnableWeather = enableWeather;
            Mode = mode;
            DisableRandomWeather = disableRandomWeather;
            CycleMinDelaySeconds = cycleMinDelaySeconds;
            CycleMaxDelaySeconds = cycleMaxDelaySeconds;
        }

        internal bool EnableWeather { get; }

        internal WeatherMode Mode { get; }

        internal bool DisableRandomWeather { get; }

        internal float CycleMinDelaySeconds { get; }

        internal float CycleMaxDelaySeconds { get; }

        internal static WeatherSceneConfig CaptureFromModConfig()
        {
            return new WeatherSceneConfig(
                ModConfig.EnableWeather.Value,
                WeatherResolver.ParseMode(ModConfig.WeatherMode.Value),
                ModConfig.DisableRandomWeather.Value,
                ModConfig.WeatherCycleMinDelaySeconds.Value,
                ModConfig.WeatherCycleMaxDelaySeconds.Value);
        }
    }
}
