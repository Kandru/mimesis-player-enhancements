using System.Reflection;
using Bifrost.Cooked;
using MimesisPlayerEnhancement.Features.WebDashboard;
using ReluNetwork.ConstEnum;
using ReluServerBase.Threading;

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

        internal static bool AreJoinsOpen()
        {
            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            if (pdata?.ClientMode != NetworkClientMode.Host)
            {
                return false;
            }

            if (pdata.main is not MaintenanceScene and not InTramWaitingScene)
            {
                return false;
            }

            if (!TryGetVRoomManager(out VRoomManager? vroomManager))
            {
                return true;
            }

            VGameSessionState state = vroomManager!.GetGameSessionInfo().GameSessionState;
            return state is not (VGameSessionState.OnPlaying or VGameSessionState.DeathMatch or VGameSessionState.AfterGame);
        }

        internal static bool TryGetActiveDungeonWaitMinutes(out int minutes)
        {
            minutes = 0;
            if (GetActiveDungeonRoom() is not DungeonRoom room
                || !DungeonRoomSessionTime.TryGetRemainingMilliseconds(room, out long remainingMs))
            {
                return false;
            }

            minutes = (int)System.Math.Ceiling(remainingMs / 60000.0);
            return minutes > 0;
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
            VGameSessionState sessionState = sessionInfo.GameSessionState;
            if (sessionState is VGameSessionState.OnPlaying or VGameSessionState.AfterGame)
            {
                return WaitingRoomBlockReason.ActiveDungeon;
            }

            if (sessionState is VGameSessionState.DeathMatch)
            {
                return WaitingRoomBlockReason.None;
            }

            if (TryScanRooms(vroomManager, out RoomScanResult scan)
                && scan.DungeonPlayerCount > 0
                && scan.ActiveDungeonRoom is DungeonRoom activeDungeon
                && !activeDungeon.IsSessionEnding)
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
            if (WebDashboardSessionAccess.GetVPlayer(context) is not VPlayer player)
            {
                ModLog.Warn(Feature, "MoveCurrentPlayerToSnapshot skipped — _vPlayer not found");
                return;
            }

            IVroom? oldRoom = player.VRoom;
            int oldActorId = player.ObjectID;

            ModLog.Debug(Feature, $"Removing old player actor={oldActorId} room={oldRoom?.GetType().Name ?? "null"}");

            oldRoom?.PendRemovePlayer(oldActorId, backup: false, kill: false);
            context.CreatePlayerSnapshot(true);
        }

        /// <summary>
        /// Vanilla maintenance→tram sends MoveToWaitingRoomSig then removes players from the
        /// maintenance room, which emits LeaveRoomSig. Clients wait for serverRoomState=Nowhere
        /// before loading InTramWaitingScene.
        /// </summary>
        internal static void ReleaseLateJoinerFromMaintenance(VPlayer player)
        {
            if (player.VRoom is not MaintenanceRoom maintenanceRoom)
            {
                return;
            }

            ModLog.Info(Feature, $"Releasing late joiner uid={player.UID} from maintenance — awaiting EnterWaitingRoomReq");

            maintenanceRoom.PendRemovePlayer(player.ObjectID, backup: false, kill: false);
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
                PrepareWaitingRoomForEnter(vroomManager);
                return true;
            }

            if (Hub.s == null)
            {
                ModLog.Warn(Feature, "TryEnsureWaitingRoom failed — Hub.s unavailable");
                return false;
            }

            if (HubVworldProperty?.GetValue(Hub.s) is not VWorld vworld)
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
            else
            {
                PrepareWaitingRoomForEnter(vroomManager);
            }

            return waitingRoom != null;
        }

        private static long _preparedWaitingRoomId;
        private static int _preparedStageCount = -1;
        private static int _preparedCycleCount = -1;

        internal static void InvalidateWaitingRoomPrepareCache()
        {
            _preparedWaitingRoomId = 0;
            _preparedStageCount = -1;
            _preparedCycleCount = -1;
        }

        internal static void EnsureWaitingRoomEnterReady()
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            if (!TryGetVRoomManager(out VRoomManager? vroomManager))
            {
                return;
            }

            if (TryGetWaitingRoom(vroomManager!) is not VWaitingRoom waitingRoom)
            {
                return;
            }

            EnsureWaitingRoomPlayable(waitingRoom);
            if (!IsWaitingRoomPrepared(waitingRoom, vroomManager!))
            {
                PrepareWaitingRoomForEnter(vroomManager, force: true);
            }
        }

        /// <summary>
        /// Join-anytime keeps the waiting room alive while the party is in a dungeon. On return,
        /// vanilla InitWaitingRoom queues ResetEnvironment asynchronously but broadcasts
        /// MakeRoomCompleteSig immediately. EnterWaitingRoomReq can arrive before spawn points
        /// exist; ProcessEnterWaitQueue then drops the request without a response (60s client timeout).
        /// </summary>
        internal static void PrepareWaitingRoomForEnter(VRoomManager? vroomManager = null, bool force = false)
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

            if (!force && IsWaitingRoomPrepared(waitingRoom, vroomManager!))
            {
                EnsureWaitingRoomPlayable(waitingRoom);
                return;
            }

            FlushVRoomManagerCommands(vroomManager!);
            EnsureWaitingRoomPlayable(waitingRoom);
            FlushRoomCommands(waitingRoom);
            EnsurePlayerStartPoints(waitingRoom);
            MarkWaitingRoomPrepared(waitingRoom, vroomManager!);
        }

        internal static void PrepareWaitingRoomBeforeBroadcast(VRoomManager vroomManager)
        {
            PrepareWaitingRoomForEnter(vroomManager, force: true);
        }

        internal static void PrepareWaitingRoomAfterDungeonSuccess()
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            PrepareWaitingRoomForEnter(force: true);
        }

        private static bool IsWaitingRoomPrepared(VWaitingRoom waitingRoom, VRoomManager vroomManager)
        {
            if (_preparedWaitingRoomId != waitingRoom.RoomID)
            {
                return false;
            }

            GameSessionInfo sessionInfo = vroomManager.GetGameSessionInfo();
            return sessionInfo.StageCount == _preparedStageCount
                && sessionInfo.CycleCount == _preparedCycleCount;
        }

        private static void MarkWaitingRoomPrepared(VWaitingRoom waitingRoom, VRoomManager vroomManager)
        {
            GameSessionInfo sessionInfo = vroomManager.GetGameSessionInfo();
            _preparedWaitingRoomId = waitingRoom.RoomID;
            _preparedStageCount = sessionInfo.StageCount;
            _preparedCycleCount = sessionInfo.CycleCount;
        }

        private static void EnsureWaitingRoomPlayable(VWaitingRoom waitingRoom)
        {
            MethodInfo? resumeRoom = typeof(IVroom).GetMethod("ResumeRoom", InstanceFlags);
            resumeRoom?.Invoke(waitingRoom, null);
            waitingRoom.SetState(WaitingRoomState.Ready);
        }

        private static void FlushVRoomManagerCommands(VRoomManager vroomManager)
        {
            if (ReflectionHelper.GetFieldValue(vroomManager, "_commandExecutor") is CommandExecutor executor)
            {
                executor.Execute();
            }
        }

        private static void FlushRoomCommands(VWaitingRoom waitingRoom)
        {
            waitingRoom.GetCommandExecutor()?.Execute();
        }

        private static void EnsurePlayerStartPoints(VWaitingRoom waitingRoom)
        {
            if (ReflectionHelper.GetFieldValue(waitingRoom, "_playerStartSpawnPoints") is Dictionary<int, SpawnPointData> points
                && points.Count > 0)
            {
                return;
            }

            waitingRoom.InitLevel();
            waitingRoom.InitSpawn();
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

            return TryScanRooms(vroomManager, out RoomScanResult scan) ? scan.ActiveDungeonRoom : null;
        }

        private struct RoomScanResult
        {
            internal IVroom? WaitingRoom;
            internal IVroom? ActiveDungeonRoom;
            internal int DungeonPlayerCount;
        }

        private static bool TryScanRooms(VRoomManager vroomManager, out RoomScanResult scan)
        {
            scan = default;
            if (ReflectionHelper.GetFieldValue(vroomManager, "_vrooms") is not Dictionary<long, IVroom> rooms)
            {
                return false;
            }

            IVroom? waitingRoom = null;
            IVroom? bestOccupied = null;
            int bestOccupiedCount = -1;
            IVroom? newest = null;
            long newestRoomId = long.MinValue;
            int dungeonPlayerCount = 0;

            foreach (IVroom room in rooms.Values)
            {
                if (room is VWaitingRoom)
                {
                    waitingRoom = room;
                    continue;
                }

                if (room is not DungeonRoom)
                {
                    continue;
                }

                int memberCount = room.GetMemberCount();
                dungeonPlayerCount += memberCount;

                if (room.RoomID > newestRoomId)
                {
                    newest = room;
                    newestRoomId = room.RoomID;
                }

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

            scan = new RoomScanResult
            {
                WaitingRoom = waitingRoom,
                ActiveDungeonRoom = bestOccupied ?? newest,
                DungeonPlayerCount = dungeonPlayerCount,
            };
            return true;
        }

        private static IVroom? TryGetWaitingRoom(VRoomManager vroomManager)
        {
            return TryScanRooms(vroomManager, out RoomScanResult scan) ? scan.WaitingRoom : null;
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
