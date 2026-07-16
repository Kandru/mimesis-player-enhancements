namespace MimesisPlayerEnhancement.Features.DungeonRandomizer.Patches
{
    internal static class DungeonRandomizerPatchHelpers
    {
        internal const string Feature = "DungeonRandomizer";

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

            try
            {
                seed = DungeonSeedFlavorResolver.ResolveSeed(seed, dungeonMasterId);
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
