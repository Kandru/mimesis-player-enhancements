using System;
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
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_Weather", "Weather");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableWeather = ModConfig.CreateTrackedEntry(_category,
                "EnableWeather",
                false,
                "Enable Weather",
                "Control dungeon weather, cyclic presets, and synced start time. Host only.");

            ModConfig.WeatherMode = ModConfig.CreateTrackedEntry(_category,
                "WeatherMode",
                "Vanilla",
                "Weather Mode",
                "Vanilla = game schedule; Fixed = one preset for the run; Cycle = rotate presets on a timer. Host only.");

            ModConfig.FixedWeatherPreset = ModConfig.CreateTrackedEntry(_category,
                "FixedWeatherPreset",
                "Sunny",
                "Fixed Weather Preset",
                "Used when Weather Mode is Fixed. Sunny, Rain, HeavyRain, or Squall.");

            ModConfig.DisableRandomWeather = ModConfig.CreateTrackedEntry(_category,
                "DisableRandomWeather",
                false,
                "Disable Random Weather",
                "Vanilla mode only — remove procedural random weather while keeping scheduled changes. Host only.");

            ModConfig.WeatherCyclePresets = ModConfig.CreateTrackedEntry(_category,
                "WeatherCyclePresets",
                "Sunny,Rain",
                "Weather Cycle Presets",
                "Cycle mode only — comma-separated preset names in order (Sunny, Rain, HeavyRain, Squall). Host only.");

            ModConfig.WeatherCycleMinDelaySeconds = ModConfig.CreateTrackedEntry(_category,
                "WeatherCycleMinDelaySeconds",
                300f,
                "Cycle Min Delay (seconds)",
                "Minimum real seconds before the next cycle step. Host only.");

            ModConfig.WeatherCycleMaxDelaySeconds = ModConfig.CreateTrackedEntry(_category,
                "WeatherCycleMaxDelaySeconds",
                600f,
                "Cycle Max Delay (seconds)",
                "Maximum real seconds before the next cycle step. Host only.");

            ModConfig.StartTimePreset = ModConfig.CreateTrackedEntry(_category,
                "StartTimePreset",
                "Vanilla",
                "Start Time Preset",
                "Synced in-game clock when the dungeon starts (lighting/sky only; real shift deadline unchanged). "
                + "Sunrise ~06:00, sunset ~18:00. Vanilla uses dungeon data (~10:00). "
                + "Vanilla ~10:00→24:00; Morning 08:00→24:00; Noon 12:00→24:00; Dusk 18:00→24:00 (sunset); "
                + "Night 21:00→24:00 (dark); Midnight 00:00→24:00 (darkest). Host only.");
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
