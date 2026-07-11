using System.Reflection;

namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
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
