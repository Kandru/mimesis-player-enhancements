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
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_PlayerAnnouncements");
        }

        internal static void CreateEntries()
        {
            ModConfig.ShowPlayerAnnouncements = ModConfig.CreateTrackedEntry(_category,
                "ShowPlayerAnnouncements",
                true);
        }

        internal static void WireValidation()
        {
            ModConfig.ShowPlayerAnnouncements.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.ShowPlayerAnnouncements));
        }
    }
}
