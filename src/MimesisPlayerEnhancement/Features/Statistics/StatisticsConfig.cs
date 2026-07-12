using MelonLoader;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_Statistics] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class StatisticsConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_Statistics");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableStatistics = ModConfig.CreateTrackedEntry(_category,
                "EnableStatistics",
                true);

            ModConfig.SessionReconnectGraceMinutes = ModConfig.CreateTrackedEntry(_category,
                "SessionReconnectGraceMinutes",
                5);

            ModConfig.ShowStatisticsToasts = ModConfig.CreateTrackedEntry(_category,
                "ShowStatisticsToasts",
                true);
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.SessionReconnectGraceMinutes.OnEntryValueChanged.Subscribe((_, value) =>
            {
                if (value < 1)
                {
                    logger.Warning("SessionReconnectGraceMinutes must be at least 1; resetting to 1.");
                    ModConfig.SessionReconnectGraceMinutes.Value = 1;
                    return;
                }

                ModConfig.NotifyChanged(ModConfig.SessionReconnectGraceMinutes);
            });

            ModConfig.EnableStatistics.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableStatistics));
            ModConfig.ShowStatisticsToasts.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.ShowStatisticsToasts));
        }
    }
}
