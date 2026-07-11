namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    [HarmonyPatch(typeof(SteamInviteDispatcher), nameof(SteamInviteDispatcher.SetLobbyName))]
    internal static class SetLobbyNameSnapshotPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            WebDashboardSnapshotCache.MarkDirty();
        }
    }

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
