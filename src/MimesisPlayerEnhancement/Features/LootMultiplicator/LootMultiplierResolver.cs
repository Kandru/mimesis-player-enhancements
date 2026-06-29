using Bifrost.ConstEnum;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootMultiplierResolver
    {
        internal static bool IsAutoScaleEnabled(LootSource source, ItemType itemType)
        {
            return (source, ItemTypeLookup.NormalizeItemType(itemType)) switch
            {
                (LootSource.Map, var type) when type.Equals(ItemType.Consumable) =>
                    ModConfig.AutoScaleMapConsumableLootByPlayerCount.Value,
                (LootSource.Map, var type) when type.Equals(ItemType.Equipment) =>
                    ModConfig.AutoScaleMapEquipmentLootByPlayerCount.Value,
                (LootSource.Map, _) => ModConfig.AutoScaleMapMiscellanyLootByPlayerCount.Value,
                (LootSource.Drop, var type) when type.Equals(ItemType.Consumable) =>
                    ModConfig.AutoScaleDropConsumableLootByPlayerCount.Value,
                (LootSource.Drop, var type) when type.Equals(ItemType.Equipment) =>
                    ModConfig.AutoScaleDropEquipmentLootByPlayerCount.Value,
                (LootSource.Drop, _) => ModConfig.AutoScaleDropMiscellanyLootByPlayerCount.Value,
                (LootSource.Trigger, var type) when type.Equals(ItemType.Consumable) =>
                    ModConfig.AutoScaleTriggerConsumableLootByPlayerCount.Value,
                (LootSource.Trigger, var type) when type.Equals(ItemType.Equipment) =>
                    ModConfig.AutoScaleTriggerEquipmentLootByPlayerCount.Value,
                _ => ModConfig.AutoScaleTriggerMiscellanyLootByPlayerCount.Value,
            };
        }

        internal static float GetPlayerScale(LootSource source, ItemType itemType, int playerCount)
        {
            return ScalingMath.GetPlayerScale(playerCount, IsAutoScaleEnabled(source, itemType));
        }

        internal static float GetBaseMultiplier(LootSource source, ItemType itemType)
        {
            return (source, ItemTypeLookup.NormalizeItemType(itemType)) switch
            {
                (LootSource.Map, var type) when type.Equals(ItemType.Consumable) =>
                    ModConfig.MapConsumableLootMultiplier.Value,
                (LootSource.Map, var type) when type.Equals(ItemType.Equipment) =>
                    ModConfig.MapEquipmentLootMultiplier.Value,
                (LootSource.Map, _) => ModConfig.MapMiscellanyLootMultiplier.Value,
                (LootSource.Drop, var type) when type.Equals(ItemType.Consumable) =>
                    ModConfig.DropConsumableLootMultiplier.Value,
                (LootSource.Drop, var type) when type.Equals(ItemType.Equipment) =>
                    ModConfig.DropEquipmentLootMultiplier.Value,
                (LootSource.Drop, _) => ModConfig.DropMiscellanyLootMultiplier.Value,
                (LootSource.Trigger, var type) when type.Equals(ItemType.Consumable) =>
                    ModConfig.TriggerConsumableLootMultiplier.Value,
                (LootSource.Trigger, var type) when type.Equals(ItemType.Equipment) =>
                    ModConfig.TriggerEquipmentLootMultiplier.Value,
                _ => ModConfig.TriggerMiscellanyLootMultiplier.Value,
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
