using System;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Economy
{
    internal static class EconomyApplier
    {
        private static readonly FieldInfo TargetCurrencyField =
            AccessTools.Field(typeof(GameSessionInfo), "_targetCurrency")
            ?? throw new InvalidOperationException("GameSessionInfo._targetCurrency not found");

        private static int _scrapScaleFrame = -1;
        private static int _scrapScalePlayerCount = SessionPlayerCountHelper.VanillaPlayerBaseline;

        internal static bool IsEnabled()
        {
            return ModConfig.EnableEconomy.Value && HostApplyGate.ShouldApplyHostOnlyFeature();
        }

        internal static bool ShouldRetainUnspentCurrency()
        {
            return ModConfig.EnableEconomy.Value
                && ModConfig.RetainUnspentCurrencyBetweenCycles.Value
                && HostApplyGate.ShouldApplyHostOnlyFeature();
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

        internal static int ScaleForType(MoneyType type, int vanilla, int playerCount, bool logAsInfo = false)
        {
            float effective = EconomyResolver.GetEffectiveMultiplier(type, playerCount);
            int scaled = EconomyResolver.ScaleAmount(vanilla, effective);
            if (logAsInfo)
            {
                EconomyLog.InfoApplied(type, vanilla, scaled, playerCount, effective);
            }
            else
            {
                EconomyLog.DebugScaled(type, vanilla, scaled, playerCount, effective);
            }

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
            currency = ScaleForType(MoneyType.Startup, currency, playerCount, logAsInfo: true);
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
            int scaled = ScaleForType(MoneyType.RoundGoal, vanilla, playerCount, logAsInfo: true);
            TargetCurrencyField.SetValue(info, scaled);
        }

        internal static int ScaleScrapValue(int vanilla)
        {
            if (!IsEnabled() || vanilla == 0)
            {
                return vanilla;
            }

            int playerCount = ResolvePlayerCountForScrap();
            float effective = EconomyResolver.GetEffectiveMultiplier(MoneyType.ScrapSellValue, playerCount);
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

        private static int ResolvePlayerCountForScrap()
        {
            int frame = UnityEngine.Time.frameCount;
            if (frame != _scrapScaleFrame)
            {
                _scrapScaleFrame = frame;
                _scrapScalePlayerCount = SessionPlayerCountHelper.ResolveFromSession();
            }

            return _scrapScalePlayerCount;
        }
    }
}
