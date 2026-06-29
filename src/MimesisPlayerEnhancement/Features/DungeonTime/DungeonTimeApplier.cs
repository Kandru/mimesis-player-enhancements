using System.Collections.Generic;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal static class DungeonTimeApplier
    {
        private static readonly HashSet<DungeonRoom> AppliedRooms = [];

        internal static void EnsureApplied(DungeonRoom room)
        {
            if (AppliedRooms.Contains(room))
            {
                return;
            }

            if (!HostApplyGate.ShouldApplyHostOnlyFeature(() => ModConfig.EnableDungeonTime.Value))
            {
                _ = AppliedRooms.Add(room);
                if (!ModConfig.EnableDungeonTime.Value)
                {
                    DungeonTimeLog.DebugSkipped("EnableDungeonTime is off");
                }
                else
                {
                    DungeonTimeLog.DebugSkipped("not host");
                }

                return;
            }

            int playerCount = room.GetMemberCount();
            long bonusMs = DungeonTimeResolver.GetBonusMilliseconds(playerCount);
            if (bonusMs <= 0)
            {
                _ = AppliedRooms.Add(room);
                DungeonTimeLog.DebugSkipped($"no bonus for players={playerCount}");
                return;
            }

            if (!DungeonRoomSessionTime.TryExtendEndTime(room, bonusMs, out long newEndTime))
            {
                _ = AppliedRooms.Add(room);
                DungeonTimeLog.DebugSkipped("failed to extend session end time");
                return;
            }

            _ = AppliedRooms.Add(room);
            DungeonTimeLog.InfoApplied(playerCount, bonusMs, newEndTime);
        }
    }
}
