namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    // game@0.3.2 Assembly-CSharp/SteamInviteDispatcher.cs:L431-441
    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetLobbyName))]
    internal static class SetLobbyNameSnapshotPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            WebDashboardSnapshotCache.MarkDirty();
        }
    }

    // game@0.3.2 Assembly-CSharp/SteamInviteDispatcher.cs:L802-814
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
