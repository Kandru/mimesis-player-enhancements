using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using MimesisPlayerEnhancement.Util;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardModerationService
    {
        private const string Feature = "WebDashboard";

        internal static WebDashboardActionResult Execute(WebDashboardPendingAction action)
        {
            if (!WebDashboardGameState.IsHost())
            {
                return Fail("Host only.");
            }

            if (action.SteamId != 0 && LocalPlayerHelper.IsLocalSteamId(action.SteamId))
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
                    _ => Fail("Unknown action."),
                };
        }

        private static WebDashboardActionResult Kick(SessionManager sessionManager, WebDashboardPendingAction action)
        {
            if (!TryResolveTarget(action, out _, out long playerUid))
            {
                return Fail("Player not found.");
            }

            if (!TryGetHostKickContext(sessionManager, out VPlayer? hostPlayer, out int hashCode))
            {
                return Fail("Host player context unavailable.");
            }

            try
            {
                MsgErrorCode result = sessionManager.HandleKickPlayerReq(hostPlayer!, playerUid, hashCode);
                if (result == MsgErrorCode.Success)
                {
                    ModLog.Info(Feature, $"Kicked player uid={playerUid} steam={action.SteamId}.");
                    return Ok("Player kicked.");
                }

                return Fail($"Kick failed: {result}");
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

            if (TryResolveTarget(action, out _, out long playerUid) && playerUid != 0)
            {
                WebDashboardPendingAction kickAction = new()
                {
                    Type = WebDashboardActionType.Kick,
                    SteamId = action.SteamId,
                    PlayerUid = playerUid,
                };
                WebDashboardActionResult kickResult = Kick(sessionManager, kickAction);
                if (!kickResult.Success)
                {
                    return Ok("Player banned (kick may have failed if already disconnected).");
                }
            }

            return Ok("Player banned.");
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
