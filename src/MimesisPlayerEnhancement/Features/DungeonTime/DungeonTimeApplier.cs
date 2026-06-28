using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.DungeonTime;

internal static class DungeonTimeApplier
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly FieldInfo SessionEndTimeField =
        AccessToolsField(typeof(DungeonRoom), "_sessionEndTime");

    private static readonly HashSet<DungeonRoom> AppliedRooms = new();

    internal static void EnsureApplied(DungeonRoom room)
    {
        if (AppliedRooms.Contains(room))
            return;

        if (!ModConfig.EnableDungeonTime.Value)
        {
            AppliedRooms.Add(room);
            DungeonTimeLog.DebugSkipped("EnableDungeonTime is off");
            return;
        }

        if (!HostApplyGate.ShouldApplyHostOnlyFeature())
        {
            AppliedRooms.Add(room);
            DungeonTimeLog.DebugSkipped("not host");
            return;
        }

        int playerCount = SessionPlayerCountHelper.ResolveFromRoom(room);
        long bonusMs = DungeonTimeResolver.GetBonusMilliseconds(playerCount);
        if (bonusMs <= 0)
        {
            AppliedRooms.Add(room);
            DungeonTimeLog.DebugSkipped($"no bonus for players={playerCount}");
            return;
        }

        long endTime = (long)SessionEndTimeField.GetValue(room)!;
        long newEndTime = endTime + bonusMs;
        SessionEndTimeField.SetValue(room, newEndTime);
        AppliedRooms.Add(room);
        DungeonTimeLog.InfoApplied(playerCount, bonusMs, newEndTime);
    }

    private static FieldInfo AccessToolsField(Type type, string name)
    {
        var field = type.GetField(name, InstanceFlags);
        if (field == null)
            throw new InvalidOperationException($"{type.Name}.{name} not found");

        return field;
    }
}
