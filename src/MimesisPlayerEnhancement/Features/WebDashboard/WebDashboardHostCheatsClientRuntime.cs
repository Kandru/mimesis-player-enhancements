namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    /// <summary>
    /// Client-side mirror of host noclip state for the local player. Populated via
    /// <see cref="WebDashboardHostCheatsNoClipSync"/> when the mod is installed on a remote client.
    /// </summary>
    internal static class WebDashboardHostCheatsClientRuntime
    {
        private const string Feature = "WebDashboard";

        private static bool _localNoClipEnabled;

        internal static bool IsLocalNoClipEnabled() => _localNoClipEnabled;

        internal static void SetLocalNoClip(bool enabled)
        {
            if (_localNoClipEnabled == enabled)
            {
                return;
            }

            _localNoClipEnabled = enabled;
            if (enabled)
            {
                WebDashboardHostCheatsNoClipMovement.PrepareActor(GetLocalPlayerUid());
            }
            else
            {
                WebDashboardHostCheatsNoClipMovement.TryRestoreActor(GetLocalPlayerUid());
            }

            ModLog.Info(Feature, $"Client noclip {(enabled ? "enabled" : "disabled")} — synced from host.");
        }

        internal static void Reset(string reason)
        {
            if (!_localNoClipEnabled)
            {
                return;
            }

            _localNoClipEnabled = false;
            WebDashboardHostCheatsNoClipMovement.TryRestoreActor(GetLocalPlayerUid());
            ModLog.Debug(Feature, $"Client noclip reset — {reason}.");
        }

        private static long GetLocalPlayerUid()
        {
            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return 0;
            }

            ulong localSteamId = LocalPlayerHelper.TryGetLocalSteamId();
            if (localSteamId == 0)
            {
                return 0;
            }

            foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
            {
                if (context.SteamID != localSteamId)
                {
                    continue;
                }

                return WebDashboardSessionAccess.GetVPlayer(context)?.UID ?? 0;
            }

            return 0;
        }
    }
}
