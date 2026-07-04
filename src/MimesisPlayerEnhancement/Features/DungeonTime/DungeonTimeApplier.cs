namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal static class DungeonTimeApplier
    {
        private const string Feature = "DungeonTime";

        internal static void EnsureApplied(DungeonRoom room)
        {
            if (DungeonRoomAppliedSet.IsApplied(room, DungeonRoomApplyKind.DungeonTime))
            {
                return;
            }

            if (!HostApplyGate.ShouldApplyHostOnlyFeature(() => ModConfig.EnableDungeonTime.Value))
            {
                DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.DungeonTime);
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
                DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.DungeonTime);
                DungeonTimeLog.DebugSkipped($"no bonus for players={playerCount}");
                return;
            }

            if (!DungeonRoomSessionTime.TryExtendEndTime(room, bonusMs, out long newEndTime))
            {
                DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.DungeonTime);
                DungeonTimeLog.DebugSkipped("failed to extend session end time");
                return;
            }

            DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.DungeonTime);
            DungeonTimeLog.InfoApplied(playerCount, bonusMs, newEndTime);
        }
    }
}
