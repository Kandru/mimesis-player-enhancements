using MelonLoader;

namespace MimesisPlayerEnhancement.Features.UserInterface
{
    internal static class UiConfig
    {
        internal const string SectionId = "MimesisPlayerEnhancement_Ui";

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
                false,
                "Enable Extended Spectator Player List",
                "Replace the 4-player spectator death list with a two-column layout that scales to screen height. Living players are shown first when space is limited; among dead players, speakers are prioritized.");
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
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.ModToastDurationSeconds);
        }
    }
}
