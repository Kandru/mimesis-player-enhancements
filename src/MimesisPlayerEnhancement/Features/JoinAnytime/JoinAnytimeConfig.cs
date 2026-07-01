using MelonLoader;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_JoinAnytime] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class JoinAnytimeConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_JoinAnytime", "Join Anytime");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableJoinAnytime = ModConfig.CreateTrackedEntry(_category,
                "EnableJoinAnytime",
                true,
                "Enable Join Anytime",
                "Allow players to join a session after it has already started.");

            ModConfig.JoinConnectionGraceSeconds = ModConfig.CreateTrackedEntry(_category,
                "JoinConnectionGraceSeconds",
                30,
                "Join Connection Grace Seconds",
                "When a player connects, block tram departure for this many seconds. Players who fail to finish loading are kicked (host is never kicked).");
        }

        /// <summary>Clamps persisted values once at startup, before change handlers are wired.</summary>
        internal static void SanitizeInitialValues(MelonLogger.Instance logger)
        {
            if (ModConfig.JoinConnectionGraceSeconds.Value < 1)
            {
                logger.Warning("JoinConnectionGraceSeconds must be at least 1; resetting to 1.");
                ModConfig.JoinConnectionGraceSeconds.Value = 1;
            }
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableJoinAnytime.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableJoinAnytime));

            ModConfig.JoinConnectionGraceSeconds.OnEntryValueChanged.Subscribe((_, value) =>
            {
                if (value < 1)
                {
                    logger.Warning("JoinConnectionGraceSeconds must be at least 1; resetting to 1.");
                    ModConfig.JoinConnectionGraceSeconds.Value = 1;
                    return;
                }

                ModConfig.NotifyChanged(ModConfig.JoinConnectionGraceSeconds);
            });
        }
    }
}
