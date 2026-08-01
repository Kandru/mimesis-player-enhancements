namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class SpawnMultiplierResolver
    {
        internal static float GetPerCategoryMultiplier(SpawnCategory category, SpawnScalingSceneConfig config)
        {
            return category switch
            {
                SpawnCategory.Mimic => config.MimicSpawnMultiplier,
                SpawnCategory.Boss => config.BossSpawnMultiplier,
                SpawnCategory.Grunt => config.GruntSpawnMultiplier,
                SpawnCategory.Special => config.SpecialSpawnMultiplier,
                SpawnCategory.Trap => config.TrapSpawnMultiplier,
                _ => config.OtherSpawnMultiplier,
            };
        }

        internal static float GetPerPlayerMultiplier(SpawnCategory category, SpawnScalingSceneConfig config)
        {
            return category switch
            {
                SpawnCategory.Mimic => config.MimicSpawnPerPlayerMultiplier,
                SpawnCategory.Boss => config.BossSpawnPerPlayerMultiplier,
                SpawnCategory.Grunt => config.GruntSpawnPerPlayerMultiplier,
                SpawnCategory.Special => config.SpecialSpawnPerPlayerMultiplier,
                SpawnCategory.Trap => config.TrapSpawnPerPlayerMultiplier,
                _ => config.OtherSpawnPerPlayerMultiplier,
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

            return ScalingMath.GetAdditiveMultiplier(
                GetPerCategoryMultiplier(category, config),
                GetPerPlayerMultiplier(category, config),
                playerCount,
                config.SpawnScalingBaselinePlayerCount);
        }

        internal static float GetEffectiveMultiplier(int masterId, int playerCount, SpawnScalingSceneConfig config)
        {
            SpawnCategory category = SpawnCategoryLookup.GetCategory(masterId);
            return GetEffectiveMultiplier(category, playerCount, config);
        }
    }
}
