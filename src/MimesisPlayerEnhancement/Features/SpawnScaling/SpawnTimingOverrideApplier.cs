namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class SpawnTimingOverrideApplier
    {
        internal static void BeginManageSpawnData(DungeonRoom room)
        {
            if (!RoomSpawnScalingRegistry.TryGet(room, out RoomSpawnScalingState? state)
                || state.TimingOverrides == null)
            {
                return;
            }

            if (SpawnScalingFields.DungeonMasterInfoField.GetValue(room) is not DungeonMasterInfo info)
            {
                return;
            }

            SpawnTimingOverrides overrides = state.TimingOverrides;
            overrides.SavedNormalMonsterSpawnTryCount = info.NormalMonsterSpawnTryCount;
            overrides.SavedNormalMonsterSpawnRate = info.NormalMonsterSpawnRate;
            overrides.SavedNormalMonsterSpawnPeriod = info.NormalMonsterSpawnPeriod;
            overrides.SavedMimicSpawnTryCount = info.MimicSpawnTryCount;
            overrides.SavedMimicSpawnRate = info.MimicSpawnRate;
            overrides.SavedMimicSpawnPeriod = info.MimicSpawnPeriod;

            info.NormalMonsterSpawnTryCount = overrides.NormalMonsterSpawnTryCount;
            info.NormalMonsterSpawnRate = overrides.NormalMonsterSpawnRate;
            info.NormalMonsterSpawnPeriod = overrides.NormalMonsterSpawnPeriod;
            info.MimicSpawnTryCount = overrides.MimicSpawnTryCount;
            info.MimicSpawnRate = overrides.MimicSpawnRate;
            info.MimicSpawnPeriod = overrides.MimicSpawnPeriod;
        }

        internal static void EndManageSpawnData(DungeonRoom room)
        {
            if (!RoomSpawnScalingRegistry.TryGet(room, out RoomSpawnScalingState? state)
                || state.TimingOverrides == null)
            {
                return;
            }

            if (SpawnScalingFields.DungeonMasterInfoField.GetValue(room) is not DungeonMasterInfo info)
            {
                return;
            }

            SpawnTimingOverrides overrides = state.TimingOverrides;
            info.NormalMonsterSpawnTryCount = overrides.SavedNormalMonsterSpawnTryCount;
            info.NormalMonsterSpawnRate = overrides.SavedNormalMonsterSpawnRate;
            info.NormalMonsterSpawnPeriod = overrides.SavedNormalMonsterSpawnPeriod;
            info.MimicSpawnTryCount = overrides.SavedMimicSpawnTryCount;
            info.MimicSpawnRate = overrides.SavedMimicSpawnRate;
            info.MimicSpawnPeriod = overrides.SavedMimicSpawnPeriod;
        }

        internal static void ConfigureTimingOverrides(
            DungeonRoom room,
            RoomSpawnScalingState state,
            DungeonMasterInfo info,
            float gruntMultiplier,
            float mimicMultiplier)
        {
            bool needsTryRateOverride = gruntMultiplier > 1f || mimicMultiplier > 1f;
            bool gruntPeriodActive = AmbientWaveTiming.IsGruntWaitActive(state.Snapshot);
            bool mimicPeriodActive = AmbientWaveTiming.IsMimicWaitActive(state.Snapshot);

            if (!needsTryRateOverride && !gruntPeriodActive && !mimicPeriodActive)
            {
                state.TimingOverrides = null;
                return;
            }

            int gruntPeriod = gruntPeriodActive
                ? state.NextGruntWavePeriodMs
                : info.NormalMonsterSpawnPeriod;
            int mimicPeriod = mimicPeriodActive
                ? state.NextMimicWavePeriodMs
                : info.MimicSpawnPeriod;

            SpawnTimingOverrides overrides = new()
            {
                NormalMonsterSpawnTryCount = needsTryRateOverride && gruntMultiplier > 1f
                    ? SpawnTimingScaleResolver.ScaleTryCount(info.NormalMonsterSpawnTryCount, gruntMultiplier)
                    : info.NormalMonsterSpawnTryCount,
                NormalMonsterSpawnRate = needsTryRateOverride && gruntMultiplier > 1f
                    ? SpawnTimingScaleResolver.ScaleRate(info.NormalMonsterSpawnRate, gruntMultiplier)
                    : info.NormalMonsterSpawnRate,
                NormalMonsterSpawnPeriod = gruntPeriod,
                MimicSpawnTryCount = needsTryRateOverride && mimicMultiplier > 1f
                    ? SpawnTimingScaleResolver.ScaleTryCount(info.MimicSpawnTryCount, mimicMultiplier)
                    : info.MimicSpawnTryCount,
                MimicSpawnRate = needsTryRateOverride && mimicMultiplier > 1f
                    ? SpawnTimingScaleResolver.ScaleRate(info.MimicSpawnRate, mimicMultiplier)
                    : info.MimicSpawnRate,
                MimicSpawnPeriod = mimicPeriod,
            };

            state.TimingOverrides = overrides;
        }
    }
}
