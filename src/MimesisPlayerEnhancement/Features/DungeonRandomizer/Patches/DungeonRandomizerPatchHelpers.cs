namespace MimesisPlayerEnhancement.Features.DungeonRandomizer.Patches
{
    internal static class DungeonRandomizerPatchHelpers
    {
        internal const string Feature = "DungeonRandomizer";

        private static int _cachedVanillaSeed;
        private static int _cachedDungeonMasterId;
        private static int _cachedCuratedSeed;
        private static bool _hasSeedCache;

        internal static bool ShouldApply =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() => SceneScopedConfigGate.DungeonRandomizer.EnableDungeonRandomizer);

        internal static bool ShouldCurateSeed =>
            ShouldApply && SceneScopedConfigGate.DungeonRandomizer.SeedFlavor != DungeonSeedFlavor.Vanilla;

        internal static void TryCurateSeed(ref int seed, int dungeonMasterId)
        {
            if (!ShouldCurateSeed)
            {
                return;
            }

            if (_hasSeedCache && seed == _cachedVanillaSeed && dungeonMasterId == _cachedDungeonMasterId)
            {
                seed = _cachedCuratedSeed;
                DungeonRandomizerLog.DebugSeedCurationReused(_cachedVanillaSeed, _cachedCuratedSeed);
                return;
            }

            int vanillaSeed = seed;
            try
            {
                seed = DungeonSeedFlavorResolver.ResolveSeed(seed, dungeonMasterId);
                _cachedVanillaSeed = vanillaSeed;
                _cachedDungeonMasterId = dungeonMasterId;
                _cachedCuratedSeed = seed;
                _hasSeedCache = true;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Seed curation failed — {ex.Message}");
            }
        }

        internal static bool ShouldIgnoreRerollExcludes()
        {
            DungeonRandomizerSceneConfig config = SceneScopedConfigGate.DungeonRandomizer;
            return ShouldApply
                   && config.RandomizeDungeonPick
                   && config.IgnoreDungeonExcludeList
                   && DungeonIdListParser.ParsePoolMode(config.DungeonPickPoolMode) == DungeonPickPoolMode.WidenVanilla;
        }
    }
}
