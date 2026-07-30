using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Economy.Patches
{
    // game@0.3.1 Assembly-CSharp/NewTramLeverLevelObject.cs:L522-534
    [HarmonyPatch]
    internal static class NewTramLeverLevelObjectGetAddtionalSimpleTextPatch
    {
        private const string Feature = "Economy";

        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(NewTramLeverLevelObject), "GetAddtionalSimpleText", [typeof(ProtoActor)]);

        [HarmonyPostfix]
        public static void Postfix(ref string __result)
        {
            try
            {
                if (string.IsNullOrEmpty(__result) || !EconomyRetainCashUi.IsConfigured())
                {
                    return;
                }

                Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
                if (pdata?.main == null)
                {
                    return;
                }

                int currency = pdata.main.CurrentCurrency;
                if (currency <= 0)
                {
                    return;
                }

                __result = EconomyRetainCashUi.FormatLeverGuideText(currency);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"GetAddtionalSimpleText postfix failed — {ex.Message}");
            }
        }
    }
}
