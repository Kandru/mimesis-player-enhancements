using System.Collections.Generic;
using System.Reflection;
using Bifrost.Cooked;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.JoinAnytime;

internal static class JoinAnytimeRoomTools
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void MoveCurrentPlayerToSnapshot(SessionContext context)
    {
        var playerField = typeof(SessionContext).GetField("_vPlayer", InstanceFlags);
        if (playerField?.GetValue(context) is not VPlayer player)
            return;

        IVroom? oldRoom = player.VRoom;
        int oldActorId = player.ObjectID;

        LateJoinManager.Log(
            $"Removing old player actor={oldActorId} room={oldRoom?.GetType().Name ?? "null"}");

        oldRoom?.PendRemovePlayer(oldActorId, backup: false, kill: false);
        context.CreatePlayerSnapshot(true);
    }

    internal static string GetSceneNameFromDungeon(int dungeonMasterId)
    {
        var datamanField = typeof(Hub).GetField("dataman", InstanceFlags);
        if (Hub.s == null || datamanField?.GetValue(Hub.s) is not DataManager dataman)
            return string.Empty;

        DungeonMasterInfo? dungeonInfo = dataman.ExcelDataManager.GetDungeonInfo(dungeonMasterId);
        if (dungeonInfo == null)
            return string.Empty;

        if (dungeonInfo.MapIDs.IsDefaultOrEmpty)
            return string.Empty;

        MapMasterInfo? mapInfo = dataman.ExcelDataManager.GetMapInfo(dungeonInfo.MapIDs[0]);
        return mapInfo?.SceneName ?? string.Empty;
    }

    internal static IVroom? GetActiveDungeonRoom()
    {
        if (Hub.s == null)
            return null;

        var vworldField = typeof(Hub).GetField("<vworld>k__BackingField", InstanceFlags)
                          ?? typeof(Hub).GetField("vworld", InstanceFlags);
        if (vworldField?.GetValue(Hub.s) is not VWorld vworld)
            return null;

        VRoomManager? vroomManager = vworld.VRoomManager;
        if (vroomManager == null)
            return null;

        if (ReflectionHelper.GetFieldValue(vroomManager, "_vrooms") is not Dictionary<long, IVroom> rooms)
            return null;

        foreach (IVroom room in rooms.Values)
        {
            if (room is DungeonRoom)
                return room;
        }

        return null;
    }
}
