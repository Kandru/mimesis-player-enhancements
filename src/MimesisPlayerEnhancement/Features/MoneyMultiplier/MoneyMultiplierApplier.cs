using System;
using System.Collections.Generic;
using System.Reflection;
using Bifrost.Cooked;
using HarmonyLib;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier;

internal static class MoneyMultiplierApplier
{
    private const string Feature = "MoneyMultiplier";
    private const int MaxShopItemRollAttemptsPerSlot = 32;

    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly PropertyInfo? HubDatamanProperty =
        typeof(Hub).GetProperty("dataman", InstanceFlags);

    private static readonly PropertyInfo? HubDynamicDataManProperty =
        typeof(Hub).GetProperty("dynamicDataMan", InstanceFlags);

    private static readonly MethodInfo? GetVendingMachineLevelObjectsMethod =
        typeof(Hub).Assembly.GetType("DynamicDataManager")?.GetMethod("GetVendingMachineLevelObjects", InstanceFlags);

    private static readonly MethodInfo? GetShopGroupInfoMethod =
        typeof(Hub).Assembly.GetType("ExcelDataManager")?.GetMethod("GetShopGroupInfo", InstanceFlags);

    private static readonly FieldInfo TargetCurrencyField =
        AccessTools.Field(typeof(GameSessionInfo), "_targetCurrency")
        ?? throw new System.InvalidOperationException("GameSessionInfo._targetCurrency not found");

    private static readonly FieldInfo PriceForItemsField =
        AccessTools.Field(typeof(MaintenanceRoom), "_priceForItems")
        ?? throw new System.InvalidOperationException("MaintenanceRoom._priceForItems not found");

    internal static bool IsEnabled() =>
        ModConfig.EnableMoneyMultiplier.Value && MoneyMultiplierHost.ShouldApply();

    internal static bool TryGetVanillaInitialMoney(out int value)
    {
        value = 0;
        if (Hub.s == null)
            return false;

        if (HubDatamanProperty?.GetValue(Hub.s) is not DataManager dataman)
            return false;

        try
        {
            value = dataman.ExcelDataManager.Consts.C_InitialMoney;
            return true;
        }
        catch
        {
            return false;
        }
    }

    internal static int ScaleForType(MoneyType type, int vanilla, int playerCount)
    {
        float effective = MoneyMultiplierResolver.GetEffectiveMultiplier(type, playerCount);
        int scaled = MoneyMultiplierResolver.ScaleAmount(vanilla, effective);
        MoneyMultiplierLog.DebugScaled(type, vanilla, scaled, playerCount, effective);
        return scaled;
    }

    internal static void ApplyStartupMoney(MaintenanceRoom room, ref int currency)
    {
        if (!IsEnabled())
            return;

        if (!TryGetVanillaInitialMoney(out int vanillaInitial) || currency != vanillaInitial)
            return;

        int playerCount = MoneyPlayerCountHelper.ResolveFromRoom(room);
        currency = ScaleForType(MoneyType.Startup, currency, playerCount);
    }

    internal static void ApplyRoundGoal(GameSessionInfo info)
    {
        if (!IsEnabled())
            return;

        int vanilla = (int)(TargetCurrencyField.GetValue(info) ?? 0);
        if (vanilla <= 0)
            return;

        int playerCount = MoneyPlayerCountHelper.ResolveFromSession(info);
        int scaled = ScaleForType(MoneyType.RoundGoal, vanilla, playerCount);
        TargetCurrencyField.SetValue(info, scaled);
    }

    internal static void ApplyRoundGoalFromSave(GameSessionInfo info, MMSaveGameData saveGameData)
    {
        if (!IsEnabled())
            return;

        if (saveGameData.TargetCurrency <= 0)
            return;

        int playerCount = MoneyPlayerCountHelper.ResolveFromSession(info);
        int scaled = ScaleForType(MoneyType.RoundGoal, saveGameData.TargetCurrency, playerCount);
        TargetCurrencyField.SetValue(info, scaled);
    }

    internal static int ScaleScrapValue(int vanilla) =>
        IsEnabled()
            ? ScaleForType(MoneyType.ScrapSellValue, vanilla, MoneyPlayerCountHelper.ResolveForItemPrices())
            : vanilla;

    internal static int ScaleShopPrice(MaintenanceRoom room, int vanilla)
    {
        if (!IsEnabled())
            return vanilla;

        int playerCount = MoneyPlayerCountHelper.ResolveFromRoom(room);
        return ScaleForType(MoneyType.ShopBuyPrice, vanilla, playerCount);
    }

    internal static int ScaleReinforcePrice(MaintenanceRoom room, int vanilla)
    {
        if (!IsEnabled())
            return vanilla;

        int playerCount = MoneyPlayerCountHelper.ResolveFromRoom(room);
        return ScaleForType(MoneyType.ReinforcePrice, vanilla, playerCount);
    }

    internal static void ApplyShopItems(MaintenanceRoom room)
    {
        if (!IsEnabled())
            return;

        if (PriceForItemsField.GetValue(room) is not Dictionary<int, ShopItemPriceInfo> priceForItems)
            return;

        int vanillaCount = priceForItems.Count;
        if (vanillaCount == 0)
            return;

        int playerCount = MoneyPlayerCountHelper.ResolveFromRoom(room);
        int targetCount = ScaleForType(MoneyType.ShopItems, vanillaCount, playerCount);
        if (targetCount <= vanillaCount)
            return;

        if (Hub.s == null)
            return;

        if (HubDatamanProperty?.GetValue(Hub.s) is not DataManager dataman)
            return;

        if (HubDynamicDataManProperty?.GetValue(Hub.s) == null
            || GetVendingMachineLevelObjectsMethod == null
            || GetShopGroupInfoMethod == null)
            return;

        object dynamicDataMan = HubDynamicDataManProperty.GetValue(Hub.s)!;
        if (GetVendingMachineLevelObjectsMethod.Invoke(dynamicDataMan, null) is not System.Collections.IList vendingMachines)
            return;

        var shopGroupIds = new List<int>();
        foreach (object? vendingMachine in vendingMachines)
        {
            if (vendingMachine is not VendingMachineLevelObject machine || machine.shopGroupID <= 0)
                continue;

            shopGroupIds.Add(machine.shopGroupID);
        }

        if (shopGroupIds.Count == 0)
        {
            ModLog.Debug(Feature, "Shop items scaling skipped — no vending-machine shop groups on map");
            return;
        }

        object excel = dataman.ExcelDataManager;
        int added = 0;
        int attempts = 0;
        int maxAttempts = (targetCount - vanillaCount) * MaxShopItemRollAttemptsPerSlot;

        while (priceForItems.Count < targetCount && attempts < maxAttempts)
        {
            attempts++;
            int shopGroupId = shopGroupIds[attempts % shopGroupIds.Count];
            object? shopResult = GetShopGroupInfoMethod.Invoke(excel, new object[] { shopGroupId });
            if (!TryParseShopGroupInfo(shopResult, out int masterId, out int price, out float discountRate))
                continue;

            if (masterId <= 0 || priceForItems.ContainsKey(masterId))
                continue;

            priceForItems.Add(masterId, new ShopItemPriceInfo
            {
                Price = price,
                DiscountRate = discountRate,
            });
            added++;
        }

        if (added > 0)
        {
            float effective = MoneyMultiplierResolver.GetEffectiveMultiplier(MoneyType.ShopItems, playerCount);
            ModLog.Info(
                Feature,
                $"Shop items scaled — {vanillaCount} -> {priceForItems.Count} unique items " +
                $"(target={targetCount}, added={added}, players={playerCount}, effective={effective:0.##}×)");
        }
        else if (ModConfig.EnableDebugLogging.Value)
        {
            ModLog.Debug(
                Feature,
                $"Shop items unchanged at {vanillaCount} — could not roll {targetCount - vanillaCount} additional unique items");
        }
    }

    private static bool TryParseShopGroupInfo(object? result, out int masterId, out int price, out float discountRate)
    {
        masterId = 0;
        price = 0;
        discountRate = 0f;
        if (result == null)
            return false;

        Type tupleType = result.GetType();
        masterId = (int)(tupleType.GetField("Item1")?.GetValue(result) ?? 0);
        price = (int)(tupleType.GetField("Item2")?.GetValue(result) ?? 0);
        discountRate = (float)(tupleType.GetField("Item3")?.GetValue(result) ?? 0f);
        return masterId > 0;
    }
}
