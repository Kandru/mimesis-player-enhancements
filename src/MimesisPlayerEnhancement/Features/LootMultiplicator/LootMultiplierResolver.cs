using Bifrost.ConstEnum;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootMultiplierResolver
    {
        internal static bool IsAutoScaleEnabled(LootSource source, ItemType itemType)
        {
            _ = itemType;
            return source switch
            {
                LootSource.Map => ModConfig.AutoScaleMapLootByPlayerCount.Value,
                LootSource.Drop => ModConfig.AutoScaleDropLootByPlayerCount.Value,
                _ => false,
            };
        }

        internal static float GetPlayerScale(LootSource source, ItemType itemType, int playerCount)
        {
            return ScalingMath.GetPlayerScale(
                playerCount,
                IsAutoScaleEnabled(source, itemType),
                ModConfig.LootMultiplicatorPlayerCountScaleRate.Value);
        }

        internal static float GetBaseMultiplier(LootSource source, ItemType itemType)
        {
            _ = itemType;
            return source switch
            {
                LootSource.Map => ModConfig.MapLootMultiplier.Value,
                LootSource.Drop => ModConfig.DropLootMultiplier.Value,
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
            if (!ModConfig.EnableLootMultiplicator.Value)
            {
                return FeatureToggleGate.NeutralMultiplier;
            }

            if (source.Equals(LootSource.Trigger))
            {
                return FeatureToggleGate.NeutralMultiplier;
            }

            if (masterId > 0 && !LootItemFilter.IsEligible(masterId))
            {
                return 0f;
            }

            return GetBaseMultiplier(source, itemType) * GetPlayerScale(source, itemType, playerCount);
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
