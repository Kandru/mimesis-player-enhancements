using MelonLoader;

namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_DungeonTime] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class DungeonTimeConfig
    {
        private static MelonPreferences_Category _category = null!;

        private static readonly string[] ValidStartTimePresets =
            ["Vanilla", "Morning", "Noon", "Dusk", "Night", "Midnight"];

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_DungeonTime");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableDungeonTime = ModConfig.CreateTrackedEntry(_category,
                "EnableDungeonTime",
                false);

            ModConfig.DungeonTimeBaselinePlayerCount = ModConfig.CreateTrackedEntry(_category,
                "DungeonTimeBaselinePlayerCount",
                4);

            ModConfig.ExtraShiftSecondsPerPlayerAboveBaseline = ModConfig.CreateTrackedEntry(_category,
                "ExtraShiftSecondsPerPlayerAboveBaseline",
                10f);

            ModConfig.StartTimePreset = ModConfig.CreateTrackedEntry(_category,
                "StartTimePreset",
                "Vanilla");

            ModConfig.TimeMultiplier = ModConfig.CreateTrackedEntry(_category,
                "TimeMultiplier",
                1f);

            ModConfig.EnableRealtimeTramClock = ModConfig.CreateTrackedEntry(_category,
                "EnableRealtimeTramClock",
                false);
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableDungeonTime.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableDungeonTime));
            ModConfig.DungeonTimeBaselinePlayerCount.OnEntryValueChanged.Subscribe((_, value) =>
            {
                if (value < 1)
                {
                    logger.Warning("DungeonTimeBaselinePlayerCount must be at least 1; resetting to 1.");
                    ModConfig.DungeonTimeBaselinePlayerCount.Value = 1;
                    return;
                }

                ModConfig.NotifyChanged(ModConfig.DungeonTimeBaselinePlayerCount);
            });
            ModConfig.ExtraShiftSecondsPerPlayerAboveBaseline.OnEntryValueChanged.Subscribe((_, value) =>
                OnExtraShiftSecondsPerPlayerChanged(logger, value));
            ModConfig.StartTimePreset.OnEntryValueChanged.Subscribe((_, value) => OnStartTimePresetChanged(logger, value));
            ModConfig.TimeMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnTimeMultiplierChanged(logger, value));
            ModConfig.EnableRealtimeTramClock.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableRealtimeTramClock));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.ExtraShiftSecondsPerPlayerAboveBaseline);
            ModConfig.TrackFloatEntry(ModConfig.TimeMultiplier);
        }

        internal static void SanitizeInitialValues(MelonLogger.Instance logger)
        {
            OnStartTimePresetChanged(logger, ModConfig.StartTimePreset.Value);
            OnTimeMultiplierChanged(logger, ModConfig.TimeMultiplier.Value);
        }

        private static void OnExtraShiftSecondsPerPlayerChanged(MelonLogger.Instance logger, float value)
        {
            if (value < 0f)
            {
                logger.Warning("ExtraShiftSecondsPerPlayerAboveBaseline must be >= 0; resetting to 0.");
                ModConfig.ExtraShiftSecondsPerPlayerAboveBaseline.Value = 0f;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(ModConfig.ExtraShiftSecondsPerPlayerAboveBaseline);
            ModConfig.NotifyChanged(ModConfig.ExtraShiftSecondsPerPlayerAboveBaseline);
        }

        private static void OnTimeMultiplierChanged(MelonLogger.Instance logger, float value)
        {
            float clamped = DungeonTimeResolver.ClampTimeMultiplier(value);
            if (!clamped.Equals(value))
            {
                logger.Warning(
                    $"TimeMultiplier must be between {DungeonTimeResolver.MinTimeMultiplier} and {DungeonTimeResolver.MaxTimeMultiplier}; clamping.");
                ModConfig.TimeMultiplier.Value = clamped;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(ModConfig.TimeMultiplier);
            ModConfig.NotifyChanged(ModConfig.TimeMultiplier);
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
