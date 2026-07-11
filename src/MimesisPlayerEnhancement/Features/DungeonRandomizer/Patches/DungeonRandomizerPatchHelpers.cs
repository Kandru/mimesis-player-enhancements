namespace MimesisPlayerEnhancement.Features.DungeonRandomizer.Patches
{
    internal static class DungeonRandomizerPatchHelpers
    {
        internal const string Feature = "DungeonRandomizer";

        internal static bool ShouldApply =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() => SceneScopedConfigGate.DungeonRandomizer.EnableDungeonRandomizer);

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
