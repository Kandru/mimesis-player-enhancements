namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardHostCheatsRuntime
    {
        private const string Feature = "WebDashboard";

        private static readonly HashSet<long> GodModePlayerUids = [];
        private static readonly HashSet<long> NoClipPlayerUids = [];
        private static bool _roomTransitionSuspend;

        internal static bool IsRoomTransitionSuspended => _roomTransitionSuspend;

        internal static bool IsGodModeEnabled(long playerUid) =>
            playerUid != 0 && GodModePlayerUids.Contains(playerUid);

        internal static bool IsNoClipEnabled(long playerUid) =>
            playerUid != 0 && NoClipPlayerUids.Contains(playerUid);

        internal static bool IsGodModeActive(VPlayer? player) =>
            !_roomTransitionSuspend && player != null && GodModePlayerUids.Contains(player.UID);

        internal static bool IsNoClipActive(VPlayer? player) =>
            !_roomTransitionSuspend && player != null && NoClipPlayerUids.Contains(player.UID);

        internal static bool IsNoClipActive(VCreature? creature) =>
            creature is VPlayer player && IsNoClipActive(player);

        internal static bool IsLocalAvatarNoClipActive()
        {
            if (_roomTransitionSuspend || !TryGetLocalVPlayer(out VPlayer? player))
            {
                return false;
            }

            return IsNoClipActive(player);
        }

        internal static void BeginRoomTransition(string reason)
        {
            if (_roomTransitionSuspend || (GodModePlayerUids.Count == 0 && NoClipPlayerUids.Count == 0))
            {
                return;
            }

            _roomTransitionSuspend = true;
            RevertAppliedCheats();
            ModLog.Debug(Feature, $"Player cheats suspended — {reason}.");
        }

        internal static void EndRoomTransition(string reason)
        {
            if (!_roomTransitionSuspend)
            {
                return;
            }

            _roomTransitionSuspend = false;
            ReapplyConfiguredCheats();
            WebDashboardSnapshotCache.MarkDirty();
            ModLog.Debug(Feature, $"Player cheats resumed — {reason}.");
        }

        internal static bool TryToggleGodMode(VPlayer player, out bool enabled, out string? errorMessage)
        {
            errorMessage = null;
            bool currentlyEnabled = IsGodModeActive(player);
            if (!TrySetGodMode(player, !currentlyEnabled, out errorMessage))
            {
                enabled = currentlyEnabled;
                return false;
            }

            enabled = !currentlyEnabled;
            return true;
        }

        internal static bool TryToggleNoClip(VPlayer player, out bool enabled, out string? errorMessage)
        {
            errorMessage = null;
            bool currentlyEnabled = IsNoClipActive(player);
            if (!TrySetNoClip(player, !currentlyEnabled, out errorMessage))
            {
                enabled = currentlyEnabled;
                return false;
            }

            enabled = !currentlyEnabled;
            return true;
        }

        internal static bool TrySetGodMode(VPlayer player, bool enabled, out string? errorMessage)
        {
            errorMessage = null;
            if (!CanApplyCheatsForPlayer(player, out errorMessage))
            {
                return false;
            }

            if (enabled)
            {
                GodModePlayerUids.Add(player.UID);
            }
            else
            {
                GodModePlayerUids.Remove(player.UID);
            }

            ApplyGodMode(player, enabled);
            WebDashboardSnapshotCache.MarkDirty();
            ModLog.Info(Feature, $"Godmode {(enabled ? "enabled" : "disabled")} — uid={player.UID}.");
            return true;
        }

        internal static bool TrySetNoClip(VPlayer player, bool enabled, out string? errorMessage)
        {
            errorMessage = null;
            if (!CanApplyCheatsForPlayer(player, out errorMessage))
            {
                return false;
            }

            if (enabled)
            {
                NoClipPlayerUids.Add(player.UID);
                if (IsLocalPlayer(player))
                {
                    WebDashboardHostCheatsNoClipMovement.PrepareLocalAvatar();
                }
            }
            else
            {
                NoClipPlayerUids.Remove(player.UID);
                if (IsLocalPlayer(player))
                {
                    WebDashboardHostCheatsNoClipMovement.TryRestoreLocalAvatar();
                }
            }

            WebDashboardSnapshotCache.MarkDirty();
            ModLog.Info(Feature, $"Noclip {(enabled ? "enabled" : "disabled")} — uid={player.UID}.");
            return true;
        }

        internal static void DisableAll(string reason)
        {
            _roomTransitionSuspend = false;

            if (GodModePlayerUids.Count == 0 && NoClipPlayerUids.Count == 0)
            {
                return;
            }

            foreach (long playerUid in new List<long>(GodModePlayerUids))
            {
                if (!WebDashboardSessionAccess.TryGetPlayerByUid(playerUid, out VPlayer? player)
                    || player == null)
                {
                    continue;
                }

                ApplyGodMode(player, enabled: false);
            }

            if (NoClipPlayerUids.Count > 0)
            {
                WebDashboardHostCheatsNoClipMovement.TryRestoreLocalAvatar();
            }

            GodModePlayerUids.Clear();
            NoClipPlayerUids.Clear();
            WebDashboardSnapshotCache.MarkDirty();
            ModLog.Debug(Feature, $"Player cheats disabled — {reason}.");
        }

        internal static void SyncFromSession()
        {
            if (!WebDashboardGameState.IsConnected() || !WebDashboardGameState.IsHost())
            {
                DisableAll("session ended");
                return;
            }

            PruneInactivePlayers();
        }

        internal static void OnDeinitialize()
        {
            DisableAll("mod deinit");
        }

        internal static bool IsNoClipActiveInRoom(IVroom room)
        {
            if (_roomTransitionSuspend)
            {
                return false;
            }

            foreach (long playerUid in NoClipPlayerUids)
            {
                if (!WebDashboardSessionAccess.TryGetPlayerByUid(playerUid, out VPlayer? player)
                    || player == null
                    || player.VRoom != room)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static void PruneInactivePlayers()
        {
            foreach (long playerUid in new List<long>(GodModePlayerUids))
            {
                if (!WebDashboardSessionAccess.TryGetPlayerByUid(playerUid, out VPlayer? player)
                    || player == null
                    || !player.IsAliveStatus())
                {
                    GodModePlayerUids.Remove(playerUid);
                    if (player != null)
                    {
                        ApplyGodMode(player, enabled: false);
                    }
                }
            }

            foreach (long playerUid in new List<long>(NoClipPlayerUids))
            {
                if (!WebDashboardSessionAccess.TryGetPlayerByUid(playerUid, out VPlayer? player)
                    || player == null
                    || !player.IsAliveStatus())
                {
                    NoClipPlayerUids.Remove(playerUid);
                    if (player != null && IsLocalPlayer(player))
                    {
                        WebDashboardHostCheatsNoClipMovement.TryRestoreLocalAvatar();
                    }
                }
            }

            if (GodModePlayerUids.Count > 0 || NoClipPlayerUids.Count > 0)
            {
                WebDashboardSnapshotCache.MarkDirty();
            }
        }

        private static void RevertAppliedCheats()
        {
            foreach (long playerUid in new List<long>(GodModePlayerUids))
            {
                if (WebDashboardSessionAccess.TryGetPlayerByUid(playerUid, out VPlayer? player)
                    && player != null)
                {
                    ApplyGodMode(player, enabled: false);
                }
            }

            if (NoClipPlayerUids.Count > 0)
            {
                WebDashboardHostCheatsNoClipMovement.TryRestoreLocalAvatar();
            }
        }

        private static void ReapplyConfiguredCheats()
        {
            foreach (long playerUid in new List<long>(GodModePlayerUids))
            {
                if (WebDashboardSessionAccess.TryGetPlayerByUid(playerUid, out VPlayer? player)
                    && player != null
                    && player.IsAliveStatus())
                {
                    ApplyGodMode(player, enabled: true);
                }
            }

            foreach (long playerUid in new List<long>(NoClipPlayerUids))
            {
                if (!WebDashboardSessionAccess.TryGetPlayerByUid(playerUid, out VPlayer? player)
                    || player == null
                    || !player.IsAliveStatus())
                {
                    continue;
                }

                if (IsLocalPlayer(player))
                {
                    WebDashboardHostCheatsNoClipMovement.PrepareLocalAvatar();
                }
            }
        }

        private static bool CanApplyCheatsForPlayer(VPlayer player, out string? errorMessage)
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

            if (!player.IsAliveStatus())
            {
                errorMessage = WebDashboardL10n.Get("api.player_cheats_require_alive");
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

        private static bool TryGetLocalVPlayer(out VPlayer? player)
        {
            player = null;
            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return false;
            }

            ulong localSteamId = LocalPlayerHelper.TryGetLocalSteamId();
            if (localSteamId == 0)
            {
                return false;
            }

            foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
            {
                if (context.SteamID != localSteamId)
                {
                    continue;
                }

                player = WebDashboardSessionAccess.GetVPlayer(context);
                return player != null;
            }

            return false;
        }

        private static bool IsLocalPlayer(VPlayer player)
        {
            return TryGetLocalVPlayer(out VPlayer? local) && local != null && local.UID == player.UID;
        }
    }
}
