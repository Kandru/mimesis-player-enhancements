using MelonLoader;

namespace MimesisPlayerEnhancement.Features.UserInterface
{
    internal static class UiConfig
    {
        internal const string SectionId = "MimesisPlayerEnhancement_Ui";

        private const float MinWorldHealthBarDurationSeconds = 1f;
        private const float MaxWorldHealthBarDurationSeconds = 5f;
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

            ModConfig.EnableWorldHealthBars = ModConfig.CreateTrackedEntry(_category,
                "EnableWorldHealthBars",
                true);

            ModConfig.WorldHealthBarDurationSeconds = ModConfig.CreateTrackedEntry(_category,
                "WorldHealthBarDurationSeconds",
                4f);

            ModConfig.EnableFloatingDamageNumbers = ModConfig.CreateTrackedEntry(_category,
                "EnableFloatingDamageNumbers",
                true);

            ModConfig.FloatingDamageDurationSeconds = ModConfig.CreateTrackedEntry(_category,
                "FloatingDamageDurationSeconds",
                2f);

            ModConfig.EnableFloatingDetoxIndicators = ModConfig.CreateTrackedEntry(_category,
                "EnableFloatingDetoxIndicators",
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

            ModConfig.EnableWorldHealthBars.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableWorldHealthBars));
            ModConfig.WorldHealthBarDurationSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnWorldHealthBarDurationChanged(logger, value));
            ModConfig.EnableFloatingDamageNumbers.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableFloatingDamageNumbers));
            ModConfig.FloatingDamageDurationSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnFloatingDamageDurationChanged(logger, value));
            ModConfig.EnableFloatingDetoxIndicators.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableFloatingDetoxIndicators));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.ModToastDurationSeconds);
            ModConfig.TrackFloatEntry(ModConfig.WorldHealthBarDurationSeconds);
            ModConfig.TrackFloatEntry(ModConfig.FloatingDamageDurationSeconds);
        }

        private static void OnWorldHealthBarDurationChanged(MelonLogger.Instance logger, float value)
        {
            if (value < MinWorldHealthBarDurationSeconds)
            {
                logger.Warning(
                    $"WorldHealthBarDurationSeconds must be at least {MinWorldHealthBarDurationSeconds}; resetting.");
                ModConfig.WorldHealthBarDurationSeconds.Value = MinWorldHealthBarDurationSeconds;
                return;
            }

            if (value > MaxWorldHealthBarDurationSeconds)
            {
                logger.Warning(
                    $"WorldHealthBarDurationSeconds must be at most {MaxWorldHealthBarDurationSeconds}; resetting.");
                ModConfig.WorldHealthBarDurationSeconds.Value = MaxWorldHealthBarDurationSeconds;
                return;
            }

            ModConfig.NotifyChanged(ModConfig.WorldHealthBarDurationSeconds);
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
