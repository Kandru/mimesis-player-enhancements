namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal static class DungeonTimeApplier
    {
        internal static void EnsureApplied(DungeonRoom room)
        {
            if (DungeonRoomAppliedSet.IsApplied(room, DungeonRoomApplyKind.DungeonTime))
            {
                return;
            }

            DungeonTimeSceneConfig config = SceneScopedConfigGate.DungeonTime;
            if (!HostApplyGate.ShouldApplyHostOnlyFeature(() => config.EnableDungeonTime))
            {
                DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.DungeonTime);
                if (!config.EnableDungeonTime)
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
            long bonusMs = DungeonTimeResolver.GetBonusMilliseconds(playerCount, config);
            if (bonusMs <= 0)
            {
                DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.DungeonTime);
                DungeonTimeLog.DebugSkipped($"no bonus for players={playerCount}");
                return;
            }

            if (!DungeonRoomSessionTime.TryGetRemainingMilliseconds(room, out long vanillaRemainingMs)
                || vanillaRemainingMs <= 0)
            {
                DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.DungeonTime);
                DungeonTimeLog.DebugSkipped("no remaining session time to scale");
                return;
            }

            if (!DungeonRoomSessionTime.TryExtendEndTime(room, bonusMs, out long newEndTime))
            {
                DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.DungeonTime);
                DungeonTimeLog.DebugSkipped("failed to extend session end time");
                return;
            }

            DungeonTimeRuntime.ArmDisplayScale(room, vanillaRemainingMs, bonusMs);
            DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.DungeonTime);
            DungeonTimeLog.InfoApplied(playerCount, bonusMs, newEndTime, vanillaRemainingMs, config);
        }
    }
}
