namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    /// <summary>
    /// Shared Vanilla / Fixed / Random math for grunt and mimic ambient wave timing.
    /// </summary>
    internal static class AmbientWaveTiming
    {
        internal static AmbientWaveMode ParseMode(string? value)
        {
            if (string.Equals(value, "Fixed", StringComparison.OrdinalIgnoreCase))
            {
                return AmbientWaveMode.Fixed;
            }

            if (string.Equals(value, "Random", StringComparison.OrdinalIgnoreCase))
            {
                return AmbientWaveMode.Random;
            }

            return AmbientWaveMode.Vanilla;
        }

        internal static AmbientWaveMode GetGruntMode(SpawnScalingSceneConfig config)
        {
            return ParseMode(config.GruntWaveMode);
        }

        internal static AmbientWaveMode GetMimicMode(SpawnScalingSceneConfig config)
        {
            return ParseMode(config.MimicWaveMode);
        }

        internal static bool IsGruntWaitActive(SpawnScalingSceneConfig config)
        {
            return config.EnableSpawnScaling && GetGruntMode(config) != AmbientWaveMode.Vanilla;
        }

        internal static bool IsMimicWaitActive(SpawnScalingSceneConfig config)
        {
            return config.EnableSpawnScaling && GetMimicMode(config) != AmbientWaveMode.Vanilla;
        }

        internal static float ResolveGruntInitialWaitSeconds(SpawnScalingSceneConfig config)
        {
            return ResolveInitialWaitSeconds(
                GetGruntMode(config),
                config.GruntWaveInitialDelaySeconds,
                config.GruntWaveInitialDelayMinSeconds,
                config.GruntWaveInitialDelayMaxSeconds);
        }

        internal static float ResolveMimicInitialWaitSeconds(SpawnScalingSceneConfig config)
        {
            return ResolveInitialWaitSeconds(
                GetMimicMode(config),
                config.MimicWaveInitialDelaySeconds,
                config.MimicWaveInitialDelayMinSeconds,
                config.MimicWaveInitialDelayMaxSeconds);
        }

        internal static int ResolveGruntWaveIntervalMs(SpawnScalingSceneConfig config)
        {
            return ResolveWaveIntervalMs(
                GetGruntMode(config),
                config.GruntWaveIntervalSeconds,
                config.GruntWaveIntervalMinSeconds,
                config.GruntWaveIntervalMaxSeconds);
        }

        internal static int ResolveMimicWaveIntervalMs(SpawnScalingSceneConfig config)
        {
            return ResolveWaveIntervalMs(
                GetMimicMode(config),
                config.MimicWaveIntervalSeconds,
                config.MimicWaveIntervalMinSeconds,
                config.MimicWaveIntervalMaxSeconds);
        }

        internal static int RollGruntWaveIntervalMs(SpawnScalingSceneConfig config)
        {
            return SecondsToMs(RollSeconds(config.GruntWaveIntervalMinSeconds, config.GruntWaveIntervalMaxSeconds));
        }

        internal static int RollMimicWaveIntervalMs(SpawnScalingSceneConfig config)
        {
            return SecondsToMs(RollSeconds(config.MimicWaveIntervalMinSeconds, config.MimicWaveIntervalMaxSeconds));
        }

        internal static float ResolveInitialWaitSeconds(
            AmbientWaveMode mode,
            float fixedSeconds,
            float minSeconds,
            float maxSeconds)
        {
            return mode switch
            {
                AmbientWaveMode.Fixed => fixedSeconds,
                AmbientWaveMode.Random => RollSeconds(minSeconds, maxSeconds),
                _ => 0f,
            };
        }

        internal static int ResolveWaveIntervalMs(
            AmbientWaveMode mode,
            float fixedSeconds,
            float minSeconds,
            float maxSeconds)
        {
            return mode switch
            {
                AmbientWaveMode.Fixed => SecondsToMs(fixedSeconds),
                AmbientWaveMode.Random => SecondsToMs(RollSeconds(minSeconds, maxSeconds)),
                _ => 0,
            };
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
