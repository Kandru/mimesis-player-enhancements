namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class SpawnMultiplierResolver
    {
        internal static bool IsAutoScaleEnabled(SpawnCategory category)
        {
            return category switch
            {
                SpawnCategory.Mimic => ModConfig.AutoScaleMimicSpawnsByPlayerCount.Value,
                SpawnCategory.Boss => ModConfig.AutoScaleBossSpawnsByPlayerCount.Value,
                SpawnCategory.Jako => ModConfig.AutoScaleJakoSpawnsByPlayerCount.Value,
                SpawnCategory.Special => ModConfig.AutoScaleSpecialSpawnsByPlayerCount.Value,
                SpawnCategory.Trap => ModConfig.AutoScaleTrapSpawnsByPlayerCount.Value,
                _ => ModConfig.AutoScaleOtherSpawnsByPlayerCount.Value,
            };
        }

        internal static float GetPlayerScale(SpawnCategory category, int playerCount)
        {
            return ScalingMath.GetPlayerScale(playerCount, IsAutoScaleEnabled(category));
        }

        internal static float GetPerCategoryMultiplier(SpawnCategory category)
        {
            return category switch
            {
                SpawnCategory.Mimic => ModConfig.MimicSpawnMultiplier.Value,
                SpawnCategory.Boss => ModConfig.BossSpawnMultiplier.Value,
                SpawnCategory.Jako => ModConfig.JakoSpawnMultiplier.Value,
                SpawnCategory.Special => ModConfig.SpecialSpawnMultiplier.Value,
                SpawnCategory.Trap => ModConfig.TrapSpawnMultiplier.Value,
                _ => ModConfig.OtherSpawnMultiplier.Value,
            };
        }

        internal static float GetEffectiveMultiplier(SpawnCategory category, int playerCount)
        {
            if (!ModConfig.EnableSpawnScaling.Value)
            {
                return FeatureToggleGate.NeutralMultiplier;
            }

            return GetPerCategoryMultiplier(category) * GetPlayerScale(category, playerCount);
        }

        internal static float GetEffectiveMultiplier(int masterId, int playerCount)
        {
            SpawnCategory category = SpawnCategoryLookup.GetCategory(masterId);
            return GetEffectiveMultiplier(category, playerCount);
        }

        internal static int ScaleCount(int vanilla, float multiplier)
        {
            return ScalingMath.ScaleCount(vanilla, multiplier);
        }

        internal static int ScaleCountWithImplicitBase(int vanilla, float multiplier, int implicitWhenZero)
        {
            return ScalingMath.ScaleCountWithImplicitBase(vanilla, multiplier, implicitWhenZero);
        }
    }
}
