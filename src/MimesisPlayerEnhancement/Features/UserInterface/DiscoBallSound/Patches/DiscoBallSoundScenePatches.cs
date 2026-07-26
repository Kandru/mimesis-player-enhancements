namespace MimesisPlayerEnhancement.Features.UserInterface.DiscoBallSound.Patches
{
    // game@0.3.1 Assembly-CSharp/GamePlayScene.cs:L265-319
    [HarmonyPatch(typeof(GamePlayScene), "Start")]
    internal static class DiscoBallSoundGamePlaySceneStartPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            try
            {
                DiscoBallSoundRuntime.OnDungeonEntryBegin();
            }
            catch (Exception ex)
            {
                ModLog.Warn(DiscoBallSoundConstants.Feature, $"Disco ball dungeon entry hook failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/GameMainBase.cs:L1115-1158
    [HarmonyPatch(typeof(GameMainBase), "OnDestroy")]
    internal static class DiscoBallSoundGameMainBaseOnDestroyPatch
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
                DiscoBallSoundRuntime.OnPlaySceneDestroyed();
            }
            catch (Exception ex)
            {
                ModLog.Warn(DiscoBallSoundConstants.Feature, $"Disco ball scene cleanup failed — {ex.Message}");
            }
        }
    }
}
