namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class PeriodicSpawnWaitResolver
    {
        internal static PeriodicSpawnWaitMode ParseMode(string? value)
        {
            if (string.Equals(value, "Fixed", StringComparison.OrdinalIgnoreCase))
            {
                return PeriodicSpawnWaitMode.Fixed;
            }

            if (string.Equals(value, "Random", StringComparison.OrdinalIgnoreCase))
            {
                return PeriodicSpawnWaitMode.Random;
            }

            return PeriodicSpawnWaitMode.Vanilla;
        }

        internal static PeriodicSpawnWaitMode GetMode()
        {
            return GetMode(SceneScopedConfigGate.Spawn);
        }

        internal static PeriodicSpawnWaitMode GetMode(SpawnScalingSceneConfig config)
        {
            return ParseMode(config.PeriodicSpawnWaitMode);
        }

        internal static bool IsWaitModeActive()
        {
            return IsWaitModeActive(SceneScopedConfigGate.Spawn);
        }

        internal static bool IsWaitModeActive(SpawnScalingSceneConfig config)
        {
            return config.EnableSpawnScaling && GetMode(config) != PeriodicSpawnWaitMode.Vanilla;
        }

        internal static float RollInitialWaitSeconds()
        {
            return RollInitialWaitSeconds(SceneScopedConfigGate.Spawn);
        }

        internal static float RollInitialWaitSeconds(SpawnScalingSceneConfig config)
        {
            float min = config.InitialPeriodicSpawnWaitMinSeconds;
            float max = config.InitialPeriodicSpawnWaitMaxSeconds;
            return RollSeconds(min, max);
        }

        internal static float ResolveInitialWaitSeconds()
        {
            return ResolveInitialWaitSeconds(SceneScopedConfigGate.Spawn);
        }

        internal static float ResolveInitialWaitSeconds(SpawnScalingSceneConfig config)
        {
            return GetMode(config) switch
            {
                PeriodicSpawnWaitMode.Fixed => config.InitialPeriodicSpawnWaitSeconds,
                PeriodicSpawnWaitMode.Random => RollInitialWaitSeconds(config),
                _ => 0f,
            };
        }

        internal static int RollWaveIntervalMs()
        {
            return RollWaveIntervalMs(SceneScopedConfigGate.Spawn);
        }

        internal static int RollWaveIntervalMs(SpawnScalingSceneConfig config)
        {
            return SecondsToMs(RollWaveIntervalSeconds(config));
        }

        internal static int ResolveWaveIntervalMs()
        {
            return ResolveWaveIntervalMs(SceneScopedConfigGate.Spawn);
        }

        internal static int ResolveWaveIntervalMs(SpawnScalingSceneConfig config)
        {
            return GetMode(config) switch
            {
                PeriodicSpawnWaitMode.Fixed => SecondsToMs(config.PeriodicSpawnIntervalSeconds),
                PeriodicSpawnWaitMode.Random => RollWaveIntervalMs(config),
                _ => 0,
            };
        }

        private static float RollWaveIntervalSeconds(SpawnScalingSceneConfig config)
        {
            float min = config.PeriodicSpawnIntervalMinSeconds;
            float max = config.PeriodicSpawnIntervalMaxSeconds;
            return RollSeconds(min, max);
        }

        private static float RollSeconds(float min, float max)
        {
            return min >= max ? min : UnityEngine.Random.Range(min, max);
        }

        private static int SecondsToMs(float seconds)
        {
            if (seconds <= 0f)
            {
                return 0;
            }

            return Math.Max(1, (int)Math.Round(seconds * 1000f));
        }
    }
}
