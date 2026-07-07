using Bifrost.Cooked;

namespace MimesisPlayerEnhancement.Features.Weather
{
    internal static class WeatherResolver
    {
        internal static bool IsFeatureEnabled =>
            ModConfig.EnableWeather.Value;

        internal static WeatherMode GetMode()
        {
            if (!IsFeatureEnabled)
            {
                return WeatherMode.Vanilla;
            }

            return ParseMode(ModConfig.WeatherMode.Value);
        }

        internal static bool ShouldStripRandomWeather() =>
            IsFeatureEnabled
            && GetMode() == WeatherMode.Vanilla
            && ModConfig.DisableRandomWeather.Value;

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

        internal static void GetCycleDelayRange(out float minSeconds, out float maxSeconds)
        {
            minSeconds = Math.Max(0f, ModConfig.WeatherCycleMinDelaySeconds.Value);
            maxSeconds = Math.Max(minSeconds, ModConfig.WeatherCycleMaxDelaySeconds.Value);
        }
    }
}
