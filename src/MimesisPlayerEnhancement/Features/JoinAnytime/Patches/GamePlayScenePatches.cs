namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    // game@0.3.1 Assembly-CSharp/GamePlayScene.cs:L265-319
    [HarmonyPatch]
    internal static class GamePlaySceneStartPatch
    {
        private static System.Reflection.MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(GamePlayScene), "Start");

        [HarmonyPostfix]
        private static void Postfix()
        {
            JoinAnytimeLobbyController.OnHostSceneReady();
        }
    }
}
