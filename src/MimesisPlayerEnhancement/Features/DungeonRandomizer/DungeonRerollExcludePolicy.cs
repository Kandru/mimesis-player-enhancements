namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonRerollExcludePolicy
    {
        internal static bool ShouldIgnoreRerollExcludes(DungeonRandomizerSceneConfig config, bool shouldApply) =>
            shouldApply
            && config.RandomizeDungeonPick
            && config.IgnoreDungeonExcludeList
            && DungeonIdListParser.ParsePoolMode(config.DungeonPickPoolMode) == DungeonPickPoolMode.WidenVanilla;
    }
}
