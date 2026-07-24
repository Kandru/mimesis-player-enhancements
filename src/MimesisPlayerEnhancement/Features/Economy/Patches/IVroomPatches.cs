namespace MimesisPlayerEnhancement.Features.Economy.Patches
{
    // game@0.3.1 Assembly-CSharp/IVroom.cs:L125
    [HarmonyPatch(typeof(IVroom), nameof(IVroom.Currency), MethodType.Setter)]
    internal static class IVroomSetCurrencyPatch
    {
        private const string Feature = "Economy";

        [HarmonyPrefix]
        public static void Prefix(IVroom __instance, ref int value)
        {
            try
            {
                if (__instance is not MaintenanceRoom room)
                {
                    return;
                }

                EconomyApplier.ApplyStartupMoney(room, ref value);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"set_Currency prefix failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/IVroom.cs:L897-921
    [HarmonyPatch(typeof(IVroom), nameof(IVroom.GetNewItemElement))]
    internal static class IVroomGetNewItemElementPatch
    {
        private const string Feature = "Economy";

        [HarmonyPostfix]
        public static void Postfix(ref ItemElement? __result)
        {
            if (!EconomyApplier.IsEnabled() || __result == null)
            {
                return;
            }

            try
            {
                EconomyApplier.WarmStaticScrapPriceCache(__result);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"GetNewItemElement postfix failed — {ex.Message}");
            }
        }
    }
}
