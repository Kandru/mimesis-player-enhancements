namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L474-508
    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.PendMoveToDungeon))]
    internal static class PendMoveToDungeonCheatsPatch
    {
        [HarmonyPrefix]
        private static void Prefix() =>
            WebDashboardHostCheatsRuntime.BeginRoomTransition("tram to dungeon");
    }

    // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L572-581
    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.PendMoveToWaitingRoom))]
    internal static class PendMoveToWaitingRoomCheatsPatch
    {
        [HarmonyPrefix]
        private static void Prefix() =>
            WebDashboardHostCheatsRuntime.BeginRoomTransition("maintenance to tram");
    }

    // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L525-570
    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.PendMoveToMaintenance))]
    internal static class PendMoveToMaintenanceCheatsPatch
    {
        [HarmonyPrefix]
        private static void Prefix() =>
            WebDashboardHostCheatsRuntime.BeginRoomTransition("dungeon to maintenance");
    }

    // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L510-523
    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.PendMoveToDeathMatch))]
    internal static class PendMoveToDeathMatchCheatsPatch
    {
        [HarmonyPrefix]
        private static void Prefix() =>
            WebDashboardHostCheatsRuntime.BeginRoomTransition("dungeon to deathmatch");
    }
}
