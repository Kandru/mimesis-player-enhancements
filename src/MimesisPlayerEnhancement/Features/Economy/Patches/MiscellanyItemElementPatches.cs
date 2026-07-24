namespace MimesisPlayerEnhancement.Features.Economy.Patches
{
    // game@0.3.1 Assembly-CSharp/MiscellanyItemElement.cs:L16-26
    [HarmonyPatch(typeof(MiscellanyItemElement), nameof(MiscellanyItemElement.toItemInfo))]
    internal static class MiscellanyItemElementToItemInfoPatch
    {
        private const string Feature = "Economy";

        [HarmonyPostfix]
        public static void Postfix(MiscellanyItemElement __instance, ref ItemInfo __result)
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
                ModLog.Warn(Feature, $"MiscellanyItemElement.toItemInfo postfix failed — {ex.Message}");
            }
        }
    }
}
