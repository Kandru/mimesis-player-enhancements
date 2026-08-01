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

        /// <summary>Vanilla <c>C_InitialMoney</c> / masterdata <c>INITIAL_MONEY</c>.</summary>
        internal const int DefaultStartupMoney = 120;

        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory(SectionId);
        }

        internal static void CreateEntries()
        {
            ModConfig.StartupMoney = ModConfig.CreateTrackedEntry(_category,
                "StartupMoney",
                DefaultStartupMoney);

            ModConfig.StartingZone = ModConfig.CreateTrackedEntry(_category,
                "StartingZone",
                1);

            ModConfig.EnableUpgradeTramHorn = ModConfig.CreateTrackedEntry(_category,
                "EnableUpgradeTramHorn",
                false);

            ModConfig.EnableUpgradeScrapScanner = ModConfig.CreateTrackedEntry(_category,
                "EnableUpgradeScrapScanner",
                false);

            ModConfig.EnableUpgradeTramBooster = ModConfig.CreateTrackedEntry(_category,
                "EnableUpgradeTramBooster",
                false);

            ModConfig.EnableUpgradeTramLight = ModConfig.CreateTrackedEntry(_category,
                "EnableUpgradeTramLight",
                false);
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.StartupMoney.OnEntryValueChanged.Subscribe((_, value) =>
                OnStartupMoneyChanged(logger, value));
            ModConfig.StartingZone.OnEntryValueChanged.Subscribe((_, value) =>
                OnStartingZoneChanged(logger, value));
            ModConfig.EnableUpgradeTramHorn.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableUpgradeTramHorn));
            ModConfig.EnableUpgradeScrapScanner.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableUpgradeScrapScanner));
            ModConfig.EnableUpgradeTramBooster.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableUpgradeTramBooster));
            ModConfig.EnableUpgradeTramLight.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableUpgradeTramLight));
        }

        private static void OnStartupMoneyChanged(MelonLogger.Instance logger, int value)
        {
            if (value < 0)
            {
                logger.Warning("StartupMoney must be at least 0; resetting to 0.");
                ModConfig.StartupMoney.Value = 0;
                return;
            }

            ModConfig.NotifyChanged(ModConfig.StartupMoney);
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
