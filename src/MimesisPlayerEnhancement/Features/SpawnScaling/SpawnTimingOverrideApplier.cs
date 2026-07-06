using System;
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
            if (jakoMultiplier <= 1f && mimicMultiplier <= 1f)
            {
                state.TimingOverrides = null;
                return;
            }

            SpawnTimingOverrides overrides = new()
            {
                NormalMonsterSpawnTryCount = ScaleTimingCount(info.NormalMonsterSpawnTryCount, jakoMultiplier),
                NormalMonsterSpawnRate = ScaleTimingRate(info.NormalMonsterSpawnRate, jakoMultiplier),
                NormalMonsterSpawnPeriod = ScaleTimingPeriod(info.NormalMonsterSpawnPeriod, jakoMultiplier),
                MimicSpawnTryCount = ScaleTimingCount(info.MimicSpawnTryCount, mimicMultiplier),
                MimicSpawnRate = ScaleTimingRate(info.MimicSpawnRate, mimicMultiplier),
                MimicSpawnPeriod = ScaleTimingPeriod(info.MimicSpawnPeriod, mimicMultiplier),
            };

            state.TimingOverrides = overrides;
            RoomSpawnScalingRegistry.Register(room, state);
        }

        private static int ScaleTimingCount(int vanilla, float multiplier)
        {
            return SpawnMultiplierResolver.ScaleCount(vanilla, multiplier);
        }

        private static int ScaleTimingRate(int vanilla, float multiplier)
        {
            if (vanilla <= 0 || multiplier <= 1f)
            {
                return vanilla;
            }

            return Math.Min(10000, (int)Math.Round(vanilla * multiplier));
        }

        private static int ScaleTimingPeriod(int vanilla, float multiplier)
        {
            if (vanilla <= 0 || multiplier <= 1f)
            {
                return vanilla;
            }

            return Math.Max(1, (int)Math.Round(vanilla / multiplier));
        }
    }
}
