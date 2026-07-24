using System.Reflection;

namespace MimesisPlayerEnhancement.Util
{
    // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L33-37
    internal static class DungeonRoomSessionTime
    {
        // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L33
        private static readonly FieldInfo SessionEndTimeField =
            AccessTools.Field(typeof(DungeonRoom), "_sessionEndTime")
            ?? throw new InvalidOperationException("DungeonRoom._sessionEndTime not found");

        // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L35
        private static readonly FieldInfo CurrentTimeField =
            AccessTools.Field(typeof(DungeonRoom), "_currentTime")
            ?? throw new InvalidOperationException("DungeonRoom._currentTime not found");

        // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L37
        private static readonly FieldInfo ElapsedTimeField =
            AccessTools.Field(typeof(DungeonRoom), "_elapsedTime")
            ?? throw new InvalidOperationException("DungeonRoom._elapsedTime not found");

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

        internal static long GetElapsedMilliseconds(DungeonRoom room)
        {
            return (long)ElapsedTimeField.GetValue(room);
        }

        internal static void SetElapsedMilliseconds(DungeonRoom room, long elapsedMs)
        {
            ElapsedTimeField.SetValue(room, elapsedMs);
        }
    }
}
