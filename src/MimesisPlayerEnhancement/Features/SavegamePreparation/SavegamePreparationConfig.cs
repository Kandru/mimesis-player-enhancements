using MelonLoader;

namespace MimesisPlayerEnhancement.Features.SavegamePreparation
{
    /// <summary>
    /// Registers the global-only [MimesisPlayerEnhancement_SavegamePreparation] section.
    /// Values apply only when creating a new savegame.
    /// </summary>
    internal static class SavegamePreparationConfig
    {
        internal const string SectionId = "MimesisPlayerEnhancement_SavegamePreparation";

        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory(SectionId);
        }

        internal static void CreateEntries()
        {
            ModConfig.AutoScaleStartupMoneyByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleStartupMoneyByPlayerCount",
                true);

            ModConfig.StartupMoneyMultiplier = ModConfig.CreateTrackedEntry(_category,
                "StartupMoneyMultiplier",
                1f);

            ModConfig.StartingZone = ModConfig.CreateTrackedEntry(_category,
                "StartingZone",
                1);
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.AutoScaleStartupMoneyByPlayerCount.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.AutoScaleStartupMoneyByPlayerCount));
            ModConfig.StartupMoneyMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.StartupMoneyMultiplier));
            ModConfig.StartingZone.OnEntryValueChanged.Subscribe((_, value) =>
                OnStartingZoneChanged(logger, value));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.StartupMoneyMultiplier);
        }

        private static void OnStartingZoneChanged(MelonLogger.Instance logger, int value)
        {
            if (value < 1)
            {
                logger.Warning("StartingZone must be at least 1; resetting to 1.");
                ModConfig.StartingZone.Value = 1;
                return;
            }

            ModConfig.NotifyChanged(ModConfig.StartingZone);
        }
    }
}
