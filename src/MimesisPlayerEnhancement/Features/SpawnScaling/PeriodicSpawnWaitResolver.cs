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
            return ParseMode(ModConfig.PeriodicSpawnWaitMode.Value);
        }

        internal static bool IsWaitModeActive()
        {
            return ModConfig.EnableSpawnScaling.Value && GetMode() != PeriodicSpawnWaitMode.Vanilla;
        }

        internal static bool ShouldApplyHostWaitOverrides()
        {
            return IsWaitModeActive() && HostApplyGate.ShouldApplyHostOnlyFeature();
        }

        internal static float RollInitialWaitSeconds()
        {
            float min = ModConfig.InitialPeriodicSpawnWaitMinSeconds.Value;
            float max = ModConfig.InitialPeriodicSpawnWaitMaxSeconds.Value;
            return RollSeconds(min, max);
        }

        internal static float ResolveInitialWaitSeconds()
        {
            return GetMode() switch
            {
                PeriodicSpawnWaitMode.Fixed => ModConfig.InitialPeriodicSpawnWaitSeconds.Value,
                PeriodicSpawnWaitMode.Random => RollInitialWaitSeconds(),
                _ => 0f,
            };
        }

        internal static int RollWaveIntervalMs()
        {
            return SecondsToMs(RollWaveIntervalSeconds());
        }

        internal static int ResolveWaveIntervalMs()
        {
            return GetMode() switch
            {
                PeriodicSpawnWaitMode.Fixed => SecondsToMs(ModConfig.PeriodicSpawnIntervalSeconds.Value),
                PeriodicSpawnWaitMode.Random => RollWaveIntervalMs(),
                _ => 0,
            };
        }

        private static float RollWaveIntervalSeconds()
        {
            float min = ModConfig.PeriodicSpawnIntervalMinSeconds.Value;
            float max = ModConfig.PeriodicSpawnIntervalMaxSeconds.Value;
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
