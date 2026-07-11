namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    [HarmonyPatch(typeof(MaintenanceScene), "TryInitHostMaintenenceRoom")]
    internal static class MaintenanceSceneReadyCheatsPatch
    {
        [HarmonyPostfix]
        private static void Postfix() =>
            WebDashboardHostCheatsRuntime.EndRoomTransition("maintenance entered");
    }
}
