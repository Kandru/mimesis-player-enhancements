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
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_DungeonRandomizer", "Dungeon Randomizer");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableDungeonRandomizer = ModConfig.CreateTrackedEntry(_category,
                "EnableDungeonRandomizer",
                false,
                "Enable Dungeon Randomizer",
                "Randomize dungeon selection: tram dungeon pick, layout flow, map variant, and procedural seed. Host only.");

            ModConfig.RandomizeDungeonPick = ModConfig.CreateTrackedEntry(_category,
                "RandomizeDungeonPick",
                true,
                "Randomize Dungeon Pick",
                "Override which dungeon master ID is rolled on the tram.");

            ModConfig.DungeonPickPoolMode = ModConfig.CreateTrackedEntry(_category,
                "DungeonPickPoolMode",
                "WidenVanilla",
                "Dungeon Pick Pool Mode",
                "WidenVanilla = keep cycle weights but allow repeats sooner; AllActiveUniform = pick uniformly from all active dungeons (ignores cycle table).");

            ModConfig.DungeonAllowlist = ModConfig.CreateTrackedEntry(_category,
                "DungeonAllowlist",
                "",
                "Dungeon Allowlist",
                "Comma-separated dungeon master IDs. When non-empty, only these IDs are eligible.");

            ModConfig.DungeonBlocklist = ModConfig.CreateTrackedEntry(_category,
                "DungeonBlocklist",
                "",
                "Dungeon Blocklist",
                "Comma-separated dungeon master IDs to exclude from the pool.");

            ModConfig.IgnoreDungeonExcludeList = ModConfig.CreateTrackedEntry(_category,
                "IgnoreDungeonExcludeList",
                true,
                "Ignore Dungeon Exclude List",
                "When using WidenVanilla, do not exclude recently played dungeons from the tram roll.");

            ModConfig.RandomizeLayoutFlow = ModConfig.CreateTrackedEntry(_category,
                "RandomizeLayoutFlow",
                true,
                "Randomize Layout Flow",
                "Pick DunGen layout flows uniformly from each dungeon's candidates instead of using weighted vanilla rolls.");

            ModConfig.RandomizeMapVariant = ModConfig.CreateTrackedEntry(_category,
                "RandomizeMapVariant",
                true,
                "Randomize Map Variant",
                "Pick map variants uniformly from each dungeon's MapIDs instead of vanilla selection.");

            ModConfig.RandomizeDungeonSeed = ModConfig.CreateTrackedEntry(_category,
                "RandomizeDungeonSeed",
                true,
                "Randomize Dungeon Seed",
                "Replace the procedural dungeon seed with a new random value when a dungeon is chosen.");
        }

        /// <summary>Clamps persisted values once at startup, before change handlers are wired.</summary>
        internal static void SanitizeInitialValues(MelonLogger.Instance logger)
        {
            OnDungeonPickPoolModeChanged(logger, ModConfig.DungeonPickPoolMode.Value);
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableDungeonRandomizer.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableDungeonRandomizer));
            ModConfig.RandomizeDungeonPick.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.RandomizeDungeonPick));
            ModConfig.DungeonPickPoolMode.OnEntryValueChanged.Subscribe((_, value) => OnDungeonPickPoolModeChanged(logger, value));
            ModConfig.DungeonAllowlist.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.DungeonAllowlist));
            ModConfig.DungeonBlocklist.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.DungeonBlocklist));
            ModConfig.IgnoreDungeonExcludeList.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.IgnoreDungeonExcludeList));
            ModConfig.RandomizeLayoutFlow.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.RandomizeLayoutFlow));
            ModConfig.RandomizeMapVariant.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.RandomizeMapVariant));
            ModConfig.RandomizeDungeonSeed.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.RandomizeDungeonSeed));
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
