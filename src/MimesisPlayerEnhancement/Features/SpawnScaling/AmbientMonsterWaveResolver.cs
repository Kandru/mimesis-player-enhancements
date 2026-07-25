namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class AmbientMonsterWaveResolver
    {
        internal static AmbientMonsterWaveMode ParseMode(string? value)
        {
            if (string.Equals(value, "Fixed", StringComparison.OrdinalIgnoreCase))
            {
                return AmbientMonsterWaveMode.Fixed;
            }

            if (string.Equals(value, "Random", StringComparison.OrdinalIgnoreCase))
            {
                return AmbientMonsterWaveMode.Random;
            }

            return AmbientMonsterWaveMode.Vanilla;
        }

        internal static AmbientMonsterWaveMode GetMode(SpawnScalingSceneConfig config)
        {
            return ParseMode(config.AmbientMonsterWaveMode);
        }

        internal static bool IsWaitModeActive(SpawnScalingSceneConfig config)
        {
            return config.EnableSpawnScaling && GetMode(config) != AmbientMonsterWaveMode.Vanilla;
        }

        internal static float RollInitialWaitSeconds(SpawnScalingSceneConfig config)
        {
            float min = config.AmbientMonsterWaveInitialDelayMinSeconds;
            float max = config.AmbientMonsterWaveInitialDelayMaxSeconds;
            return RollSeconds(min, max);
        }

        internal static float ResolveInitialWaitSeconds(SpawnScalingSceneConfig config)
        {
            return GetMode(config) switch
            {
                AmbientMonsterWaveMode.Fixed => config.AmbientMonsterWaveInitialDelaySeconds,
                AmbientMonsterWaveMode.Random => RollInitialWaitSeconds(config),
                _ => 0f,
            };
        }

        internal static int RollWaveIntervalMs(SpawnScalingSceneConfig config)
        {
            return SecondsToMs(RollWaveIntervalSeconds(config));
        }

        internal static int ResolveWaveIntervalMs(SpawnScalingSceneConfig config)
        {
            return GetMode(config) switch
            {
                AmbientMonsterWaveMode.Fixed => SecondsToMs(config.AmbientMonsterWaveIntervalSeconds),
                AmbientMonsterWaveMode.Random => RollWaveIntervalMs(config),
                _ => 0,
            };
        }

        private static float RollWaveIntervalSeconds(SpawnScalingSceneConfig config)
        {
            float min = config.AmbientMonsterWaveIntervalMinSeconds;
            float max = config.AmbientMonsterWaveIntervalMaxSeconds;
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
