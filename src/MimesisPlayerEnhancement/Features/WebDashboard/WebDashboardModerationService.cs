using System.Reflection;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardModerationService
    {
        private const string Feature = "WebDashboard";

        private static string L(string key, params object[] args) => WebDashboardL10n.Get($"api.{key}", args);

        // game@0.3.1 GameMainBase.CorDying — deadCameraDuration, then +4s while any player is alive.
        private const float DeathPresentationExtraWaitSeconds = 5f;
        private const float FallbackDeadCameraDurationSeconds = 5f;

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
                && action.Type is not WebDashboardActionType.Heal)
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
                    WebDashboardActionType.Heal => Heal(action),
                    _ => Fail(L("unknown_action")),
                };
        }

        private static WebDashboardActionResult Kick(SessionManager sessionManager, WebDashboardPendingAction action)
        {
            if (!TryResolveTarget(action, out _, out long playerUid) || playerUid == 0)
            {
                return Fail(L("player_not_found"));
            }

            try
            {
                if (!WebDashboardSessionAccess.TryForceDisconnect(
                        sessionManager,
                        playerUid,
                        DisconnectReason.KickByServer))
                {
                    return Fail(L("kick_failed"));
                }

                ModLog.Info(Feature, $"Kicked player uid={playerUid}.");
                return Ok(L("player_kicked"));
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

            if (TryResolveTarget(action, out _, out long playerUid)
                && playerUid != 0
                && TryGetHostKickContext(sessionManager, out VPlayer? hostPlayer, out int hashCode))
            {
                try
                {
                    MsgErrorCode result = sessionManager.HandleKickPlayerReq(hostPlayer!, playerUid, hashCode);
                    WebDashboardActionResult mapped = MapBanKickResult(result);
                    if (mapped.Success)
                    {
                        ModLog.Info(Feature, $"Banned player uid={playerUid}.");
                    }

                    return mapped;
                }
                catch (System.Exception ex)
                {
                    ModLog.Warn(Feature, $"Ban failed: {ex.Message}");
                    return Fail(L("ban_failed"));
                }
            }

            if (WebDashboardSessionAccess.IsBanned(sessionManager, action.SteamId))
            {
                return Ok(L("player_already_banned"));
            }

            if (!WebDashboardSessionAccess.TryAddBan(sessionManager, action.SteamId))
            {
                return Fail(L("ban_failed"));
            }

            ModLog.Info(Feature, $"Banned steam={action.SteamId} (offline).");
            return Ok(L("player_banned"));
        }

        private static WebDashboardActionResult MapBanKickResult(MsgErrorCode result)
        {
            return result switch
            {
                MsgErrorCode.Success => Ok(L("player_banned")),
                MsgErrorCode.SessionNotFound => Fail(L("player_not_found")),
                MsgErrorCode.PermissionDenied => Fail(L("host_only")),
                MsgErrorCode.InvalidErrorCode => Fail(L("cannot_moderate_host")),
                _ => Fail(L("ban_failed")),
            };
        }

        internal static WebDashboardActionResult Respawn(ulong steamId, long playerUid)
        {
            if (!WebDashboardGameState.IsHost())
            {
                return Fail(L("host_only"));
            }

            WebDashboardPendingAction action = new()
            {
                SteamId = steamId,
                PlayerUid = playerUid,
            };

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

            if (!IsDeathPresentationFinished(action, out int remainingSeconds))
            {
                return Fail(L("player_still_dying", new Dictionary<string, object>
                {
                    ["seconds"] = remainingSeconds,
                }));
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

        private static bool IsDeathPresentationFinished(WebDashboardPendingAction action, out int remainingSeconds)
        {
            remainingSeconds = 0;
            WebDashboardLiveRoster roster = WebDashboardLiveRoster.Capture();
            ProtoActor? actor = null;

            if (action.PlayerUid != 0 && roster.TryGetByUid(action.PlayerUid, out ProtoActor byUid))
            {
                actor = byUid;
            }
            else if (action.SteamId != 0 && roster.TryGetBySteamId(action.SteamId, out ProtoActor bySteam))
            {
                actor = bySteam;
            }

            if (actor == null || !actor.dead)
            {
                return true;
            }

            float deadCameraDuration = FallbackDeadCameraDurationSeconds;
            try
            {
                GameConfig.PlayerActor? config = actor.paConfig;
                if (config != null)
                {
                    deadCameraDuration = config.deadCameraDuration;
                }
            }
            catch
            {
                /* scene may be transitioning */
            }

            float requiredWait = deadCameraDuration + DeathPresentationExtraWaitSeconds;
            float remaining = requiredWait - (Time.time - actor.deadTime);
            if (remaining <= 0f)
            {
                return true;
            }

            remainingSeconds = (int)System.Math.Ceiling(remaining);
            return false;
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
