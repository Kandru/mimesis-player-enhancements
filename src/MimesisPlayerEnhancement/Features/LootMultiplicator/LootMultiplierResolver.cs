namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootMultiplierResolver
    {
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

        internal static float GetPerPlayerMultiplier(LootSource source, ItemType itemType, LootMultiplicatorSceneConfig config)
        {
            _ = itemType;
            return source switch
            {
                LootSource.Map => config.MapLootPerPlayerMultiplier,
                LootSource.Drop => config.DropLootPerPlayerMultiplier,
                _ => 0f,
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

            return ScalingMath.GetAdditiveMultiplier(
                GetBaseMultiplier(source, itemType, config),
                GetPerPlayerMultiplier(source, itemType, config),
                playerCount,
                config.LootMultiplicatorBaselinePlayerCount);
        }

        internal static float GetEffectiveMultiplier(LootSource source, int masterId, int playerCount)
        {
            return GetEffectiveMultiplier(source, ItemTypeLookup.GetItemType(masterId), playerCount, masterId);
        }
    }
}
