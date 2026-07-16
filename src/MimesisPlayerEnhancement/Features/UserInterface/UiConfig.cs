using MelonLoader;

namespace MimesisPlayerEnhancement.Features.UserInterface
{
    internal static class UiConfig
    {
        internal const string SectionId = "MimesisPlayerEnhancement_Ui";

        private const float MinFloatingDamageDurationSeconds = 1f;
        private const float MaxFloatingDamageDurationSeconds = 3f;

        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory(SectionId);
        }

        internal static void CreateEntries()
        {
            ModConfig.ModToastDurationSeconds = ModConfig.CreateTrackedEntry(_category,
                "ModToastDurationSeconds",
                5f);

            ModConfig.EnableExtendedSaveSlots = ModConfig.CreateTrackedEntry(_category,
                "EnableExtendedSaveSlots",
                true);

            ModConfig.EnableExtendedSpectatorPlayerList = ModConfig.CreateTrackedEntry(_category,
                "EnableExtendedSpectatorPlayerList",
                true);

            ModConfig.EnableExtendedInGameMenuPlayerList = ModConfig.CreateTrackedEntry(_category,
                "EnableExtendedInGameMenuPlayerList",
                true);

            ModConfig.EnableDamageHealthGlow = ModConfig.CreateTrackedEntry(_category,
                "EnableDamageHealthGlow",
                true);

            ModConfig.EnableFloatingDamageNumbers = ModConfig.CreateTrackedEntry(_category,
                "EnableFloatingDamageNumbers",
                true);

            ModConfig.FloatingDamageDurationSeconds = ModConfig.CreateTrackedEntry(_category,
                "FloatingDamageDurationSeconds",
                2f);

            ModConfig.EnableFpsUi = ModConfig.CreateTrackedEntry(_category,
                "EnableFpsUi",
                true);

            ModConfig.EnableFpsUiInventoryNetWorth = ModConfig.CreateTrackedEntry(_category,
                "EnableFpsUiInventoryNetWorth",
                true);
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.ModToastDurationSeconds.OnEntryValueChanged.Subscribe((_, value) =>
            {
                if (value < 1f)
                {
                    logger.Warning("ModToastDurationSeconds must be at least 1; resetting to 1.");
                    ModConfig.ModToastDurationSeconds.Value = 1f;
                    return;
                }

                ModConfig.NotifyChanged(ModConfig.ModToastDurationSeconds);
            });
            ModConfig.EnableExtendedSaveSlots.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableExtendedSaveSlots));
            ModConfig.EnableExtendedSpectatorPlayerList.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableExtendedSpectatorPlayerList));
            ModConfig.EnableExtendedInGameMenuPlayerList.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableExtendedInGameMenuPlayerList));

            ModConfig.EnableDamageHealthGlow.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableDamageHealthGlow));
            ModConfig.EnableFloatingDamageNumbers.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableFloatingDamageNumbers));
            ModConfig.FloatingDamageDurationSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnFloatingDamageDurationChanged(logger, value));
            ModConfig.EnableFpsUi.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableFpsUi));
            ModConfig.EnableFpsUiInventoryNetWorth.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableFpsUiInventoryNetWorth));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.ModToastDurationSeconds);
            ModConfig.TrackFloatEntry(ModConfig.FloatingDamageDurationSeconds);
        }

        private static void OnFloatingDamageDurationChanged(MelonLogger.Instance logger, float value)
        {
            if (value < MinFloatingDamageDurationSeconds)
            {
                logger.Warning(
                    $"FloatingDamageDurationSeconds must be at least {MinFloatingDamageDurationSeconds}; resetting.");
                ModConfig.FloatingDamageDurationSeconds.Value = MinFloatingDamageDurationSeconds;
                return;
            }

            if (value > MaxFloatingDamageDurationSeconds)
            {
                logger.Warning(
                    $"FloatingDamageDurationSeconds must be at most {MaxFloatingDamageDurationSeconds}; resetting.");
                ModConfig.FloatingDamageDurationSeconds.Value = MaxFloatingDamageDurationSeconds;
                return;
            }

            ModConfig.NotifyChanged(ModConfig.FloatingDamageDurationSeconds);
        }
    }
}
