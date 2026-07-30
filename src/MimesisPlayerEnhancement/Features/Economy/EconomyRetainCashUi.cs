using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.Economy
{
    internal static class EconomyRetainCashUi
    {
        private const string Feature = "Economy";
        private const string CurrentFundsKey = "STRING_CROSSHAIR_CURRENT_FUNDS";

        private static readonly FieldInfo? TramConsoleField =
            AccessTools.Field(typeof(GameMainBase), "tramConsole");
        private static readonly FieldInfo? TramRepairedField =
            AccessTools.Field(typeof(UIPrefab_TramScreen), "_repaired");
        private static readonly FieldInfo? TramRepairingField =
            AccessTools.Field(typeof(UIPrefab_TramScreen), "_repairing");
        private static readonly FieldInfo? TramCurrentCurrencyField =
            AccessTools.Field(typeof(UIPrefab_TramScreen), "_currentCurrency");
        private static readonly PropertyInfo? TramCautionTextProperty =
            AccessTools.Property(typeof(UIPrefab_TramScreen), "UE_CautionText");

        internal static bool IsConfigured() => ModConfig.RetainUnspentCurrencyBetweenCycles.Value;

        internal static void RefreshTramScreenIfVisible()
        {
            try
            {
                if (!GameLocaleAccess.IsMainThread)
                {
                    return;
                }

                Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
                if (pdata?.main == null)
                {
                    return;
                }

                TramConsole? tramConsole = TramConsoleField?.GetValue(pdata.main) as TramConsole;
                if (tramConsole == null)
                {
                    return;
                }

                tramConsole.UpdateRepairGuide(pdata.main.CurrentCurrency);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Refresh tram screen failed — {ex.Message}");
            }
        }

        internal static void TryApplyTramScreenGuide(UIPrefab_TramScreen screen)
        {
            if (!IsConfigured())
            {
                return;
            }

            bool repaired = TramRepairedField?.GetValue(screen) is true;
            bool repairing = TramRepairingField?.GetValue(screen) is true;
            if (!repaired && !repairing)
            {
                return;
            }

            int currency = TramCurrentCurrencyField?.GetValue(screen) is int value ? value : 0;
            if (currency <= 0)
            {
                return;
            }

            ModUiText.SetText(
                TramCautionTextProperty?.GetValue(screen) as Component,
                ModL10n.Get(
                    "economy.tram_screen_cash_kept",
                    new Dictionary<string, object> { ["amount"] = currency }));
        }

        internal static string FormatLeverGuideText(int currency)
        {
            string fundsLine = GameLocaleAccess.GetL10NText(CurrentFundsKey, currency);
            return ModL10n.Get("economy.tram_leave_cash_kept") + "\n" + fundsLine;
        }
    }
}
