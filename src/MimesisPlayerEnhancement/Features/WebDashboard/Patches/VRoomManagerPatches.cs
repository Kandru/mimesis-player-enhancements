namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.PendMoveToDungeon))]
    internal static class PendMoveToDungeonCheatsPatch
    {
        [HarmonyPrefix]
        private static void Prefix() =>
            WebDashboardHostCheatsRuntime.BeginRoomTransition("tram to dungeon");
    }

    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.PendMoveToWaitingRoom))]
    internal static class PendMoveToWaitingRoomCheatsPatch
    {
        [HarmonyPrefix]
        private static void Prefix() =>
            WebDashboardHostCheatsRuntime.BeginRoomTransition("maintenance to tram");
    }

    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.PendMoveToMaintenance))]
    internal static class PendMoveToMaintenanceCheatsPatch
    {
        [HarmonyPrefix]
        private static void Prefix() =>
            WebDashboardHostCheatsRuntime.BeginRoomTransition("dungeon to maintenance");
    }

    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.PendMoveToDeathMatch))]
    internal static class PendMoveToDeathMatchCheatsPatch
    {
        [HarmonyPrefix]
        private static void Prefix() =>
            WebDashboardHostCheatsRuntime.BeginRoomTransition("dungeon to deathmatch");
    }
}
