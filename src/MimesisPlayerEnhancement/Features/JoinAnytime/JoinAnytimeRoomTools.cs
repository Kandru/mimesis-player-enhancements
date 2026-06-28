using System.Collections.Generic;
using System.Reflection;
using Bifrost.Cooked;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeRoomTools
    {
        private const string Feature = "JoinAnytime";

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly PropertyInfo? HubDatamanProperty =
            typeof(Hub).GetProperty("dataman", InstanceFlags);

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

            if (Hub.s == null || HubDatamanProperty?.GetValue(Hub.s) is not DataManager dataman)
            {
                ModLog.Warn(Feature, "GetSceneNameFromMapId failed — dataman unavailable");
                return string.Empty;
            }

            MapMasterInfo? mapInfo = dataman.ExcelDataManager.GetMapInfo(mapMasterId);
            return mapInfo?.SceneName ?? string.Empty;
        }

        internal static string GetSceneNameFromDungeon(int dungeonMasterId, int pickedMapId = 0)
        {
            if (pickedMapId != 0)
            {
                return GetSceneNameFromMapId(pickedMapId);
            }

            if (Hub.s == null || HubDatamanProperty?.GetValue(Hub.s) is not DataManager dataman)
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

        internal static int GetPickedMapId(IVroom? room)
        {
            return room is DungeonRoom dungeonRoom ? dungeonRoom.PickedMapID : 0;
        }

        internal static IVroom? GetActiveDungeonRoom()
        {
            if (Hub.s == null)
            {
                return null;
            }

            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            long preferredRoomUid = 0;
            if (pdata?.main is GamePlayScene gps)
            {
                FieldInfo roomUidField = typeof(GamePlayScene).GetField("RoomUID", InstanceFlags)
                                   ?? typeof(GamePlayScene).GetField("roomUID", InstanceFlags);
                if (roomUidField != null)
                {
                    preferredRoomUid = System.Convert.ToInt64(roomUidField.GetValue(gps));
                }
            }

            FieldInfo vworldField = typeof(Hub).GetField("<vworld>k__BackingField", InstanceFlags)
                              ?? typeof(Hub).GetField("vworld", InstanceFlags);
            if (vworldField?.GetValue(Hub.s) is not VWorld vworld)
            {
                ModLog.Warn(Feature, "GetActiveDungeonRoom failed — vworld unavailable");
                return null;
            }

            VRoomManager? vroomManager = vworld.VRoomManager;
            if (vroomManager == null)
            {
                return null;
            }

            if (ReflectionHelper.GetFieldValue(vroomManager, "_vrooms") is not Dictionary<long, IVroom> rooms)
            {
                return null;
            }

            IVroom? fallback = null;
            foreach (IVroom room in rooms.Values)
            {
                if (room is not DungeonRoom)
                {
                    continue;
                }

                fallback ??= room;
                if (preferredRoomUid != 0 && room.RoomID == preferredRoomUid)
                {
                    return room;
                }
            }

            return fallback;
        }
    }
}
