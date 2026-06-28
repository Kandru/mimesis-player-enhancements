using System.Reflection;
using Bifrost.Cooked;
using HarmonyLib;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier;

internal static class MoneyMultiplierApplier
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly PropertyInfo? HubDatamanProperty =
        typeof(Hub).GetProperty("dataman", InstanceFlags);

    private static readonly FieldInfo TargetCurrencyField =
        AccessTools.Field(typeof(GameSessionInfo), "_targetCurrency")
        ?? throw new System.InvalidOperationException("GameSessionInfo._targetCurrency not found");

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
}
