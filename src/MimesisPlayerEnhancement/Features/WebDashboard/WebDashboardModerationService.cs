using System.Reflection;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardModerationService
    {
        private const string Feature = "WebDashboard";

        private static string L(string key) => WebDashboardL10n.Get($"api.{key}");

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly PropertyInfo? HubDynamicDataManProperty =
            typeof(Hub).GetProperty("dynamicDataMan", InstanceFlags);

        private static readonly MethodInfo? GetPlayerRevivePointMethod =
            typeof(Hub).Assembly.GetType("DynamicDataManager")?.GetMethod(
                "GetPlayerRevivePoint",
                InstanceFlags,
                binder: null,
                types: [typeof(int)],
                modifiers: null);

        private static readonly MethodInfo? GetPlayerStartPointMethod =
            typeof(Hub).Assembly.GetType("DynamicDataManager")?.GetMethod(
                "GetPlayerStartPoint",
                InstanceFlags,
                binder: null,
                types: [typeof(int)],
                modifiers: null);

        internal static WebDashboardActionResult Execute(WebDashboardPendingAction action)
        {
            if (!WebDashboardGameState.IsHost())
            {
                return Fail(L("host_only"));
            }

            if (action.SteamId != 0 && LocalPlayerHelper.IsLocalSteamId(action.SteamId)
                && action.Type is not WebDashboardActionType.Respawn and not WebDashboardActionType.Heal)
            {
                return Fail(L("cannot_moderate_host"));
            }

            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            return sessionManager == null
                ? Fail(L("session_manager_unavailable"))
                : action.Type switch
                {
                    WebDashboardActionType.Kick => Kick(sessionManager, action),
                    WebDashboardActionType.Ban => Ban(sessionManager, action),
                    WebDashboardActionType.Unban => Unban(sessionManager, action),
                    WebDashboardActionType.Respawn => Respawn(action),
                    WebDashboardActionType.Heal => Heal(action),
                    WebDashboardActionType.ToggleGodMode => ToggleGodMode(action),
                    WebDashboardActionType.ToggleNoClip => ToggleNoClip(action),
                    _ => Fail(L("unknown_action")),
                };
        }

        private static WebDashboardActionResult Kick(SessionManager sessionManager, WebDashboardPendingAction action)
        {
            if (!TryResolveTarget(action, out SessionContext? targetContext, out long playerUid))
            {
                return Fail(L("player_not_found"));
            }

            if (!TryGetHostKickContext(sessionManager, out VPlayer? hostPlayer, out int hashCode))
            {
                return Fail(L("host_context_unavailable"));
            }

            if (!WebDashboardSessionAccess.TryGetSessionId(targetContext!, out long sessionId))
            {
                return Fail(L("session_id_unavailable"));
            }

            try
            {
                // HandleKickPlayerReq always adds to _bannedSteamIDs; disconnect without banning instead.
                return DisconnectPlayer(
                    sessionManager,
                    hostPlayer!,
                    playerUid,
                    hashCode,
                    sessionId,
                    DisconnectReason.KickByServer,
                    "Kicked",
                    L("player_kicked"));
            }
            catch (System.Exception ex)
            {
                ModLog.Warn(Feature, $"Kick failed: {ex.Message}");
                return Fail(L("kick_failed"));
            }
        }

        private static WebDashboardActionResult Ban(SessionManager sessionManager, WebDashboardPendingAction action)
        {
            if (action.SteamId == 0)
            {
                return Fail(L("invalid_steam_id"));
            }

            if (!WebDashboardSessionAccess.TryAddBan(sessionManager, action.SteamId))
            {
                return WebDashboardSessionAccess.IsBanned(sessionManager, action.SteamId)
                    ? Ok(L("player_already_banned"))
                    : Fail(L("ban_failed"));
            }

            ModLog.Info(Feature, $"Banned steam={action.SteamId}.");

            if (TryResolveTarget(action, out SessionContext? targetContext, out long playerUid)
                && playerUid != 0
                && TryGetHostKickContext(sessionManager, out VPlayer? hostPlayer, out int hashCode)
                && WebDashboardSessionAccess.TryGetSessionId(targetContext!, out long sessionId))
            {
                try
                {
                    WebDashboardActionResult disconnectResult = DisconnectPlayer(
                        sessionManager,
                        hostPlayer!,
                        playerUid,
                        hashCode,
                        sessionId,
                        DisconnectReason.KickByHost,
                        "Banned and kicked",
                        L("player_banned"));
                    if (!disconnectResult.Success)
                    {
                        return Ok(L("player_banned_offline"));
                    }

                    return disconnectResult;
                }
                catch (System.Exception ex)
                {
                    ModLog.Warn(Feature, $"Ban disconnect failed: {ex.Message}");
                    return Ok(L("player_banned_offline"));
                }
            }

            return Ok(L("player_banned"));
        }

        private static WebDashboardActionResult DisconnectPlayer(
            SessionManager sessionManager,
            VPlayer hostPlayer,
            long playerUid,
            int hashCode,
            long sessionId,
            DisconnectReason reason,
            string logAction,
            string successMessage)
        {
            hostPlayer.SendToMe(new KickPlayerRes(hashCode)
            {
                kickPlayerUID = playerUid,
            });
            sessionManager.BroadcastToAll(new KickPlayerSig
            {
                kickPlayerUID = playerUid,
            });
            WebDashboardSessionAccess.DisconnectSession(sessionManager, sessionId, reason);
            ModLog.Info(Feature, $"{logAction} player uid={playerUid}.");
            return Ok(successMessage);
        }

        private static WebDashboardActionResult Respawn(WebDashboardPendingAction action)
        {
            if (!TryResolveTarget(action, out SessionContext? targetContext, out _))
            {
                return Fail(L("player_not_found"));
            }

            VPlayer? vPlayer = WebDashboardSessionAccess.GetVPlayer(targetContext!);
            if (vPlayer == null)
            {
                return Fail(L("player_not_in_game"));
            }

            if (vPlayer.LifeCycle != VCreatureLifeCycle.Dead)
            {
                return Fail(L("player_not_dead"));
            }

            if (vPlayer.VRoom == null || !vPlayer.VRoom.CanReviveCheat())
            {
                return Fail(L("revive_not_allowed"));
            }

            if (!TryGetReviveSpawnPoint(out MapMarker_CreatureSpawnPoint? spawnPoint))
            {
                return Fail(L("no_revive_point"));
            }

            try
            {
                vPlayer.SetIsIndoor(spawnPoint!.IsIndoor);
                if (!vPlayer.Revive(spawnPoint.pos))
                {
                    return Fail(L("revive_failed"));
                }

                if (vPlayer.StatControlUnit != null)
                {
                    ApplyFullHealthAndClearConta(vPlayer);
                    vPlayer.StatControlUnit.RecoverStamina(
                        vPlayer.StatControlUnit.GetSpecificStatValue(StatType.Stamina));
                }

                vPlayer.VRoom.IterateAllMonster(monster =>
                {
                    if (monster.IsAliveStatus())
                    {
                        monster.AIControlUnit?.OnSightIn(vPlayer);
                    }
                });

                ModLog.Info(Feature, $"Respawned player uid={vPlayer.UID}.");
                WebDashboardSnapshotCache.MarkDirty();
                return Ok(L("player_respawned"));
            }
            catch (System.Exception ex)
            {
                ModLog.Warn(Feature, $"Respawn failed: {ex.Message}");
                return Fail(L("respawn_failed"));
            }
        }

        private static WebDashboardActionResult Heal(WebDashboardPendingAction action)
        {
            if (!TryResolveTarget(action, out SessionContext? targetContext, out _))
            {
                return Fail(L("player_not_found"));
            }

            VPlayer? vPlayer = WebDashboardSessionAccess.GetVPlayer(targetContext!);
            if (vPlayer == null)
            {
                return Fail(L("player_not_in_game"));
            }

            if (!vPlayer.IsAliveStatus())
            {
                return Fail(L("player_dead_use_respawn"));
            }

            if (vPlayer.StatControlUnit == null)
            {
                return Fail(L("player_stats_unavailable"));
            }

            try
            {
                ApplyFullHealthAndClearConta(vPlayer);
                ModLog.Info(Feature, $"Healed player uid={vPlayer.UID}.");
                WebDashboardSnapshotCache.MarkDirty();
                return Ok(L("player_healed"));
            }
            catch (System.Exception ex)
            {
                ModLog.Warn(Feature, $"Heal failed: {ex.Message}");
                return Fail(L("heal_failed"));
            }
        }

        private static WebDashboardActionResult ToggleGodMode(WebDashboardPendingAction action)
        {
            if (!TryResolveTarget(action, out SessionContext? targetContext, out _))
            {
                return Fail(L("player_not_found"));
            }

            VPlayer? vPlayer = WebDashboardSessionAccess.GetVPlayer(targetContext!);
            if (vPlayer == null)
            {
                return Fail(L("player_not_in_game"));
            }

            if (!WebDashboardHostCheatsRuntime.TryToggleGodMode(vPlayer, out bool enabled, out string? errorMessage))
            {
                return Fail(errorMessage ?? L("failed_apply"));
            }

            return Ok(enabled ? L("player_godmode_enabled") : L("player_godmode_disabled"));
        }

        private static WebDashboardActionResult ToggleNoClip(WebDashboardPendingAction action)
        {
            if (!TryResolveTarget(action, out SessionContext? targetContext, out _))
            {
                return Fail(L("player_not_found"));
            }

            VPlayer? vPlayer = WebDashboardSessionAccess.GetVPlayer(targetContext!);
            if (vPlayer == null)
            {
                return Fail(L("player_not_in_game"));
            }

            if (!WebDashboardHostCheatsRuntime.TryToggleNoClip(vPlayer, out bool enabled, out string? errorMessage))
            {
                return Fail(errorMessage ?? L("failed_apply"));
            }

            return Ok(enabled ? L("player_noclip_enabled") : L("player_noclip_disabled"));
        }

        private static void ApplyFullHealthAndClearConta(VPlayer vPlayer)
        {
            StatController? stats = vPlayer.StatControlUnit;
            if (stats == null)
            {
                return;
            }

            stats.AdjustHP(0L, full: true);
            stats.AdjustConta(0);
        }

        private static WebDashboardActionResult Unban(SessionManager sessionManager, WebDashboardPendingAction action)
        {
            if (action.SteamId == 0)
            {
                return Fail(L("invalid_steam_id"));
            }

            if (!WebDashboardSessionAccess.TryRemoveBan(sessionManager, action.SteamId))
            {
                return Fail(L("player_not_banned"));
            }

            ModLog.Info(Feature, $"Unbanned steam={action.SteamId}.");
            return Ok(L("ban_removed"));
        }

        private static bool TryResolveTarget(
            WebDashboardPendingAction action,
            out SessionContext? targetContext,
            out long playerUid)
        {
            targetContext = null;
            playerUid = action.PlayerUid;

            SessionManager? manager = WebDashboardSessionAccess.GetSessionManager();
            if (playerUid != 0 && manager != null)
            {
                foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(manager))
                {
                    if (context.GetPlayerUID() == playerUid)
                    {
                        targetContext = context;
                        return true;
                    }
                }
            }

            if (action.SteamId == 0)
            {
                return false;
            }

            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager == null)
            {
                return false;
            }

            foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
            {
                if (context.SteamID == action.SteamId)
                {
                    targetContext = context;
                    playerUid = context.GetPlayerUID();
                    return playerUid != 0;
                }
            }

            return false;
        }

        private static bool TryGetHostKickContext(SessionManager sessionManager, out VPlayer? hostPlayer, out int hashCode)
        {
            hostPlayer = null;
            hashCode = 0;

            SessionContext? hostContext = WebDashboardSessionAccess.FindHostSessionContext(sessionManager);
            if (hostContext == null)
            {
                return false;
            }

            hostPlayer = WebDashboardSessionAccess.GetVPlayer(hostContext);
            hashCode = WebDashboardSessionAccess.GetEnterPktHashCode(hostContext);
            return hostPlayer != null;
        }

        private static bool TryGetReviveSpawnPoint(out MapMarker_CreatureSpawnPoint? spawnPoint)
        {
            spawnPoint = null;
            if (Hub.s == null
                || HubDynamicDataManProperty?.GetValue(Hub.s) is not object dynamicDataMan
                || GetPlayerRevivePointMethod == null
                || GetPlayerStartPointMethod == null)
            {
                return false;
            }

            spawnPoint = GetPlayerRevivePointMethod.Invoke(dynamicDataMan, [0]) as MapMarker_CreatureSpawnPoint
                ?? GetPlayerStartPointMethod.Invoke(dynamicDataMan, [0]) as MapMarker_CreatureSpawnPoint;
            return spawnPoint != null;
        }

        private static WebDashboardActionResult Ok(string message)
        {
            return new() { Success = true, Message = message };
        }

        private static WebDashboardActionResult Fail(string message)
        {
            return new() { Success = false, Message = message };
        }
    }
}
