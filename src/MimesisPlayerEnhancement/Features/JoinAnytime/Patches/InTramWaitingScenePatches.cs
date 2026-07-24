namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    // game@0.3.1 Assembly-CSharp/InTramWaitingScene.cs:L68-92
    [HarmonyPatch]
    internal static class InTramWaitingSceneStartPatch
    {
        private static System.Reflection.MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(InTramWaitingScene), "Start");

        [HarmonyPostfix]
        private static void Postfix()
        {
            JoinAnytimeLobbyController.OnHostSceneReady();
            JoinAnytimeLobbyController.ApplyHostPublicLobbyIntent();
        }
    }
}
