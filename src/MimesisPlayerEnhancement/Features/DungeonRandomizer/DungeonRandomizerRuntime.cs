namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonRandomizerRuntime
    {
        internal static void OnSessionEnded()
        {
            Patches.DungeonRandomizerPatchHelpers.ClearSeedCache();
            Patches.RuntimeDungeonGeneratePatch.ResetGenerationAttempt();
            DungeonPickResolver.ClearFilterCache();
        }
    }
}
