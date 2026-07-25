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

    internal static class AmbientMonsterWaveApplier
    {
        internal static ManageSpawnDataSnapshot CaptureSnapshot(DungeonRoom room)
        {
            long lastJako = ReadLastSpawnTime(SpawnScalingFields.LastNormalMonsterSpawnTimeField, room);
            long lastMimic = ReadLastSpawnTime(SpawnScalingFields.LastMimicSpawnTimeField, room);
            return new ManageSpawnDataSnapshot(lastJako, lastMimic);
        }

        internal static void ApplyInitialWait(DungeonRoom room, RoomSpawnScalingState state)
        {
            SpawnScalingSceneConfig config = state.HasSnapshot ? state.Snapshot : SceneScopedConfigGate.Spawn;
            if (!config.EnableSpawnScaling || !AmbientMonsterWaveResolver.IsWaitModeActive(config))
            {
                return;
            }

            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            float initialSeconds = AmbientMonsterWaveResolver.ResolveInitialWaitSeconds(config);
            int intervalMs = AmbientMonsterWaveResolver.ResolveWaveIntervalMs(config);
            state.NextJakoWavePeriodMs = intervalMs;
            state.NextMimicWavePeriodMs = intervalMs;

            long now = GameSessionAccess.TryGetTimeUtil()?.GetCurrentTickMilliSec() ?? 0L;
            long initialWaitMs = (long)(initialSeconds * 1000f);
            long lastJako = now - intervalMs + initialWaitMs;
            long lastMimic = now - intervalMs + initialWaitMs;

            SpawnScalingFields.LastNormalMonsterSpawnTimeField.SetValue(room, lastJako);
            SpawnScalingFields.LastMimicSpawnTimeField.SetValue(room, lastMimic);

            SpawnScalingLog.InfoAmbientMonsterWaveApplied(
                AmbientMonsterWaveResolver.GetMode(config),
                initialSeconds,
                intervalMs / 1000f);
        }

        internal static void OnManageSpawnDataPostfix(DungeonRoom room, ManageSpawnDataSnapshot snapshot)
        {
            if (!RoomSpawnScalingRegistry.TryGet(room, out RoomSpawnScalingState? state))
            {
                return;
            }

            SpawnScalingSceneConfig config = state.HasSnapshot ? state.Snapshot : SceneScopedConfigGate.Spawn;
            if (!config.EnableSpawnScaling || !AmbientMonsterWaveResolver.IsWaitModeActive(config))
            {
                return;
            }

            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            if (AmbientMonsterWaveResolver.GetMode(config) != AmbientMonsterWaveMode.Random)
            {
                return;
            }

            long currentJako = ReadLastSpawnTime(SpawnScalingFields.LastNormalMonsterSpawnTimeField, room);
            long currentMimic = ReadLastSpawnTime(SpawnScalingFields.LastMimicSpawnTimeField, room);

            if (currentJako != snapshot.LastNormalMonsterSpawnTime)
            {
                state.NextJakoWavePeriodMs = AmbientMonsterWaveResolver.RollWaveIntervalMs(config);
                SpawnScalingLog.DebugAmbientMonsterWaveIntervalRerolled("jako", state.NextJakoWavePeriodMs / 1000f);
                RefreshTimingOverridePeriod(state);
            }

            if (currentMimic != snapshot.LastMimicSpawnTime)
            {
                state.NextMimicWavePeriodMs = AmbientMonsterWaveResolver.RollWaveIntervalMs(config);
                SpawnScalingLog.DebugAmbientMonsterWaveIntervalRerolled("mimic", state.NextMimicWavePeriodMs / 1000f);
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
