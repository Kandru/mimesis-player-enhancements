using MelonLoader;

namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_DungeonRandomizer] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class DungeonRandomizerConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_DungeonRandomizer");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableDungeonRandomizer = ModConfig.CreateTrackedEntry(_category,
                "EnableDungeonRandomizer",
                false);

            ModConfig.RandomizeDungeonPick = ModConfig.CreateTrackedEntry(_category,
                "RandomizeDungeonPick",
                true);

            ModConfig.DungeonPickPoolMode = ModConfig.CreateTrackedEntry(_category,
                "DungeonPickPoolMode",
                "WidenVanilla");

            ModConfig.DungeonAllowlist = ModConfig.CreateTrackedEntry(_category,
                "DungeonAllowlist",
                "");

            ModConfig.DungeonBlocklist = ModConfig.CreateTrackedEntry(_category,
                "DungeonBlocklist",
                "");

            ModConfig.IgnoreDungeonExcludeList = ModConfig.CreateTrackedEntry(_category,
                "IgnoreDungeonExcludeList",
                true);

            ModConfig.RandomizeMapVariant = ModConfig.CreateTrackedEntry(_category,
                "RandomizeMapVariant",
                true);

            ModConfig.DungeonSeedFlavor = ModConfig.CreateTrackedEntry(_category,
                "DungeonSeedFlavor",
                "Vanilla");
        }

        /// <summary>Clamps persisted values once at startup, before change handlers are wired.</summary>
        internal static void SanitizeInitialValues(MelonLogger.Instance logger)
        {
            OnDungeonPickPoolModeChanged(logger, ModConfig.DungeonPickPoolMode.Value);
            OnDungeonSeedFlavorChanged(logger, ModConfig.DungeonSeedFlavor.Value);
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableDungeonRandomizer.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableDungeonRandomizer));
            ModConfig.RandomizeDungeonPick.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.RandomizeDungeonPick));
            ModConfig.DungeonPickPoolMode.OnEntryValueChanged.Subscribe((_, value) => OnDungeonPickPoolModeChanged(logger, value));
            ModConfig.DungeonAllowlist.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.DungeonAllowlist));
            ModConfig.DungeonBlocklist.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.DungeonBlocklist));
            ModConfig.IgnoreDungeonExcludeList.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.IgnoreDungeonExcludeList));
            ModConfig.RandomizeMapVariant.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.RandomizeMapVariant));
            ModConfig.DungeonSeedFlavor.OnEntryValueChanged.Subscribe((_, value) => OnDungeonSeedFlavorChanged(logger, value));
        }

        private static void OnDungeonSeedFlavorChanged(MelonLogger.Instance logger, string value)
        {
            if (!DungeonSeedFlavorUtil.TryParse(value, out _))
            {
                logger.Warning($"DungeonSeedFlavor must be a known flavor name; resetting to Vanilla.");
                ModConfig.DungeonSeedFlavor.Value = "Vanilla";
                return;
            }

            ModConfig.NotifyChanged(ModConfig.DungeonSeedFlavor);
        }

        private static void OnDungeonPickPoolModeChanged(MelonLogger.Instance logger, string value)
        {
            if (!string.Equals(value, "WidenVanilla", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "AllActiveUniform", StringComparison.OrdinalIgnoreCase))
            {
                logger.Warning("DungeonPickPoolMode must be WidenVanilla or AllActiveUniform; resetting to WidenVanilla.");
                ModConfig.DungeonPickPoolMode.Value = "WidenVanilla";
                return;
            }

            ModConfig.NotifyChanged(ModConfig.DungeonPickPoolMode);
        }
    }
}
