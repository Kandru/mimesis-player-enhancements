namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    // game@0.3.2 Assembly-CSharp/MaintenanceScene.cs:L243-258
    [HarmonyPatch(typeof(MaintenanceScene), "TryInitHostMaintenenceRoom")]
    internal static class MaintenanceSceneReadyCheatsPatch
    {
        [HarmonyPostfix]
        private static void Postfix() =>
            WebDashboardHostCheatsRuntime.EndRoomTransition("maintenance entered");
    }
}
