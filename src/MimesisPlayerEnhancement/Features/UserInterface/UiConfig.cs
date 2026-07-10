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
            _category = ModConfig.CreateCategory(SectionId, "User Interface");
        }

        internal static void CreateEntries()
        {
            ModConfig.ModToastDurationSeconds = ModConfig.CreateTrackedEntry(_category,
                "ModToastDurationSeconds",
                5f,
                "Mod Message Duration (seconds)",
                "How long mod messages stay visible in the bottom-left corner before fading. Vanilla join/leave connect messages are unchanged (~2 seconds). Each player controls this locally.");

            ModConfig.EnableExtendedSaveSlots = ModConfig.CreateTrackedEntry(_category,
                "EnableExtendedSaveSlots",
                true,
                "Enable Extended Save Slots",
                "When enabled, replaces the separate New/Load Tram menus with a unified save picker (up to 99 manual slots). When disabled, vanilla New/Load Tram behavior is used.");

            ModConfig.EnableExtendedSpectatorPlayerList = ModConfig.CreateTrackedEntry(_category,
                "EnableExtendedSpectatorPlayerList",
                true,
                "Enable Extended Spectator Player List",
                "Replace the 4-player spectator death list with a two-column layout that scales to screen height. Living players are shown first, then dead; each group is sorted alphabetically.");

            ModConfig.EnableWorldHealthBars = ModConfig.CreateTrackedEntry(_category,
                "EnableWorldHealthBars",
                true,
                "Enable World Health Bars",
                "Show a world-space health bar above other players, mimics, and monsters for a few seconds after they take damage. Never shown on your own avatar.");

            ModConfig.WorldHealthBarDurationSeconds = ModConfig.CreateTrackedEntry(_category,
                "WorldHealthBarDurationSeconds",
                4f,
                "World Health Bar Duration (seconds)",
                "How long the world health bar stays visible after an entity takes damage.");

            ModConfig.EnableFloatingDamageNumbers = ModConfig.CreateTrackedEntry(_category,
                "EnableFloatingDamageNumbers",
                true,
                "Enable Floating Damage Numbers",
                "Show animated floating damage numbers when other players, mimics, or monsters take damage. Never shown on your own avatar.");

            ModConfig.FloatingDamageDurationSeconds = ModConfig.CreateTrackedEntry(_category,
                "FloatingDamageDurationSeconds",
                2f,
                "Floating Damage Duration (seconds)",
                "How long floating damage and detox indicators remain visible.");

            ModConfig.EnableFloatingDetoxIndicators = ModConfig.CreateTrackedEntry(_category,
                "EnableFloatingDetoxIndicators",
                true,
                "Enable Floating Detox Indicators",
                "Show green floating toxicity reduction (e.g. -27%) when another player drinks detox juice. Duration is configured under Floating damage numbers.");
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
