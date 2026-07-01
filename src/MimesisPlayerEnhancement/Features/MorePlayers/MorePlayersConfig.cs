using MelonLoader;

namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_MorePlayers] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class MorePlayersConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_MorePlayers", "More Players");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableMorePlayers = ModConfig.CreateTrackedEntry(_category,
                "EnableMorePlayers",
                false,
                "Enable More Players",
                "Raise the multiplayer player cap above 4.");

            ModConfig.MaxPlayers = ModConfig.CreateTrackedEntry(_category,
                "MaxPlayers",
                32,
                "Max Players",
                "Maximum players in a session including the host (1 = solo, 2 = host + 1 client, etc.).");
        }

        /// <summary>Clamps persisted values once at startup, before change handlers are wired.</summary>
        internal static void SanitizeInitialValues(MelonLogger.Instance logger)
        {
            if (ModConfig.MaxPlayers.Value < 1)
            {
                logger.Warning("MaxPlayers must be at least 1; resetting to 1.");
                ModConfig.MaxPlayers.Value = 1;
            }
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.MaxPlayers.OnEntryValueChanged.Subscribe((_, value) =>
            {
                if (value < 1)
                {
                    logger.Warning("MaxPlayers must be at least 1; resetting to 1.");
                    ModConfig.MaxPlayers.Value = 1;
                    return;
                }

                ModConfig.NotifyChanged(ModConfig.MaxPlayers);
            });

            ModConfig.EnableMorePlayers.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableMorePlayers));
        }
    }
}
