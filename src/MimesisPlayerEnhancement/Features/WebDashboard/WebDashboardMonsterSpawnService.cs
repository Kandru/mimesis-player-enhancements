using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMonsterSpawnService
    {
        private const string Feature = "WebDashboard";

        private static string L(string key) => WebDashboardL10n.Get($"api.{key}");

        internal static WebDashboardActionResult Execute(
            ulong steamId,
            long playerUid,
            string monsterId)
        {
            if (!WebDashboardGameState.IsHost())
            {
                return Fail(L("host_only"));
            }

            if (!WebDashboardMonsterCatalogService.TryResolveMasterId(monsterId, out int masterId, out string errorKey))
            {
                return Fail(L(errorKey));
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

            if (!vPlayer.IsAliveStatus())
            {
                return Fail(L("player_dead_use_respawn"));
            }

            IVroom? vroom = vPlayer.VRoom;
            if (vroom == null)
            {
                return Fail(L("player_not_in_game"));
            }

            try
            {
                if (!WebDashboardSpawnPlacement.TryResolveForwardSpawn(
                        vPlayer,
                        4f,
                        5f,
                        out PosWithRot spawnPos))
                {
                    ModLog.Info(Feature, $"Monster spawn blocked — no clear space in front, uid={vPlayer.UID}.");
                    return Fail(L("monster_spawn_blocked"));
                }

                VMonster? monster = vroom.CreateMonster(
                    masterId,
                    spawnPos,
                    vPlayer.IsIndoor,
                    aiName: "",
                    btName: "",
                    ReasonOfSpawn.Admin);

                if (monster == null)
                {
                    return Fail(L("monster_spawn_failed"));
                }

                ModLog.Info(Feature, $"Spawned monster masterId={masterId} in world — uid={vPlayer.UID}.");
                WebDashboardSnapshotCache.MarkDirty();
                return Ok(L("monster_spawned"));
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Monster spawn failed — {ex.Message}");
                return Fail(L("monster_spawn_failed"));
            }
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

        private static WebDashboardActionResult Ok(string message)
        {
            return new()
            {
                Success = true,
                Message = message,
            };
        }

        private static WebDashboardActionResult Fail(string message)
        {
            return new()
            {
                Success = false,
                Message = message,
            };
        }
    }
}
