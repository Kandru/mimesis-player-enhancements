namespace MimesisPlayerEnhancement.Features.Economy.Patches
{
    // game@0.3.1 Assembly-CSharp/ConsumableItemElement.cs:L27-38
    [HarmonyPatch(typeof(ConsumableItemElement), nameof(ConsumableItemElement.toItemInfo))]
    internal static class ConsumableItemElementToItemInfoPatch
    {
        private const string Feature = "Economy";

        [HarmonyPostfix]
        public static void Postfix(ConsumableItemElement __instance, ref ItemInfo __result)
        {
            if (!EconomyApplier.IsEnabled())
            {
                return;
            }

            try
            {
                __result.price = EconomyApplier.ResolveScrapSellPrice(__instance, __result.price);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"ConsumableItemElement.toItemInfo postfix failed — {ex.Message}");
            }
        }
    }
}
