using MelonLoader;

namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_PlayerAnnouncements] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class PlayerAnnouncementsConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_PlayerAnnouncements", "Player Announcements");
        }

        internal static void CreateEntries()
        {
            ModConfig.ShowPlayerAnnouncements = ModConfig.CreateTrackedEntry(_category,
                "ShowPlayerAnnouncements",
                true,
                "Show Player Messages",
                "Show player messages in the bottom-left corner for dungeon run settings, boss spawns, and your per-map stats when you die. Does not replace the game's own messages.");
        }

        internal static void WireValidation()
        {
            ModConfig.ShowPlayerAnnouncements.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.ShowPlayerAnnouncements));
        }
    }
}
