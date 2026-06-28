using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier;

internal static class ShopBuyPriceApplier
{
    private const string Feature = "MoneyMultiplier";

    private static readonly FieldInfo PriceForItemsField =
        AccessTools.Field(typeof(MaintenanceRoom), "_priceForItems")
        ?? throw new InvalidOperationException("MaintenanceRoom._priceForItems not found");

    private static readonly ConditionalWeakTable<MaintenanceRoom, Dictionary<int, int>> VanillaPricesByRoom = new();

    internal static bool ShouldApplyShopPrices() =>
        ModConfig.EnableMoneyMultiplier.Value && HostApplyGate.ShouldApplyHostOnlyFeature();

    internal static void ClearVanillaPrices(MaintenanceRoom room)
    {
        if (room == null)
            return;

        VanillaPricesByRoom.Remove(room);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Dictionary<int, int> GetVanillaPrices(MaintenanceRoom room) =>
        VanillaPricesByRoom.GetOrCreateValue(room);

    internal static int ScalePrice(MaintenanceRoom room, int vanilla)
    {
        if (!ShouldApplyShopPrices() || vanilla <= 0)
            return vanilla;

        int playerCount = MoneyPlayerCountHelper.ResolveFromRoom(room);
        float effective = MoneyMultiplierResolver.GetEffectiveMultiplier(MoneyType.ShopBuyPrice, playerCount);
        return MoneyMultiplierResolver.ScaleAmount(vanilla, effective);
    }

    internal static void ApplyInPlace(MaintenanceRoom room)
    {
        if (!ShouldApplyShopPrices())
            return;

        if (PriceForItemsField.GetValue(room) is not Dictionary<int, ShopItemPriceInfo> priceForItems
            || priceForItems.Count == 0)
            return;

        int playerCount = MoneyPlayerCountHelper.ResolveFromRoom(room);
        float effective = MoneyMultiplierResolver.GetEffectiveMultiplier(MoneyType.ShopBuyPrice, playerCount);

        Dictionary<int, int> vanillaPrices = GetVanillaPrices(room);

        int scaledCount = 0;
        foreach (KeyValuePair<int, ShopItemPriceInfo> entry in priceForItems)
        {
            ShopItemPriceInfo? info = entry.Value;
            if (info == null || info.Price <= 0)
                continue;

            if (!vanillaPrices.TryGetValue(entry.Key, out int vanilla))
            {
                vanilla = info.Price;
                vanillaPrices[entry.Key] = vanilla;
            }

            int scaled = effective == 1f ? vanilla : MoneyMultiplierResolver.ScaleAmount(vanilla, effective);
            if (info.Price == scaled)
                continue;

            info.Price = scaled;
            scaledCount++;
        }

        if (scaledCount == 0 && effective == 1f)
            return;

        MaintenanceShopPriceSync.SyncVendingMachineLevelObjects(room, priceForItems);

        if (scaledCount > 0 || ModConfig.EnableDebugLogging.Value)
        {
            ModLog.Info(
                Feature,
                $"Shop buy prices scaled — {scaledCount}/{priceForItems.Count} items " +
                $"(players={playerCount}, effective={effective:0.##}×)");
        }
    }
}
