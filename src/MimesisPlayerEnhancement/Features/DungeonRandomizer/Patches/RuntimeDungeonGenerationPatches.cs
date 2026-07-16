using DunGen;

namespace MimesisPlayerEnhancement.Features.DungeonRandomizer.Patches
{
    [HarmonyPatch(typeof(RuntimeDungeon), nameof(RuntimeDungeon.Generate))]
    internal static class RuntimeDungeonGeneratePatch
    {
        private const int MaxGenerationAttempts = 4;

        private static int _attempt;

        [HarmonyPrefix]
        private static void Prefix(RuntimeDungeon __instance)
        {
            if (!DungeonRandomizerPatchHelpers.ShouldApply)
            {
                return;
            }

            if (_attempt == 0)
            {
                _attempt = 1;
            }
        }

        [HarmonyPostfix]
        private static void Postfix(RuntimeDungeon __instance)
        {
            if (!DungeonRandomizerPatchHelpers.ShouldApply)
            {
                return;
            }

            DungeonGenerator generator = __instance.Generator;
            if (generator.Status == GenerationStatus.Complete)
            {
                _attempt = 0;
                return;
            }

            if (generator.Status != GenerationStatus.Failed)
            {
                return;
            }

            int failedSeed = generator.Seed;
            int nextSeed = failedSeed + 1;
            DungeonRandomizerLog.WarnGenerationFailed(_attempt, failedSeed, nextSeed);

            if (_attempt < MaxGenerationAttempts)
            {
                _attempt++;
            }
        }
    }
}
