using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier;

internal static class ShopDiscountApplier
{
    private const string Feature = "MoneyMultiplier";

    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly FieldInfo PriceForItemsField =
        AccessTools.Field(typeof(MaintenanceRoom), "_priceForItems")
        ?? throw new InvalidOperationException("MaintenanceRoom._priceForItems not found");

    private static readonly FieldInfo LevelObjectsField =
        AccessTools.Field(typeof(IVroom), "_levelObjects")
        ?? throw new InvalidOperationException("IVroom._levelObjects not found");

    internal static void Apply(MaintenanceRoom room)
    {
        if (!MoneyMultiplierApplier.IsEnabled())
            return;

        if (ModConfig.ShopDiscountChancePercent.Value <= 0f)
            return;

        if (PriceForItemsField.GetValue(room) is not Dictionary<int, ShopItemPriceInfo> priceForItems
            || priceForItems.Count == 0)
            return;

        float minPercent = ModConfig.ShopDiscountMinPercent.Value;
        float maxPercent = ModConfig.ShopDiscountMaxPercent.Value;
        float chancePercent = ModConfig.ShopDiscountChancePercent.Value;
        if (maxPercent < minPercent)
            maxPercent = minPercent;

        int discounted = 0;
        foreach (ShopItemPriceInfo info in priceForItems.Values)
        {
            if (info == null)
                continue;

            int basePrice = GetBasePrice(info.Price, info.DiscountRate);
            if (basePrice <= 0)
                continue;

            if (!RollDiscount(chancePercent))
            {
                info.DiscountRate = 0f;
                info.Price = basePrice;
                continue;
            }

            float discountPercent = RollDiscountPercent(minPercent, maxPercent);
            info.DiscountRate = discountPercent / 100f;
            info.Price = Math.Max(1, (int)Math.Round(basePrice * (1f - info.DiscountRate)));
            discounted++;
        }

        SyncVendingMachineLevelObjects(room, priceForItems);

        if (discounted > 0 || ModConfig.EnableDebugLogging.Value)
        {
            ModLog.Debug(
                Feature,
                $"Shop discounts applied — {discounted}/{priceForItems.Count} items discounted " +
                $"(chance={chancePercent:0.##}%, range={minPercent:0.##}-{maxPercent:0.##}%)");
        }
    }

    private static int GetBasePrice(int price, float discountRate)
    {
        if (price <= 0)
            return 0;

        if (discountRate <= 0f || discountRate >= 1f)
            return price;

        return Math.Max(1, (int)Math.Round(price / (1f - discountRate)));
    }

    private static bool RollDiscount(float chancePercent)
    {
        if (chancePercent >= 100f)
            return true;

        if (chancePercent <= 0f)
            return false;

        return SimpleRandUtil.Next(0, 10000) < (int)Math.Round(chancePercent * 100f);
    }

    private static float RollDiscountPercent(float minPercent, float maxPercent)
    {
        if (maxPercent <= minPercent)
            return minPercent;

        int min = (int)Math.Round(minPercent);
        int max = (int)Math.Round(maxPercent);
        return SimpleRandUtil.Next(min, max + 1);
    }

    private static void SyncVendingMachineLevelObjects(
        MaintenanceRoom room,
        Dictionary<int, ShopItemPriceInfo> priceForItems)
    {
        if (LevelObjectsField.GetValue(room) is not IDictionary levelObjects)
            return;

        foreach (DictionaryEntry entry in levelObjects)
        {
            if (entry.Value is not InsertLevelObjectInfo insertLevelObjectInfo)
                continue;

            if (insertLevelObjectInfo.InsertLevelObjectType != InsertLevelObjectType.VendingMachine)
                continue;

            if (!priceForItems.TryGetValue(insertLevelObjectInfo.OutputItemMasterID, out ShopItemPriceInfo? shopInfo)
                || shopInfo == null)
                continue;

            insertLevelObjectInfo.InputAmount = shopInfo.Price;
            insertLevelObjectInfo.DiscountRate = shopInfo.DiscountRate;
        }
    }
}
