using MelonLoader;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_Persistence] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class PersistenceConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_Persistence", "Persistence");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnablePersistence = ModConfig.CreateTrackedEntry(_category,
                "EnablePersistence",
                true,
                "Enable Voice Persistence",
                "Save and restore mimic voice recordings across save/load.");
        }

        internal static void WireValidation()
        {
            ModConfig.EnablePersistence.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnablePersistence));
        }
    }
}
