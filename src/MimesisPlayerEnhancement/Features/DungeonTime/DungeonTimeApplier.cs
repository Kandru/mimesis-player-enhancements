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

            if (!DungeonRoomSessionTime.TryGetRemainingMilliseconds(room, out long vanillaRemainingMs)
                || vanillaRemainingMs <= 0)
            {
                DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.DungeonTime);
                DungeonTimeLog.DebugSkipped("no remaining session time to scale");
                return;
            }

            long vanillaStartSeconds = DungeonTimeRoomAccess.GetVanillaStartSeconds(room);
            long effectiveStartSeconds = DungeonTimeClockResolver.GetEffectiveStartSeconds(room, config);
            long endSeconds = DungeonTimeRoomAccess.GetEndSeconds(room);
            long baseRemainingMs = DungeonTimeResolver.GetPresetAdjustedRemainingMs(
                vanillaRemainingMs,
                vanillaStartSeconds,
                effectiveStartSeconds,
                endSeconds);

            int playerCount = room.GetMemberCount();
            long bonusMs = DungeonTimeResolver.GetBonusMilliseconds(playerCount, config);
            long targetRemainingMs = baseRemainingMs + bonusMs;
            long deltaMs = targetRemainingMs - vanillaRemainingMs;
            if (deltaMs == 0)
            {
                DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.DungeonTime);
                DungeonTimeLog.DebugSkipped($"no adjustment for players={playerCount}, preset={config.StartTimePreset}");
                return;
            }

            if (!DungeonRoomSessionTime.TryAdjustEndTime(room, deltaMs, out long newEndTime))
            {
                DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.DungeonTime);
                DungeonTimeLog.DebugSkipped("failed to adjust session end time");
                return;
            }

            if (bonusMs > 0)
            {
                DungeonTimeRuntime.ArmDisplayScale(room, baseRemainingMs, bonusMs);
            }

            DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.DungeonTime);
            DungeonTimeLog.InfoApplied(
                playerCount,
                config.StartTimePreset,
                baseRemainingMs,
                bonusMs,
                newEndTime,
                vanillaRemainingMs,
                config);
        }
    }
}
