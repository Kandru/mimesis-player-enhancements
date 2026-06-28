using Bifrost.ConstEnum;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator;

internal static class LootMultiplierResolver
{
    private const int VanillaPlayerBaseline = 4;

    internal static bool IsAutoScaleEnabled(LootSource source, ItemType itemType) =>
        (source, ItemTypeLookup.NormalizeItemType(itemType)) switch
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

    internal static float GetPlayerScale(LootSource source, ItemType itemType, int playerCount)
    {
        if (!IsAutoScaleEnabled(source, itemType) || playerCount <= VanillaPlayerBaseline)
            return 1f;

        return playerCount / (float)VanillaPlayerBaseline;
    }

    internal static float GetBaseMultiplier(LootSource source, ItemType itemType) =>
        (source, ItemTypeLookup.NormalizeItemType(itemType)) switch
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

    internal static float GetEffectiveMultiplier(LootSource source, ItemType itemType, int playerCount) =>
        GetBaseMultiplier(source, itemType) * GetPlayerScale(source, itemType, playerCount);

    internal static float GetEffectiveMultiplier(LootSource source, int masterId, int playerCount) =>
        GetEffectiveMultiplier(source, ItemTypeLookup.GetItemType(masterId), playerCount);

    internal static int ScaleCount(int vanilla, float multiplier)
    {
        if (vanilla == 0)
            return 0;

        if (multiplier <= 0f)
            return 0;

        return System.Math.Max(1, (int)System.Math.Round(vanilla * multiplier));
    }

    internal static int ScaleCountWithImplicitBase(int vanilla, float multiplier, int implicitWhenZero)
    {
        int baseCount = vanilla > 0 ? vanilla : implicitWhenZero;
        return ScaleCount(baseCount, multiplier);
    }
}
