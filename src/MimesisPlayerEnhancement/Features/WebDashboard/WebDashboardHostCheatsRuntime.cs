namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardHostCheatsRuntime
    {
        private const string Feature = "WebDashboard";

        private static readonly HashSet<long> GodModePlayerUids = [];
        private static readonly HashSet<long> NoClipPlayerUids = [];
        private static bool _roomTransitionSuspend;

        [ThreadStatic]
        private static VCreature? _moveValidationCreature;

        internal static bool IsRoomTransitionSuspended => _roomTransitionSuspend;

        internal static VCreature? MoveValidationCreature => _moveValidationCreature;

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

        internal static bool IsNoClipActiveForActor(ProtoActor? actor)
        {
            if (actor == null || !actor.AmIAvatar() || _roomTransitionSuspend)
            {
                return false;
            }

            if (actor.UID != 0 && IsNoClipEnabled(actor.UID))
            {
                return true;
            }

            return WebDashboardHostCheatsClientRuntime.IsLocalNoClipEnabled();
        }

        internal static void BeginMoveValidationBypass(VCreature creature)
        {
            if (IsNoClipActive(creature))
            {
                _moveValidationCreature = creature;
            }
        }

        internal static void EndMoveValidationBypass()
        {
            _moveValidationCreature = null;
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
            bool currentlyEnabled = IsGodModeEnabled(player.UID);
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
            bool currentlyEnabled = IsNoClipEnabled(player.UID);
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
                if (WebDashboardMinimapBlindMode.Enabled)
                {
                    errorMessage = WebDashboardL10n.Get("api.player_cheats_require_blind_off");
                    return false;
                }

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
                if (WebDashboardMinimapBlindMode.Enabled)
                {
                    errorMessage = WebDashboardL10n.Get("api.player_cheats_require_blind_off");
                    return false;
                }

                NoClipPlayerUids.Add(player.UID);
                WebDashboardHostCheatsNoClipMovement.PrepareActor(player.UID);
            }
            else
            {
                NoClipPlayerUids.Remove(player.UID);
                WebDashboardHostCheatsNoClipMovement.TryRestoreActor(player.UID);
            }

            SyncNoClipToClient(player, enabled);
            WebDashboardSnapshotCache.MarkDirty();
            ModLog.Info(Feature, $"Noclip {(enabled ? "enabled" : "disabled")} — uid={player.UID}.");
            return true;
        }

        internal static void DisableAll(string reason)
        {
            _roomTransitionSuspend = false;

            if (GodModePlayerUids.Count == 0 && NoClipPlayerUids.Count == 0)
            {
                WebDashboardHostCheatsClientRuntime.Reset(reason);
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

            foreach (long playerUid in new List<long>(NoClipPlayerUids))
            {
                WebDashboardHostCheatsNoClipMovement.TryRestoreActor(playerUid);
                if (WebDashboardSessionAccess.TryGetPlayerByUid(playerUid, out VPlayer? player)
                    && player != null)
                {
                    WebDashboardHostCheatsNoClipSync.SendToPlayer(player, enabled: false);
                }
            }

            GodModePlayerUids.Clear();
            NoClipPlayerUids.Clear();
            WebDashboardHostCheatsClientRuntime.Reset(reason);
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

            if (!_roomTransitionSuspend)
            {
                PruneInactivePlayers();
                ReapplyConfiguredCheats();
            }
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
            bool changed = false;

            foreach (long playerUid in new List<long>(GodModePlayerUids))
            {
                if (!ShouldPruneCheatPlayer(playerUid, out VPlayer? player, out bool shouldDisable))
                {
                    continue;
                }

                _ = GodModePlayerUids.Remove(playerUid);
                changed = true;
                if (shouldDisable && player != null)
                {
                    ApplyGodMode(player, enabled: false);
                }
            }

            foreach (long playerUid in new List<long>(NoClipPlayerUids))
            {
                if (!ShouldPruneCheatPlayer(playerUid, out VPlayer? player, out bool shouldDisable))
                {
                    continue;
                }

                _ = NoClipPlayerUids.Remove(playerUid);
                changed = true;
                WebDashboardHostCheatsNoClipMovement.TryRestoreActor(playerUid);
                if (shouldDisable && player != null)
                {
                    WebDashboardHostCheatsNoClipSync.SendToPlayer(player, enabled: false);
                }
            }

            if (changed)
            {
                WebDashboardSnapshotCache.MarkDirty();
            }
        }

        private static bool ShouldPruneCheatPlayer(long playerUid, out VPlayer? player, out bool shouldDisable)
        {
            player = null;
            shouldDisable = false;

            if (!WebDashboardSessionAccess.TryGetSessionContextByUid(playerUid, out SessionContext? context)
                || context == null)
            {
                return true;
            }

            if (!WebDashboardSessionAccess.TryGetPlayerByUid(playerUid, out player)
                || player == null)
            {
                return false;
            }

            if (!player.IsAliveStatus())
            {
                shouldDisable = true;
                return true;
            }

            return false;
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

            foreach (long playerUid in new List<long>(NoClipPlayerUids))
            {
                WebDashboardHostCheatsNoClipMovement.TryRestoreActor(playerUid);
                if (WebDashboardSessionAccess.TryGetPlayerByUid(playerUid, out VPlayer? player)
                    && player != null)
                {
                    WebDashboardHostCheatsNoClipSync.SendToPlayer(player, enabled: false);
                }
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

                WebDashboardHostCheatsNoClipMovement.PrepareActor(playerUid);
                WebDashboardHostCheatsNoClipSync.SendToPlayer(player, enabled: true);
            }
        }

        private static void SyncNoClipToClient(VPlayer player, bool enabled)
        {
            if (!WebDashboardGameState.IsHost())
            {
                return;
            }

            WebDashboardHostCheatsNoClipSync.SendToPlayer(player, enabled);
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
    }
}
