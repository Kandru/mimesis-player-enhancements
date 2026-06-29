using System.Collections.Generic;
using System.Reflection;
using Bifrost.Cooked;
using MimesisPlayerEnhancement.Util;
using ReluNetwork.ConstEnum;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeRoomTools
    {
        private const string Feature = "JoinAnytime";

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly PropertyInfo? HubDatamanProperty =
            typeof(Hub).GetProperty("dataman", InstanceFlags);

        private static readonly PropertyInfo? HubVworldProperty =
            typeof(Hub).GetProperty("vworld", InstanceFlags);

        private static readonly FieldInfo? DungeonSessionEndTimeField =
            typeof(DungeonRoom).GetField("_sessionEndTime", InstanceFlags);

        private static readonly FieldInfo? DungeonCurrentTimeField =
            typeof(DungeonRoom).GetField("_currentTime", InstanceFlags);

        internal static JoinAnytimeSessionPhase ResolveHostPhase()
        {
            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (pdata?.ClientMode != NetworkClientMode.Host)
            {
                return JoinAnytimeSessionPhase.None;
            }

            return pdata.main switch
            {
                MaintenanceScene => JoinAnytimeSessionPhase.Maintenance,
                InTramWaitingScene => JoinAnytimeSessionPhase.Tram,
                GamePlayScene => JoinAnytimeSessionPhase.Dungeon,
                _ => JoinAnytimeSessionPhase.None,
            };
        }

        internal static int GetSessionPlayerCount()
        {
            if (!TryGetVRoomManager(out VRoomManager? vroomManager) || vroomManager == null)
            {
                return 0;
            }

            return vroomManager.GetPlayerCountInSession();
        }

        internal static int GetDungeonRemainingMinutes()
        {
            if (GetActiveDungeonRoom() is not DungeonRoom room)
            {
                return 0;
            }

            return GetDungeonRemainingMinutes(room);
        }

        internal static bool AreJoinsOpen()
        {
            JoinAnytimeSessionPhase phase = ResolveHostPhase();
            if (phase is not JoinAnytimeSessionPhase.Maintenance and not JoinAnytimeSessionPhase.Tram)
            {
                return false;
            }

            return !IsSessionJoinsClosed();
        }

        internal static bool IsDungeonReadyForLobbyDisplay()
        {
            if (ResolveHostPhase() != JoinAnytimeSessionPhase.Dungeon)
            {
                return false;
            }

            if (GetActiveDungeonRoom() is not DungeonRoom room)
            {
                return false;
            }

            return GetDungeonRemainingMinutes(room) > 0;
        }

        private static int GetDungeonRemainingMinutes(DungeonRoom room)
        {
            if (DungeonSessionEndTimeField == null || DungeonCurrentTimeField == null)
            {
                return 0;
            }

            long endTime = (long)DungeonSessionEndTimeField.GetValue(room);
            long currentTime = (long)DungeonCurrentTimeField.GetValue(room);
            long remainingMs = endTime - currentTime;
            if (remainingMs <= 0)
            {
                return 0;
            }

            return (int)System.Math.Ceiling(remainingMs / 60000.0);
        }

        private static bool IsSessionJoinsClosed()
        {
            if (!TryGetVRoomManager(out VRoomManager? vroomManager) || vroomManager == null)
            {
                return false;
            }

            VGameSessionState state = vroomManager.GetGameSessionInfo().GameSessionState;
            return state is VGameSessionState.OnPlaying or VGameSessionState.DeathMatch;
        }

        internal static WaitingRoomBlockReason GetWaitingRoomBlockReason()
        {
            if (JoinAnytimeConnectingTracker.HasPending())
            {
                return WaitingRoomBlockReason.PlayersConnecting;
            }

            if (!TryGetVRoomManager(out VRoomManager? vroomManager) || vroomManager == null)
            {
                return WaitingRoomBlockReason.None;
            }

            GameSessionInfo sessionInfo = vroomManager.GetGameSessionInfo();
            if (sessionInfo.GameSessionState == VGameSessionState.OnPlaying)
            {
                return WaitingRoomBlockReason.ActiveDungeon;
            }

            if (CountDungeonPlayers(vroomManager) > 0)
            {
                return WaitingRoomBlockReason.ActiveDungeon;
            }

            int sessionPlayers = vroomManager.GetPlayerCountInSession();
            if (sessionPlayers <= 0)
            {
                return WaitingRoomBlockReason.None;
            }

            int waitingPlayers = vroomManager.GetRoomMemberCount(VRoomType.Waiting);
            if (waitingPlayers < sessionPlayers)
            {
                return WaitingRoomBlockReason.PlayersSplit;
            }

            return WaitingRoomBlockReason.None;
        }

        internal static bool ShouldBlockWaitingRoomStartGame() =>
            GetWaitingRoomBlockReason() != WaitingRoomBlockReason.None;

        internal static void MoveCurrentPlayerToSnapshot(SessionContext context)
        {
            FieldInfo playerField = typeof(SessionContext).GetField("_vPlayer", InstanceFlags);
            if (playerField?.GetValue(context) is not VPlayer player)
            {
                ModLog.Warn(Feature, "MoveCurrentPlayerToSnapshot skipped — _vPlayer not found");
                return;
            }

            IVroom? oldRoom = player.VRoom;
            int oldActorId = player.ObjectID;

            ModLog.Debug(
                Feature,
                $"Removing old player actor={oldActorId} room={oldRoom?.GetType().Name ?? "null"}");

            oldRoom?.PendRemovePlayer(oldActorId, backup: false, kill: false);
            context.CreatePlayerSnapshot(true);
        }

        internal static string GetSceneNameFromMapId(int mapMasterId)
        {
            if (mapMasterId == 0)
            {
                return string.Empty;
            }

            if (!TryGetDataman(out DataManager dataman))
            {
                ModLog.Warn(Feature, "GetSceneNameFromMapId failed — dataman unavailable");
                return string.Empty;
            }

            MapMasterInfo? mapInfo = dataman.ExcelDataManager.GetMapInfo(mapMasterId);
            return mapInfo?.SceneName ?? string.Empty;
        }

        internal static string GetSceneNameFromDungeon(int dungeonMasterId, int pickedMapId = 0)
        {
            int resolvedMapId = pickedMapId != 0 ? pickedMapId : ResolvePickedMapId(null);
            if (resolvedMapId != 0)
            {
                return GetSceneNameFromMapId(resolvedMapId);
            }

            if (!TryGetDataman(out DataManager dataman))
            {
                ModLog.Warn(Feature, "GetSceneNameFromDungeon failed — dataman unavailable");
                return string.Empty;
            }

            DungeonMasterInfo? dungeonInfo = dataman.ExcelDataManager.GetDungeonInfo(dungeonMasterId);
            if (dungeonInfo == null)
            {
                return string.Empty;
            }

            if (dungeonInfo.MapIDs.IsDefaultOrEmpty)
            {
                return string.Empty;
            }

            return GetSceneNameFromMapId(dungeonInfo.MapIDs[0]);
        }

        internal static int ResolvePickedMapId(IVroom? room)
        {
            if (room is DungeonRoom dungeonRoom && dungeonRoom.PickedMapID != 0)
            {
                return dungeonRoom.PickedMapID;
            }

            if (TryGetVRoomManager(out VRoomManager? vroomManager) && vroomManager != null)
            {
                GameSessionInfo sessionInfo = vroomManager.GetGameSessionInfo();
                if (sessionInfo.PickedMapID != 0)
                {
                    return sessionInfo.PickedMapID;
                }
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            return pdata?.PickedMapID ?? 0;
        }

        internal static bool TryEnsureWaitingRoom(out IVroom? waitingRoom)
        {
            waitingRoom = null;
            if (!TryGetVRoomManager(out VRoomManager? vroomManager) || vroomManager == null)
            {
                ModLog.Warn(Feature, "TryEnsureWaitingRoom failed — VRoomManager unavailable");
                return false;
            }

            waitingRoom = TryGetWaitingRoom(vroomManager);
            if (waitingRoom != null)
            {
                return true;
            }

            if (Hub.s == null)
            {
                ModLog.Warn(Feature, "TryEnsureWaitingRoom failed — Hub.s unavailable");
                return false;
            }

            PropertyInfo? vworldProperty = typeof(Hub).GetProperty("vworld", InstanceFlags);
            if (vworldProperty?.GetValue(Hub.s) is not VWorld vworld)
            {
                ModLog.Warn(Feature, "TryEnsureWaitingRoom failed — VWorld unavailable");
                return false;
            }

            ModLog.Info(Feature, "Creating waiting room for late joiner");
            vworld.InitWaitingRoom();
            waitingRoom = TryGetWaitingRoom(vroomManager);
            if (waitingRoom == null)
            {
                ModLog.Warn(Feature, "TryEnsureWaitingRoom failed — room still missing after InitWaitingRoom");
            }

            return waitingRoom != null;
        }

        /// <summary>
        /// After the host re-inits an existing waiting room (dungeon return), players who never
        /// left the tram miss EnterWaitingRoomRes. Push RollDungeonSig so map consoles stay in sync.
        /// </summary>
        internal static void RefreshWaitingRoomDisplaysForOccupants(VRoomManager? vroomManager = null)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            if (vroomManager == null && !TryGetVRoomManager(out vroomManager))
            {
                return;
            }

            if (TryGetWaitingRoom(vroomManager!) is not VWaitingRoom waitingRoom)
            {
                return;
            }

            bool hasLoadedOccupant = false;
            waitingRoom.IterateAllPlayer(player =>
            {
                if (player.LevelLoadCompleted)
                {
                    hasLoadedOccupant = true;
                }
            });

            if (!hasLoadedOccupant)
            {
                return;
            }

            waitingRoom.SendRollDungeonSig();
            ModLog.Info(Feature, "Broadcast RollDungeonSig to refresh tram displays for players already in waiting room");
        }

        internal static IVroom? GetActiveDungeonRoom()
        {
            if (!TryGetVRoomManager(out VRoomManager? vroomManager) || vroomManager == null)
            {
                ModLog.Warn(Feature, "GetActiveDungeonRoom failed — VRoomManager unavailable");
                return null;
            }

            if (ReflectionHelper.GetFieldValue(vroomManager, "_vrooms") is not Dictionary<long, IVroom> rooms)
            {
                return null;
            }

            IVroom? bestOccupied = null;
            int bestOccupiedCount = -1;
            IVroom? newest = null;
            long newestRoomId = long.MinValue;

            foreach (IVroom room in rooms.Values)
            {
                if (room is not DungeonRoom)
                {
                    continue;
                }

                if (room.RoomID > newestRoomId)
                {
                    newest = room;
                    newestRoomId = room.RoomID;
                }

                int memberCount = room.GetMemberCount();
                if (memberCount <= 0)
                {
                    continue;
                }

                if (bestOccupied == null
                    || memberCount > bestOccupiedCount
                    || (memberCount == bestOccupiedCount && room.RoomID > bestOccupied.RoomID))
                {
                    bestOccupied = room;
                    bestOccupiedCount = memberCount;
                }
            }

            return bestOccupied ?? newest;
        }

        private static IVroom? TryGetWaitingRoom(VRoomManager vroomManager)
        {
            if (ReflectionHelper.GetFieldValue(vroomManager, "_vrooms") is not Dictionary<long, IVroom> rooms)
            {
                return null;
            }

            foreach (IVroom room in rooms.Values)
            {
                if (room is VWaitingRoom)
                {
                    return room;
                }
            }

            return null;
        }

        private static int CountDungeonPlayers(VRoomManager vroomManager)
        {
            if (ReflectionHelper.GetFieldValue(vroomManager, "_vrooms") is not Dictionary<long, IVroom> rooms)
            {
                return 0;
            }

            int count = 0;
            foreach (IVroom room in rooms.Values)
            {
                if (room is DungeonRoom)
                {
                    count += room.GetMemberCount();
                }
            }

            return count;
        }

        private static bool TryGetDataman(out DataManager dataman)
        {
            dataman = null!;
            if (Hub.s == null || HubDatamanProperty?.GetValue(Hub.s) is not DataManager resolved)
            {
                return false;
            }

            dataman = resolved;
            return true;
        }

        private static bool TryGetVRoomManager(out VRoomManager? vroomManager)
        {
            vroomManager = null;
            if (Hub.s == null || HubVworldProperty?.GetValue(Hub.s) is not VWorld vworld)
            {
                return false;
            }

            vroomManager = vworld.VRoomManager;
            return vroomManager != null;
        }
    }
}
