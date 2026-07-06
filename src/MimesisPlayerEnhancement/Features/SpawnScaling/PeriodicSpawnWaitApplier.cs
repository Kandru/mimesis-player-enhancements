namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal readonly struct ManageSpawnDataSnapshot
    {
        internal ManageSpawnDataSnapshot(long lastNormalMonsterSpawnTime, long lastMimicSpawnTime)
        {
            LastNormalMonsterSpawnTime = lastNormalMonsterSpawnTime;
            LastMimicSpawnTime = lastMimicSpawnTime;
        }

        internal long LastNormalMonsterSpawnTime { get; }

        internal long LastMimicSpawnTime { get; }
    }

    internal static class PeriodicSpawnWaitApplier
    {
        internal static ManageSpawnDataSnapshot CaptureSnapshot(DungeonRoom room)
        {
            long lastJako = ReadLastSpawnTime(SpawnScalingFields.LastNormalMonsterSpawnTimeField, room);
            long lastMimic = ReadLastSpawnTime(SpawnScalingFields.LastMimicSpawnTimeField, room);
            return new ManageSpawnDataSnapshot(lastJako, lastMimic);
        }

        internal static void ApplyInitialWait(DungeonRoom room, RoomSpawnScalingState state)
        {
            if (!PeriodicSpawnWaitResolver.ShouldApplyHostWaitOverrides())
            {
                return;
            }

            float initialSeconds = PeriodicSpawnWaitResolver.ResolveInitialWaitSeconds();
            int intervalMs = PeriodicSpawnWaitResolver.ResolveWaveIntervalMs();
            state.NextJakoWavePeriodMs = intervalMs;
            state.NextMimicWavePeriodMs = intervalMs;

            long now = GameSessionAccess.TryGetTimeUtil()?.GetCurrentTickMilliSec() ?? 0L;
            long initialWaitMs = (long)(initialSeconds * 1000f);
            long lastJako = now - intervalMs + initialWaitMs;
            long lastMimic = now - intervalMs + initialWaitMs;

            SpawnScalingFields.LastNormalMonsterSpawnTimeField.SetValue(room, lastJako);
            SpawnScalingFields.LastMimicSpawnTimeField.SetValue(room, lastMimic);

            SpawnScalingLog.InfoPeriodicSpawnWaitApplied(
                PeriodicSpawnWaitResolver.GetMode(),
                initialSeconds,
                intervalMs / 1000f);
        }

        internal static void OnManageSpawnDataPostfix(DungeonRoom room, ManageSpawnDataSnapshot snapshot)
        {
            if (!PeriodicSpawnWaitResolver.ShouldApplyHostWaitOverrides())
            {
                return;
            }

            if (PeriodicSpawnWaitResolver.GetMode() != PeriodicSpawnWaitMode.Random)
            {
                return;
            }

            if (!RoomSpawnScalingRegistry.TryGet(room, out RoomSpawnScalingState? state))
            {
                return;
            }

            long currentJako = ReadLastSpawnTime(SpawnScalingFields.LastNormalMonsterSpawnTimeField, room);
            long currentMimic = ReadLastSpawnTime(SpawnScalingFields.LastMimicSpawnTimeField, room);

            if (currentJako != snapshot.LastNormalMonsterSpawnTime)
            {
                state.NextJakoWavePeriodMs = PeriodicSpawnWaitResolver.RollWaveIntervalMs();
                SpawnScalingLog.DebugPeriodicSpawnIntervalRerolled("jako", state.NextJakoWavePeriodMs / 1000f);
                RefreshTimingOverridePeriod(state);
            }

            if (currentMimic != snapshot.LastMimicSpawnTime)
            {
                state.NextMimicWavePeriodMs = PeriodicSpawnWaitResolver.RollWaveIntervalMs();
                SpawnScalingLog.DebugPeriodicSpawnIntervalRerolled("mimic", state.NextMimicWavePeriodMs / 1000f);
                RefreshTimingOverridePeriod(state);
            }
        }

        private static void RefreshTimingOverridePeriod(RoomSpawnScalingState state)
        {
            if (state.TimingOverrides == null)
            {
                return;
            }

            state.TimingOverrides.NormalMonsterSpawnPeriod = state.NextJakoWavePeriodMs;
            state.TimingOverrides.MimicSpawnPeriod = state.NextMimicWavePeriodMs;
        }

        private static long ReadLastSpawnTime(System.Reflection.FieldInfo field, DungeonRoom room)
        {
            return (long)(field.GetValue(room) ?? 0L);
        }
    }
}
