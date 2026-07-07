using Bifrost.Cooked;

namespace MimesisPlayerEnhancement.Features.Economy.Patches
{
    [HarmonyPatch(typeof(ItemElement), nameof(ItemElement.FinalPrice), MethodType.Getter)]
    internal static class ItemElementFinalPricePatch
    {
        private const string Feature = "Economy";

        [HarmonyPostfix]
        public static void Postfix(ref int __result)
        {
            if (!ModConfig.EnableEconomy.Value)
            {
                return;
            }

            try
            {
                int scaled = EconomyApplier.ScaleScrapValue(__result);
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

    [HarmonyPatch(typeof(ConsumableItemElement), nameof(ConsumableItemElement.toItemInfo))]
    internal static class ConsumableItemElementToItemInfoPatch
    {
        private const string Feature = "Economy";

        [HarmonyPostfix]
        public static void Postfix(ref ItemInfo __result)
        {
            if (!ModConfig.EnableEconomy.Value)
            {
                return;
            }

            try
            {
                __result.price = EconomyApplier.ScaleScrapValue(__result.price);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"ConsumableItemElement.toItemInfo postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(MiscellanyItemElement), nameof(MiscellanyItemElement.toItemInfo))]
    internal static class MiscellanyItemElementToItemInfoPatch
    {
        private const string Feature = "Economy";

        [HarmonyPostfix]
        public static void Postfix(ref ItemInfo __result)
        {
            if (!ModConfig.EnableEconomy.Value)
            {
                return;
            }

            try
            {
                __result.price = EconomyApplier.ScaleScrapValue(__result.price);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"MiscellanyItemElement.toItemInfo postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(ItemMasterInfo), nameof(ItemMasterInfo.GetMeanPrice))]
    internal static class ItemMasterInfoGetMeanPricePatch
    {
        private const string Feature = "Economy";

        [HarmonyPostfix]
        public static void Postfix(ref int __result)
        {
            if (!ModConfig.EnableEconomy.Value)
            {
                return;
            }

            try
            {
                int scaled = EconomyApplier.ScaleScrapValue(__result);
                if (scaled != __result)
                {
                    __result = scaled;
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"GetMeanPrice postfix failed — {ex.Message}");
            }
        }
    }
}
