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
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_Statistics", "Statistics");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableStatistics = ModConfig.CreateTrackedEntry(_category,
                "EnableStatistics",
                true,
                "Enable Player Statistics",
                "Track per-session and global player statistics per save slot.");

            ModConfig.SessionReconnectGraceMinutes = ModConfig.CreateTrackedEntry(_category,
                "SessionReconnectGraceMinutes",
                5,
                "Session Reconnect Grace (minutes)",
                "Reuse the previous session when a player reconnects within this many minutes.");

            ModConfig.ShowStatisticsToasts = ModConfig.CreateTrackedEntry(_category,
                "ShowStatisticsToasts",
                true,
                "Show Statistics Toasts",
                "Show mod stats toasts in plain English (session intro for you, global stats on join/leave). Does not replace the game's own connect messages.");
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
