using System.Reflection;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using MimesisPlayerEnhancement.Util;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardModerationService
    {
        private const string Feature = "WebDashboard";

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
                return Fail("Host only.");
            }

            if (action.SteamId != 0 && LocalPlayerHelper.IsLocalSteamId(action.SteamId)
                && action.Type != WebDashboardActionType.Respawn)
            {
                return Fail("Cannot moderate the local host player.");
            }

            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            return sessionManager == null
                ? Fail("Session manager unavailable.")
                : action.Type switch
                {
                    WebDashboardActionType.Kick => Kick(sessionManager, action),
                    WebDashboardActionType.Ban => Ban(sessionManager, action),
                    WebDashboardActionType.Unban => Unban(sessionManager, action),
                    WebDashboardActionType.Respawn => Respawn(action),
                    _ => Fail("Unknown action."),
                };
        }

        private static WebDashboardActionResult Kick(SessionManager sessionManager, WebDashboardPendingAction action)
        {
            if (!TryResolveTarget(action, out SessionContext? targetContext, out long playerUid))
            {
                return Fail("Player not found.");
            }

            if (!TryGetHostKickContext(sessionManager, out VPlayer? hostPlayer, out int hashCode))
            {
                return Fail("Host player context unavailable.");
            }

            if (!WebDashboardSessionAccess.TryGetSessionId(targetContext!, out long sessionId))
            {
                return Fail("Session ID unavailable.");
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
                    "Player kicked.");
            }
            catch (System.Exception ex)
            {
                ModLog.Warn(Feature, $"Kick failed: {ex.Message}");
                return Fail("Kick failed.");
            }
        }

        private static WebDashboardActionResult Ban(SessionManager sessionManager, WebDashboardPendingAction action)
        {
            if (action.SteamId == 0)
            {
                return Fail("Invalid Steam ID.");
            }

            if (!WebDashboardSessionAccess.TryAddBan(sessionManager, action.SteamId))
            {
                return WebDashboardSessionAccess.IsBanned(sessionManager, action.SteamId) ? Ok("Player already banned.") : Fail("Failed to add ban.");
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
                        "Player banned.");
                    if (!disconnectResult.Success)
                    {
                        return Ok("Player banned (disconnect may have failed if already offline).");
                    }

                    return disconnectResult;
                }
                catch (System.Exception ex)
                {
                    ModLog.Warn(Feature, $"Ban disconnect failed: {ex.Message}");
                    return Ok("Player banned (disconnect may have failed if already offline).");
                }
            }

            return Ok("Player banned.");
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
                return Fail("Player not found.");
            }

            VPlayer? vPlayer = WebDashboardSessionAccess.GetVPlayer(targetContext!);
            if (vPlayer == null)
            {
                return Fail("Player not in game.");
            }

            if (vPlayer.LifeCycle != VCreatureLifeCycle.Dead)
            {
                return Fail("Player is not dead.");
            }

            if (vPlayer.VRoom == null || !vPlayer.VRoom.CanReviveCheat())
            {
                return Fail("Revive not allowed in the current room state.");
            }

            if (!TryGetReviveSpawnPoint(out MapMarker_CreatureSpawnPoint? spawnPoint))
            {
                return Fail("No revive point available.");
            }

            try
            {
                vPlayer.SetIsIndoor(spawnPoint!.IsIndoor);
                if (!vPlayer.Revive(spawnPoint.pos))
                {
                    return Fail("Revive failed.");
                }

                if (vPlayer.StatControlUnit != null)
                {
                    vPlayer.StatControlUnit.AdjustHP(0L, full: true);
                    vPlayer.StatControlUnit.RecoverStamina(
                        vPlayer.StatControlUnit.GetSpecificStatValue(StatType.Stamina));
                    vPlayer.StatControlUnit.AdjustConta(0);
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
                return Ok("Player respawned.");
            }
            catch (System.Exception ex)
            {
                ModLog.Warn(Feature, $"Respawn failed: {ex.Message}");
                return Fail("Respawn failed.");
            }
        }

        private static WebDashboardActionResult Unban(SessionManager sessionManager, WebDashboardPendingAction action)
        {
            if (action.SteamId == 0)
            {
                return Fail("Invalid Steam ID.");
            }

            if (!WebDashboardSessionAccess.TryRemoveBan(sessionManager, action.SteamId))
            {
                return Fail("Player was not banned.");
            }

            ModLog.Info(Feature, $"Unbanned steam={action.SteamId}.");
            return Ok("Ban removed.");
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
