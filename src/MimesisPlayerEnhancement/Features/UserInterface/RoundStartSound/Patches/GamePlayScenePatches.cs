using ModUtility;

namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound.Patches
{
    [HarmonyPatch(typeof(GamePlayScene), "Start")]
    internal static class GamePlaySceneStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            try
            {
                RoundStartSoundRuntime.OnDungeonEntryBegin();
            }
            catch (Exception ex)
            {
                ModLog.Warn(RoundStartSoundConstants.Feature, $"Dungeon entry tracker start failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(ModHelper), nameof(ModHelper.InvokeTimingCallback))]
    internal static class ModHelperInvokeTimingCallbackPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ModHelper.eTiming timing)
        {
            if (timing != ModHelper.eTiming.EnterGame
                || GameSessionAccess.TryGetPdata()?.main is not GamePlayScene)
            {
                return;
            }

            try
            {
                RoundStartSoundRuntime.OnDungeonEntryEnterGame();
            }
            catch (Exception ex)
            {
                ModLog.Warn(RoundStartSoundConstants.Feature, $"Dungeon entry tracker close schedule failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GameMainBase), "OnDestroy")]
    internal static class GameMainBaseOnDestroyRoundStartSoundPatch
    {
        [HarmonyPostfix]
        private static void Postfix(GameMainBase __instance)
        {
            if (__instance is not GamePlayScene)
            {
                return;
            }

            try
            {
                DungeonLandingEntryTracker.End();
            }
            catch (Exception ex)
            {
                ModLog.Warn(RoundStartSoundConstants.Feature, $"Dungeon entry tracker cleanup failed — {ex.Message}");
            }
        }
    }
}
