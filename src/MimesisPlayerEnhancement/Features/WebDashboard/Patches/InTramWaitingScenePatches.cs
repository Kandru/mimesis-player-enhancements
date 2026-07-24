using System.Reflection;

namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    // game@0.3.1 Assembly-CSharp/InTramWaitingScene.cs:L68-92
    [HarmonyPatch]
    internal static class InTramWaitingSceneReadyCheatsPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(InTramWaitingScene), "Start")
                ?? throw new InvalidOperationException("InTramWaitingScene.Start not found");

        [HarmonyPostfix]
        private static void Postfix() =>
            WebDashboardHostCheatsRuntime.EndRoomTransition("tram entered");
    }
}
