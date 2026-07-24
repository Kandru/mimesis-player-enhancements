namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    // game@0.3.1 Assembly-CSharp/SessionContext.cs:L340-397
    [HarmonyPatch(typeof(SessionContext), nameof(SessionContext.Login))]
    internal static class SessionContextLoginPatch
    {
        [HarmonyPostfix]
        private static void Postfix(SessionContext __instance)
        {
            // Host status is consumed mod-wide (host-only gating) — always invalidate.
            HostStatusCache.Invalidate();

            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            JoinAnytimeConnectingTracker.OnServerLogin(__instance);
            JoinAnytimeLobbyController.OnSessionRosterChanged();
        }
    }
}
