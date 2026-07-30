namespace MimesisPlayerEnhancement.Features.Economy.Patches
{
    // game@0.3.1 Assembly-CSharp/NewTramLeverLevelObject.cs:L522-534
    [HarmonyPatch(typeof(NewTramLeverLevelObject), nameof(NewTramLeverLevelObject.GetAddtionalSimpleText))]
    internal static class NewTramLeverLevelObjectGetAddtionalSimpleTextPatch
    {
        private const string Feature = "Economy";
        private const string CurrentFundsKey = "STRING_CROSSHAIR_CURRENT_FUNDS";

        [HarmonyPostfix]
        public static void Postfix(ref string __result)
        {
            try
            {
                if (!EconomyApplier.ShouldRetainUnspentCurrency() || string.IsNullOrEmpty(__result))
                {
                    return;
                }

                Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
                if (pdata?.main == null)
                {
                    return;
                }

                int currentCurrency = pdata.main.CurrentCurrency;
                if (currentCurrency <= 0)
                {
                    return;
                }

                string fundsLine = GameLocaleAccess.GetL10NText(CurrentFundsKey, currentCurrency);
                __result = ModL10n.Get("economy.tram_leave_cash_kept") + "\n" + fundsLine;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"GetAddtionalSimpleText postfix failed — {ex.Message}");
            }
        }
    }
}
