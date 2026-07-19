namespace MimesisPlayerEnhancement.Features.Weather
{
    internal static class WeatherResolver
    {
        internal static bool IsFeatureEnabled =>
            ModConfig.EnableWeather.Value;

        internal static WeatherMode GetMode() => GetMode(WeatherSceneConfig.CaptureFromModConfig());

        internal static WeatherMode GetMode(WeatherSceneConfig config)
        {
            if (!config.EnableWeather)
            {
                return WeatherMode.Vanilla;
            }

            return config.Mode;
        }

        internal static bool ShouldStripRandomWeather() =>
            ShouldStripRandomWeather(WeatherSceneConfig.CaptureFromModConfig());

        internal static bool ShouldStripRandomWeather(WeatherSceneConfig config) =>
            config.EnableWeather
            && config.Mode == WeatherMode.Vanilla
            && config.DisableRandomWeather;

        internal static bool TryGetFixedWeatherMasterId(out int masterId)
        {
            masterId = 0;
            if (!IsFeatureEnabled || GetMode() != WeatherMode.Fixed)
            {
                return false;
            }

            return TryResolvePresetMasterId(ModConfig.FixedWeatherPreset.Value, out masterId);
        }

        internal static bool TryResolvePresetMasterId(string? presetName, out int masterId)
        {
            masterId = 0;
            if (!TryParsePresetName(presetName, out SkyAndWeatherSystem.eWeatherPreset preset))
            {
                return false;
            }

            ExcelDataManager? excel = HubGameDataAccess.Excel;
            if (excel == null)
            {
                return false;
            }

            foreach (System.Collections.Generic.KeyValuePair<int, WeatherInfo> entry in excel.Weathers)
            {
                if (entry.Value.WeatherPreset == preset)
                {
                    masterId = entry.Key;
                    return true;
                }
            }

            return false;
        }

        internal static bool TryParsePresetName(string? value, out SkyAndWeatherSystem.eWeatherPreset preset)
        {
            preset = SkyAndWeatherSystem.eWeatherPreset.Sunny;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return Enum.TryParse(value.Trim(), ignoreCase: true, out preset);
        }

        internal static WeatherMode ParseMode(string? value)
        {
            if (string.Equals(value, "Fixed", StringComparison.OrdinalIgnoreCase))
            {
                return WeatherMode.Fixed;
            }

            if (string.Equals(value, "Cycle", StringComparison.OrdinalIgnoreCase))
            {
                return WeatherMode.Cycle;
            }

            return WeatherMode.Vanilla;
        }

        internal static void GetCycleDelayRange(out float minSeconds, out float maxSeconds) =>
            GetCycleDelayRange(WeatherSceneConfig.CaptureFromModConfig(), out minSeconds, out maxSeconds);

        internal static void GetCycleDelayRange(WeatherSceneConfig config, out float minSeconds, out float maxSeconds)
        {
            minSeconds = Math.Max(0f, config.CycleMinDelaySeconds);
            maxSeconds = Math.Max(minSeconds, config.CycleMaxDelaySeconds);
        }
    }
}
