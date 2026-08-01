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

    internal static class AmbientWaveApplier
    {
        internal static ManageSpawnDataSnapshot CaptureSnapshot(DungeonRoom room)
        {
            long lastGrunt = ReadLastSpawnTime(SpawnScalingFields.LastNormalMonsterSpawnTimeField, room);
            long lastMimic = ReadLastSpawnTime(SpawnScalingFields.LastMimicSpawnTimeField, room);
            return new ManageSpawnDataSnapshot(lastGrunt, lastMimic);
        }

        internal static void ApplyInitialWait(DungeonRoom room, RoomSpawnScalingState state)
        {
            SpawnScalingSceneConfig config = state.HasSnapshot ? state.Snapshot : SceneScopedConfigGate.Spawn;
            if (!config.EnableSpawnScaling)
            {
                return;
            }

            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            bool gruntActive = AmbientWaveTiming.IsGruntWaitActive(config);
            bool mimicActive = AmbientWaveTiming.IsMimicWaitActive(config);
            if (!gruntActive && !mimicActive)
            {
                return;
            }

            long now = GameSessionAccess.TryGetTimeUtil()?.GetCurrentTickMilliSec() ?? 0L;

            if (gruntActive)
            {
                float initialSeconds = AmbientWaveTiming.ResolveGruntInitialWaitSeconds(config);
                int intervalMs = AmbientWaveTiming.ResolveGruntWaveIntervalMs(config);
                state.NextGruntWavePeriodMs = intervalMs;
                long lastGrunt = now - intervalMs + (long)(initialSeconds * 1000f);
                SpawnScalingFields.LastNormalMonsterSpawnTimeField.SetValue(room, lastGrunt);
                SpawnScalingLog.InfoAmbientWaveApplied(
                    "grunt",
                    AmbientWaveTiming.GetGruntMode(config),
                    initialSeconds,
                    intervalMs / 1000f);
            }

            if (mimicActive)
            {
                float initialSeconds = AmbientWaveTiming.ResolveMimicInitialWaitSeconds(config);
                int intervalMs = AmbientWaveTiming.ResolveMimicWaveIntervalMs(config);
                state.NextMimicWavePeriodMs = intervalMs;
                long lastMimic = now - intervalMs + (long)(initialSeconds * 1000f);
                SpawnScalingFields.LastMimicSpawnTimeField.SetValue(room, lastMimic);
                SpawnScalingLog.InfoAmbientWaveApplied(
                    "mimic",
                    AmbientWaveTiming.GetMimicMode(config),
                    initialSeconds,
                    intervalMs / 1000f);
            }
        }

        internal static void OnManageSpawnDataPostfix(DungeonRoom room, ManageSpawnDataSnapshot snapshot)
        {
            if (!RoomSpawnScalingRegistry.TryGet(room, out RoomSpawnScalingState? state))
            {
                return;
            }

            SpawnScalingSceneConfig config = state.HasSnapshot ? state.Snapshot : SceneScopedConfigGate.Spawn;
            if (!config.EnableSpawnScaling)
            {
                return;
            }

            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            bool refreshed = false;

            if (AmbientWaveTiming.IsGruntWaitActive(config)
                && AmbientWaveTiming.GetGruntMode(config) == AmbientWaveMode.Random)
            {
                long currentGrunt = ReadLastSpawnTime(SpawnScalingFields.LastNormalMonsterSpawnTimeField, room);
                if (currentGrunt != snapshot.LastNormalMonsterSpawnTime)
                {
                    state.NextGruntWavePeriodMs = AmbientWaveTiming.RollGruntWaveIntervalMs(config);
                    SpawnScalingLog.DebugAmbientWaveIntervalRerolled("grunt", state.NextGruntWavePeriodMs / 1000f);
                    refreshed = true;
                }
            }

            if (AmbientWaveTiming.IsMimicWaitActive(config)
                && AmbientWaveTiming.GetMimicMode(config) == AmbientWaveMode.Random)
            {
                long currentMimic = ReadLastSpawnTime(SpawnScalingFields.LastMimicSpawnTimeField, room);
                if (currentMimic != snapshot.LastMimicSpawnTime)
                {
                    state.NextMimicWavePeriodMs = AmbientWaveTiming.RollMimicWaveIntervalMs(config);
                    SpawnScalingLog.DebugAmbientWaveIntervalRerolled("mimic", state.NextMimicWavePeriodMs / 1000f);
                    refreshed = true;
                }
            }

            if (refreshed)
            {
                RefreshTimingOverridePeriod(state);
            }
        }

        private static void RefreshTimingOverridePeriod(RoomSpawnScalingState state)
        {
            if (state.TimingOverrides == null)
            {
                return;
            }

            SpawnScalingSceneConfig config = state.HasSnapshot ? state.Snapshot : SceneScopedConfigGate.Spawn;
            if (AmbientWaveTiming.IsGruntWaitActive(config))
            {
                state.TimingOverrides.NormalMonsterSpawnPeriod = state.NextGruntWavePeriodMs;
            }

            if (AmbientWaveTiming.IsMimicWaitActive(config))
            {
                state.TimingOverrides.MimicSpawnPeriod = state.NextMimicWavePeriodMs;
            }
        }

        private static long ReadLastSpawnTime(System.Reflection.FieldInfo field, DungeonRoom room)
        {
            return (long)(field.GetValue(room) ?? 0L);
        }
    }
}
