using MelonLoader;

namespace MimesisPlayerEnhancement.Features.Weather
{
    internal static class WeatherConfig
    {
        private static MelonPreferences_Category _category = null!;

        private static readonly string[] ValidWeatherModes = ["Vanilla", "Fixed", "Cycle"];
        private static readonly string[] ValidWeatherPresets = ["Sunny", "Rain", "HeavyRain", "Squall"];
        private static readonly string[] ValidStartTimePresets =
            ["Vanilla", "Morning", "Noon", "Dusk", "Night", "Midnight"];

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_Weather");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableWeather = ModConfig.CreateTrackedEntry(_category,
                "EnableWeather",
                false);

            ModConfig.WeatherMode = ModConfig.CreateTrackedEntry(_category,
                "WeatherMode",
                "Vanilla");

            ModConfig.FixedWeatherPreset = ModConfig.CreateTrackedEntry(_category,
                "FixedWeatherPreset",
                "Sunny");

            ModConfig.DisableRandomWeather = ModConfig.CreateTrackedEntry(_category,
                "DisableRandomWeather",
                false);

            ModConfig.WeatherCyclePresets = ModConfig.CreateTrackedEntry(_category,
                "WeatherCyclePresets",
                "Sunny,Rain");

            ModConfig.WeatherCycleMinDelaySeconds = ModConfig.CreateTrackedEntry(_category,
                "WeatherCycleMinDelaySeconds",
                300f);

            ModConfig.WeatherCycleMaxDelaySeconds = ModConfig.CreateTrackedEntry(_category,
                "WeatherCycleMaxDelaySeconds",
                600f);

            ModConfig.StartTimePreset = ModConfig.CreateTrackedEntry(_category,
                "StartTimePreset",
                "Vanilla");

            ModConfig.EnableRealtimeTramClock = ModConfig.CreateTrackedEntry(_category,
                "EnableRealtimeTramClock",
                false);
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableWeather.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableWeather));
            ModConfig.WeatherMode.OnEntryValueChanged.Subscribe((_, value) => OnWeatherModeChanged(logger, value));
            ModConfig.FixedWeatherPreset.OnEntryValueChanged.Subscribe((_, value) => OnFixedWeatherPresetChanged(logger, value));
            ModConfig.DisableRandomWeather.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.DisableRandomWeather));
            ModConfig.WeatherCyclePresets.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.WeatherCyclePresets));
            ModConfig.WeatherCycleMinDelaySeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnCycleMinDelayChanged(logger, value));
            ModConfig.WeatherCycleMaxDelaySeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnCycleMaxDelayChanged(logger, value));
            ModConfig.StartTimePreset.OnEntryValueChanged.Subscribe((_, value) => OnStartTimePresetChanged(logger, value));
            ModConfig.EnableRealtimeTramClock.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableRealtimeTramClock));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.WeatherCycleMinDelaySeconds);
            ModConfig.TrackFloatEntry(ModConfig.WeatherCycleMaxDelaySeconds);
        }

        internal static void SanitizeInitialValues(MelonLogger.Instance logger)
        {
            OnWeatherModeChanged(logger, ModConfig.WeatherMode.Value);
            OnFixedWeatherPresetChanged(logger, ModConfig.FixedWeatherPreset.Value);
            OnStartTimePresetChanged(logger, ModConfig.StartTimePreset.Value);
            OnCycleMinDelayChanged(logger, ModConfig.WeatherCycleMinDelaySeconds.Value);
            OnCycleMaxDelayChanged(logger, ModConfig.WeatherCycleMaxDelaySeconds.Value);
        }

        private static void OnWeatherModeChanged(MelonLogger.Instance logger, string value)
        {
            if (!ContainsIgnoreCase(ValidWeatherModes, value))
            {
                logger.Warning("WeatherMode must be Vanilla, Fixed, or Cycle; resetting to Vanilla.");
                ModConfig.WeatherMode.Value = "Vanilla";
                return;
            }

            ModConfig.NotifyChanged(ModConfig.WeatherMode);
        }

        private static void OnFixedWeatherPresetChanged(MelonLogger.Instance logger, string value)
        {
            if (!ContainsIgnoreCase(ValidWeatherPresets, value))
            {
                logger.Warning("FixedWeatherPreset must be Sunny, Rain, HeavyRain, or Squall; resetting to Sunny.");
                ModConfig.FixedWeatherPreset.Value = "Sunny";
                return;
            }

            ModConfig.NotifyChanged(ModConfig.FixedWeatherPreset);
        }

        private static void OnStartTimePresetChanged(MelonLogger.Instance logger, string value)
        {
            if (!ContainsIgnoreCase(ValidStartTimePresets, value))
            {
                logger.Warning("StartTimePreset must be Vanilla, Morning, Noon, Dusk, Night, or Midnight; resetting to Vanilla.");
                ModConfig.StartTimePreset.Value = "Vanilla";
                return;
            }

            ModConfig.NotifyChanged(ModConfig.StartTimePreset);
        }

        private static void OnCycleMinDelayChanged(MelonLogger.Instance logger, float value)
        {
            if (value < 0f)
            {
                logger.Warning("WeatherCycleMinDelaySeconds must be >= 0; resetting to 0.");
                ModConfig.WeatherCycleMinDelaySeconds.Value = 0f;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(ModConfig.WeatherCycleMinDelaySeconds);
            if (ModConfig.WeatherCycleMaxDelaySeconds.Value < value)
            {
                ModConfig.WeatherCycleMaxDelaySeconds.Value = value;
            }

            ModConfig.NotifyChanged(ModConfig.WeatherCycleMinDelaySeconds);
        }

        private static void OnCycleMaxDelayChanged(MelonLogger.Instance logger, float value)
        {
            if (value < ModConfig.WeatherCycleMinDelaySeconds.Value)
            {
                logger.Warning("WeatherCycleMaxDelaySeconds must be >= min delay; resetting to min.");
                ModConfig.WeatherCycleMaxDelaySeconds.Value = ModConfig.WeatherCycleMinDelaySeconds.Value;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(ModConfig.WeatherCycleMaxDelaySeconds);
            ModConfig.NotifyChanged(ModConfig.WeatherCycleMaxDelaySeconds);
        }

        private static bool ContainsIgnoreCase(string[] values, string? candidate)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (string.Equals(values[i], candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
