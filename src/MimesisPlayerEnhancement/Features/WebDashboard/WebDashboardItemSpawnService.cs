using System;
using System.Reflection;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using ReluProtocol;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardItemSpawnService
    {
        private const string Feature = "WebDashboard";

        private static string L(string key) => WebDashboardL10n.Get($"api.{key}");

        private static readonly Type[] SpawnLootingObjectParameterTypes =
        [
            typeof(ItemElement),
            typeof(PosWithRot),
            typeof(bool),
            typeof(ReasonOfSpawn),
            typeof(int),
            typeof(int),
            typeof(long),
            typeof(bool),
            typeof(bool),
        ];

        private static readonly MethodInfo SpawnLootingObjectMethod =
            AccessTools.Method(typeof(IVroom), "SpawnLootingObject", SpawnLootingObjectParameterTypes)
            ?? throw new InvalidOperationException("IVroom.SpawnLootingObject not found");

        internal static WebDashboardSpawnItemResult Execute(
            ulong steamId,
            long playerUid,
            string itemId,
            int? percent)
        {
            if (!WebDashboardGameState.IsHost())
            {
                return Fail(L("host_only"));
            }

            if (!WebDashboardItemCatalogService.TryResolveMasterId(itemId, percent, out int masterId, out string errorKey))
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

            ItemElement? element = vroom.GetNewItemElement(masterId, isFake: false);
            if (element == null)
            {
                return Fail(L("item_spawn_failed"));
            }

            ItemElement item = element;

            try
            {
                InventoryController? inventory = vPlayer.InventoryControlUnit;
                if (inventory != null && inventory.CanAddItem(item))
                {
                    MsgErrorCode addResult = inventory.HandleAddItem(item, out _, sync: true, byLooting: false);
                    if (addResult == MsgErrorCode.Success)
                    {
                        ModLog.Info(Feature, $"Spawned item masterId={masterId} to inventory — uid={vPlayer.UID}.");
                        WebDashboardSnapshotCache.MarkDirty();
                        return Ok(L("item_spawned_inventory"), "inventory");
                    }

                    ModLog.Warn(Feature, $"HandleAddItem failed — masterId={masterId}, code={addResult}");
                    return Fail(L("item_spawn_failed"));
                }

                PosWithRot spawnPos = CreateSpawnPos(vPlayer);
                int actorId = (int)SpawnLootingObjectMethod.Invoke(
                    vroom,
                    [
                        item,
                        spawnPos,
                        vPlayer.IsIndoor,
                        ReasonOfSpawn.Admin,
                        0,
                        0,
                        0L,
                        false,
                        false,
                    ])!;

                if (actorId == 0)
                {
                    return Fail(L("item_spawn_failed"));
                }

                ModLog.Info(Feature, $"Spawned item masterId={masterId} in world — uid={vPlayer.UID}.");
                WebDashboardSnapshotCache.MarkDirty();
                return Ok(L("item_spawned_world"), "world");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Item spawn failed — {ex.Message}");
                return Fail(L("item_spawn_failed"));
            }
        }

        private static PosWithRot CreateSpawnPos(VPlayer player)
        {
            VWorld? vworld = GameSessionAccess.TryGetVWorld();
            if (vworld != null)
            {
                Vector3 reachable = vworld.GetReachableDistancePos(
                    player.PositionVector,
                    player.Position.yaw,
                    1f);
                if (reachable != Vector3.zero)
                {
                    PosWithRot pos = reachable.toPosWithRot(0f);
                    pos.yaw = player.Position.yaw - 180f;
                    return pos;
                }
            }

            PosWithRot forward = player.Position.CreateForwardPosWithRot(2f);
            forward.yaw = player.Position.yaw - 180f;
            return forward;
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

        private static WebDashboardSpawnItemResult Ok(string message, string location)
        {
            return new()
            {
                Success = true,
                Message = message,
                Location = location,
            };
        }

        private static WebDashboardSpawnItemResult Fail(string message)
        {
            return new()
            {
                Success = false,
                Message = message,
            };
        }
    }
}
