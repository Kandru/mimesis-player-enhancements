namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardHostCheatsRuntime
    {
        private const string Feature = "WebDashboard";

        internal static bool GodModeEnabled { get; private set; }

        internal static bool NoClipEnabled { get; private set; }

        internal static bool IsAvailable =>
            ModConfig.EnableWebDashboard.Value
            && WebDashboardGameState.IsHost()
            && HostApplyGate.ShouldApplyHostOnlyFeature()
            && TryGetHostVPlayer(out VPlayer? player)
            && player!.IsAliveStatus();

        internal static bool IsGodModeActive(VPlayer? player) =>
            GodModeEnabled && IsHostPlayer(player);

        internal static bool IsNoClipActive(VPlayer? player) =>
            NoClipEnabled && IsHostPlayer(player);

        internal static bool IsNoClipActive(VCreature? creature) =>
            creature is VPlayer player && IsNoClipActive(player);

        internal static bool TryGetHostVPlayer(out VPlayer? player)
        {
            player = null;
            if (!WebDashboardGameState.IsHost())
            {
                return false;
            }

            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return false;
            }

            SessionContext? hostContext = WebDashboardSessionAccess.FindHostSessionContext(sessionManager);
            if (hostContext == null)
            {
                return false;
            }

            player = WebDashboardSessionAccess.GetVPlayer(hostContext);
            return player != null;
        }

        internal static bool TrySetGodMode(bool enabled, out string? errorMessage)
        {
            errorMessage = null;
            if (!CanApplyCheats(out errorMessage))
            {
                return false;
            }

            if (!TryGetHostVPlayer(out VPlayer? player) || player == null)
            {
                errorMessage = WebDashboardL10n.Get("api.host_player_unavailable");
                return false;
            }

            ApplyGodMode(player, enabled);
            GodModeEnabled = enabled;
            WebDashboardSnapshotCache.MarkDirty();
            ModLog.Info(Feature, $"Host godmode {(enabled ? "enabled" : "disabled")}.");
            return true;
        }

        internal static bool TrySetNoClip(bool enabled, out string? errorMessage)
        {
            errorMessage = null;
            if (!CanApplyCheats(out errorMessage))
            {
                return false;
            }

            if (!TryGetHostVPlayer(out VPlayer? player) || player == null)
            {
                errorMessage = WebDashboardL10n.Get("api.host_player_unavailable");
                return false;
            }

            NoClipEnabled = enabled;
            if (!enabled)
            {
                WebDashboardHostCheatsNoClipMovement.TryRestoreLocalAvatar();
            }

            WebDashboardSnapshotCache.MarkDirty();
            ModLog.Info(Feature, $"Host noclip {(enabled ? "enabled" : "disabled")}.");
            return true;
        }

        internal static void DisableAll(string reason)
        {
            if (!GodModeEnabled && !NoClipEnabled)
            {
                return;
            }

            if (TryGetHostVPlayer(out VPlayer? player) && player != null)
            {
                if (GodModeEnabled)
                {
                    ApplyGodMode(player, enabled: false);
                }
            }

            if (NoClipEnabled)
            {
                WebDashboardHostCheatsNoClipMovement.TryRestoreLocalAvatar();
            }

            GodModeEnabled = false;
            NoClipEnabled = false;
            WebDashboardSnapshotCache.MarkDirty();
            ModLog.Debug(Feature, $"Host cheats disabled — {reason}.");
        }

        internal static void SyncFromSession()
        {
            if (!WebDashboardGameState.IsConnected() || !WebDashboardGameState.IsHost())
            {
                DisableAll("session ended");
                return;
            }

            if (!TryGetHostVPlayer(out VPlayer? player)
                || player == null
                || !player.IsAliveStatus())
            {
                DisableAll("host not alive");
            }
        }

        internal static void OnDeinitialize()
        {
            DisableAll("mod deinit");
        }

        private static bool CanApplyCheats(out string? errorMessage)
        {
            errorMessage = null;
            if (!ModConfig.EnableWebDashboard.Value)
            {
                errorMessage = WebDashboardL10n.Get("api.web_dashboard_disabled");
                return false;
            }

            if (!WebDashboardGameState.IsHost())
            {
                errorMessage = WebDashboardL10n.Get("api.host_only");
                return false;
            }

            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                errorMessage = WebDashboardL10n.Get("api.host_only");
                return false;
            }

            if (!WebDashboardGameState.IsConnected())
            {
                errorMessage = WebDashboardL10n.Get("api.not_connected");
                return false;
            }

            if (!TryGetHostVPlayer(out VPlayer? player) || player == null)
            {
                errorMessage = WebDashboardL10n.Get("api.host_player_unavailable");
                return false;
            }

            if (!player.IsAliveStatus())
            {
                errorMessage = WebDashboardL10n.Get("api.host_cheats_require_alive");
                return false;
            }

            return true;
        }

        private static void ApplyGodMode(VPlayer player, bool enabled)
        {
            StatController? stats = player.StatControlUnit;
            if (stats != null)
            {
                stats.SetCheatUndying(enabled);
            }

            player.SetHarmBlocked(enabled);
        }

        private static bool IsHostPlayer(VPlayer? player)
        {
            if (player == null || !HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return false;
            }

            return TryGetHostVPlayer(out VPlayer? host) && host != null && host.UID == player.UID;
        }
    }
}
