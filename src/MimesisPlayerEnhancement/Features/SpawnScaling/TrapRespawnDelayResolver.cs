namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class TrapRespawnDelayResolver
    {
        internal static TrapRespawnMode ParseMode(string? value)
        {
            if (string.Equals(value, "Fixed", StringComparison.OrdinalIgnoreCase))
            {
                return TrapRespawnMode.Fixed;
            }

            if (string.Equals(value, "Random", StringComparison.OrdinalIgnoreCase))
            {
                return TrapRespawnMode.Random;
            }

            return TrapRespawnMode.Vanilla;
        }

        internal static TrapRespawnMode GetMode(SpawnScalingSceneConfig config)
        {
            return ParseMode(config.TrapRespawnMode);
        }

        internal static bool IsForceRespawnActive(SpawnScalingSceneConfig config)
        {
            return config.EnableSpawnScaling && GetMode(config) != TrapRespawnMode.Vanilla;
        }

        internal static float ResolveDelaySeconds(SpawnScalingSceneConfig config)
        {
            return GetMode(config) switch
            {
                TrapRespawnMode.Fixed => config.TrapRespawnDelaySeconds,
                TrapRespawnMode.Random => RollDelaySeconds(config),
                _ => 0f,
            };
        }

        internal static float ResolveMinPlayerDistanceMeters(SpawnScalingSceneConfig config)
        {
            return IsForceRespawnActive(config) ? config.TrapRespawnMinPlayerDistanceMeters : 0f;
        }

        private static float RollDelaySeconds(SpawnScalingSceneConfig config)
        {
            float min = config.TrapRespawnDelayMinSeconds;
            float max = config.TrapRespawnDelayMaxSeconds;
            return min >= max ? min : UnityEngine.Random.Range(min, max);
        }
    }
}
