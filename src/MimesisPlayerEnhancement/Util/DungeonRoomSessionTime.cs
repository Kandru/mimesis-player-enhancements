using System;
using System.Reflection;
using HarmonyLib;

namespace MimesisPlayerEnhancement.Util
{
    internal static class DungeonRoomSessionTime
    {
        private static readonly FieldInfo SessionEndTimeField =
            AccessTools.Field(typeof(DungeonRoom), "_sessionEndTime")
            ?? throw new InvalidOperationException("DungeonRoom._sessionEndTime not found");

        private static readonly FieldInfo CurrentTimeField =
            AccessTools.Field(typeof(DungeonRoom), "_currentTime")
            ?? throw new InvalidOperationException("DungeonRoom._currentTime not found");

        internal static bool TryGetRemainingMilliseconds(DungeonRoom room, out long remainingMs)
        {
            remainingMs = 0;
            if (room == null)
            {
                return false;
            }

            long endTime = (long)SessionEndTimeField.GetValue(room);
            long currentTime = (long)CurrentTimeField.GetValue(room);
            remainingMs = endTime - currentTime;
            return remainingMs > 0;
        }

        internal static bool TryExtendEndTime(DungeonRoom room, long bonusMs, out long newEndTime)
        {
            newEndTime = 0;
            if (room == null || bonusMs <= 0)
            {
                return false;
            }

            long endTime = (long)SessionEndTimeField.GetValue(room);
            newEndTime = endTime + bonusMs;
            SessionEndTimeField.SetValue(room, newEndTime);
            return true;
        }
    }
}
