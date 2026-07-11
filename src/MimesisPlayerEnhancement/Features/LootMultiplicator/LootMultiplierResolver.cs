namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootMultiplierResolver
    {
        internal static bool IsAutoScaleEnabled(LootSource source, ItemType itemType)
        {
            return IsAutoScaleEnabled(source, itemType, SceneScopedConfigGate.Loot);
        }

        internal static bool IsAutoScaleEnabled(
            LootSource source,
            ItemType itemType,
            LootMultiplicatorSceneConfig config)
        {
            _ = itemType;
            return source switch
            {
                LootSource.Map => config.AutoScaleMapLootByPlayerCount,
                LootSource.Drop => config.AutoScaleDropLootByPlayerCount,
                _ => false,
            };
        }

        internal static float GetPlayerScale(LootSource source, ItemType itemType, int playerCount)
        {
            return GetPlayerScale(source, itemType, playerCount, SceneScopedConfigGate.Loot);
        }

        internal static float GetPlayerScale(
            LootSource source,
            ItemType itemType,
            int playerCount,
            LootMultiplicatorSceneConfig config)
        {
            return ScalingMath.GetPlayerScale(
                playerCount,
                IsAutoScaleEnabled(source, itemType, config),
                config.LootMultiplicatorPlayerCountScaleRate);
        }

        internal static float GetBaseMultiplier(LootSource source, ItemType itemType)
        {
            return GetBaseMultiplier(source, itemType, SceneScopedConfigGate.Loot);
        }

        internal static float GetBaseMultiplier(LootSource source, ItemType itemType, LootMultiplicatorSceneConfig config)
        {
            _ = itemType;
            return source switch
            {
                LootSource.Map => config.MapLootMultiplier,
                LootSource.Drop => config.DropLootMultiplier,
                _ => FeatureToggleGate.NeutralMultiplier,
            };
        }

        internal static float GetEffectiveMultiplier(LootSource source, ItemType itemType, int playerCount)
        {
            return GetEffectiveMultiplier(source, itemType, playerCount, masterId: 0);
        }

        internal static float GetEffectiveMultiplier(
            LootSource source,
            ItemType itemType,
            int playerCount,
            int masterId)
        {
            return GetEffectiveMultiplier(source, itemType, playerCount, masterId, SceneScopedConfigGate.Loot);
        }

        internal static float GetEffectiveMultiplier(
            LootSource source,
            ItemType itemType,
            int playerCount,
            int masterId,
            LootMultiplicatorSceneConfig config)
        {
            _ = masterId;
            if (!config.EnableLootMultiplicator)
            {
                return FeatureToggleGate.NeutralMultiplier;
            }

            if (source.Equals(LootSource.Trigger))
            {
                return FeatureToggleGate.NeutralMultiplier;
            }

            return GetBaseMultiplier(source, itemType, config) * GetPlayerScale(source, itemType, playerCount, config);
        }

        internal static float GetEffectiveMultiplier(LootSource source, int masterId, int playerCount)
        {
            return GetEffectiveMultiplier(source, ItemTypeLookup.GetItemType(masterId), playerCount, masterId);
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
