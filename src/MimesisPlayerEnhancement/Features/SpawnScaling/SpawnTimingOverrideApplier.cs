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
            float jakoMultiplier,
            float mimicMultiplier)
        {
            bool needsTryRateOverride = jakoMultiplier > 1f || mimicMultiplier > 1f;
            bool needsPeriodOverride = PeriodicSpawnWaitResolver.IsWaitModeActive();

            if (!needsTryRateOverride && !needsPeriodOverride)
            {
                state.TimingOverrides = null;
                return;
            }

            int jakoPeriod = needsPeriodOverride
                ? state.NextJakoWavePeriodMs
                : info.NormalMonsterSpawnPeriod;
            int mimicPeriod = needsPeriodOverride
                ? state.NextMimicWavePeriodMs
                : info.MimicSpawnPeriod;

            SpawnTimingOverrides overrides = new()
            {
                NormalMonsterSpawnTryCount = needsTryRateOverride && jakoMultiplier > 1f
                    ? ScaleTimingCount(info.NormalMonsterSpawnTryCount, jakoMultiplier)
                    : info.NormalMonsterSpawnTryCount,
                NormalMonsterSpawnRate = needsTryRateOverride && jakoMultiplier > 1f
                    ? ScaleTimingRate(info.NormalMonsterSpawnRate, jakoMultiplier)
                    : info.NormalMonsterSpawnRate,
                NormalMonsterSpawnPeriod = jakoPeriod,
                MimicSpawnTryCount = needsTryRateOverride && mimicMultiplier > 1f
                    ? ScaleTimingCount(info.MimicSpawnTryCount, mimicMultiplier)
                    : info.MimicSpawnTryCount,
                MimicSpawnRate = needsTryRateOverride && mimicMultiplier > 1f
                    ? ScaleTimingRate(info.MimicSpawnRate, mimicMultiplier)
                    : info.MimicSpawnRate,
                MimicSpawnPeriod = mimicPeriod,
            };

            state.TimingOverrides = overrides;
        }

        private static int ScaleTimingCount(int vanilla, float multiplier)
        {
            return ScalingMath.ScaleCount(vanilla, multiplier);
        }

        private static int ScaleTimingRate(int vanilla, float multiplier)
        {
            if (vanilla <= 0 || multiplier <= 1f)
            {
                return vanilla;
            }

            return Math.Min(10000, (int)Math.Round(vanilla * multiplier));
        }
    }
}
