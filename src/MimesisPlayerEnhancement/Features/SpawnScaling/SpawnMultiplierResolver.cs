namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class SpawnMultiplierResolver
    {
        internal static bool IsAutoScaleEnabled(SpawnCategory category)
        {
            return IsAutoScaleEnabled(category, SceneScopedConfigGate.Spawn);
        }

        internal static bool IsAutoScaleEnabled(SpawnCategory category, SpawnScalingSceneConfig config)
        {
            return category switch
            {
                SpawnCategory.Mimic => config.AutoScaleMimicSpawnsByPlayerCount,
                SpawnCategory.Boss => config.AutoScaleBossSpawnsByPlayerCount,
                SpawnCategory.Jako => config.AutoScaleJakoSpawnsByPlayerCount,
                SpawnCategory.Special => config.AutoScaleSpecialSpawnsByPlayerCount,
                SpawnCategory.Trap => config.AutoScaleTrapSpawnsByPlayerCount,
                _ => config.AutoScaleOtherSpawnsByPlayerCount,
            };
        }

        internal static float GetPlayerScale(SpawnCategory category, int playerCount)
        {
            return GetPlayerScale(category, playerCount, SceneScopedConfigGate.Spawn);
        }

        internal static float GetPlayerScale(SpawnCategory category, int playerCount, SpawnScalingSceneConfig config)
        {
            return ScalingMath.GetPlayerScale(
                playerCount,
                IsAutoScaleEnabled(category, config),
                config.SpawnScalingPlayerCountScaleRate);
        }

        internal static float GetPerCategoryMultiplier(SpawnCategory category)
        {
            return GetPerCategoryMultiplier(category, SceneScopedConfigGate.Spawn);
        }

        internal static float GetPerCategoryMultiplier(SpawnCategory category, SpawnScalingSceneConfig config)
        {
            return category switch
            {
                SpawnCategory.Mimic => config.MimicSpawnMultiplier,
                SpawnCategory.Boss => config.BossSpawnMultiplier,
                SpawnCategory.Jako => config.JakoSpawnMultiplier,
                SpawnCategory.Special => config.SpecialSpawnMultiplier,
                SpawnCategory.Trap => config.TrapSpawnMultiplier,
                _ => config.OtherSpawnMultiplier,
            };
        }

        internal static float GetEffectiveMultiplier(SpawnCategory category, int playerCount)
        {
            return GetEffectiveMultiplier(category, playerCount, SceneScopedConfigGate.Spawn);
        }

        internal static float GetEffectiveMultiplier(SpawnCategory category, int playerCount, SpawnScalingSceneConfig config)
        {
            if (!config.EnableSpawnScaling)
            {
                return FeatureToggleGate.NeutralMultiplier;
            }

            return GetPerCategoryMultiplier(category, config) * GetPlayerScale(category, playerCount, config);
        }

        internal static float GetEffectiveMultiplier(int masterId, int playerCount)
        {
            SpawnCategory category = SpawnCategoryLookup.GetCategory(masterId);
            return GetEffectiveMultiplier(category, playerCount);
        }

        internal static float GetEffectiveMultiplier(int masterId, int playerCount, SpawnScalingSceneConfig config)
        {
            SpawnCategory category = SpawnCategoryLookup.GetCategory(masterId);
            return GetEffectiveMultiplier(category, playerCount, config);
        }
    }
}
