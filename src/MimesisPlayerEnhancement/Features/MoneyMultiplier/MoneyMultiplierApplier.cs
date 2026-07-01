using System;
using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.MoneyMultiplier
{
    internal static class MoneyMultiplierApplier
    {
        private static readonly FieldInfo TargetCurrencyField =
            AccessTools.Field(typeof(GameSessionInfo), "_targetCurrency")
            ?? throw new InvalidOperationException("GameSessionInfo._targetCurrency not found");

        internal static bool IsEnabled()
        {
            return ModConfig.EnableMoneyMultiplier.Value && HostApplyGate.ShouldApplyHostOnlyFeature();
        }

        internal static bool TryGetVanillaInitialMoney(out int value)
        {
            value = 0;
            ExcelDataManager? excel = HubGameDataAccess.Excel;
            if (excel == null)
            {
                return false;
            }

            try
            {
                value = excel.Consts.C_InitialMoney;
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
            if (!IsEnabled() || StartupMoneyLoadGuard.IsActive)
            {
                return;
            }

            if (!TryGetVanillaInitialMoney(out int vanillaInitial) || currency != vanillaInitial)
            {
                return;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromRoom(room);
            currency = ScaleForType(MoneyType.Startup, currency, playerCount);
        }

        internal static void ApplyRoundGoal(GameSessionInfo info)
        {
            if (!IsEnabled())
            {
                return;
            }

            int vanilla = (int)(TargetCurrencyField.GetValue(info) ?? 0);
            if (vanilla <= 0)
            {
                return;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromSession(info);
            int scaled = ScaleForType(MoneyType.RoundGoal, vanilla, playerCount);
            TargetCurrencyField.SetValue(info, scaled);
        }

        internal static int ScaleScrapValue(int vanilla)
        {
            if (!IsEnabled() || vanilla == 0)
            {
                return vanilla;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromSession();
            float effective = MoneyMultiplierResolver.GetEffectiveMultiplier(MoneyType.ScrapSellValue, playerCount);
            if (effective == 1f)
            {
                return vanilla;
            }

            return ScaleForType(MoneyType.ScrapSellValue, vanilla, playerCount);
        }

        internal static int ScaleReinforcePrice(MaintenanceRoom room, int vanilla)
        {
            if (!IsEnabled())
            {
                return vanilla;
            }

            int playerCount = SessionPlayerCountHelper.ResolveFromRoom(room);
            return ScaleForType(MoneyType.ReinforcePrice, vanilla, playerCount);
        }

        internal static int ScaleReinforceCost(int upgradeCost, MaintenanceRoom room)
        {
            return ScaleReinforcePrice(room, upgradeCost);
        }
    }
}
