namespace MimesisPlayerEnhancement.Features.Economy.Patches
{
    // game@0.3.1 Assembly-CSharp/ItemElement.cs:L39-56
    [HarmonyPatch(typeof(ItemElement), nameof(ItemElement.FinalPrice), MethodType.Getter)]
    internal static class ItemElementFinalPricePatch
    {
        private const string Feature = "Economy";

        [HarmonyPostfix]
        public static void Postfix(ItemElement __instance, ref int __result)
        {
            if (!EconomyApplier.IsEnabled())
            {
                return;
            }

            try
            {
                int scaled = EconomyApplier.ResolveScrapSellPrice(__instance, __result);
                if (scaled != __result)
                {
                    __result = scaled;
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FinalPrice postfix failed — {ex.Message}");
            }
        }
    }
}
