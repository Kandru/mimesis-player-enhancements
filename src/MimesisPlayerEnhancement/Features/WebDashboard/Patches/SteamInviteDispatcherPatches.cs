namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    // game@0.3.1 Assembly-CSharp/SteamInviteDispatcher.cs:L429-439
    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetLobbyName))]
    internal static class SetLobbyNameSnapshotPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            WebDashboardSnapshotCache.MarkDirty();
        }
    }

    // game@0.3.1 Assembly-CSharp/SteamInviteDispatcher.cs:L800-812
    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.LeaveLobby))]
    internal static class LeaveLobbySnapshotPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            WebDashboardSnapshotCache.MarkDirty();
        }
    }
}
