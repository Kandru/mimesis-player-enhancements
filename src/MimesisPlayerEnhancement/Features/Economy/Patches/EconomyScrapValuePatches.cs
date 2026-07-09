using Bifrost.Cooked;

namespace MimesisPlayerEnhancement.Features.Economy.Patches
{
    [HarmonyPatch(typeof(ItemElement), nameof(ItemElement.FinalPrice), MethodType.Getter)]
    internal static class ItemElementFinalPricePatch
    {
        private const string Feature = "Economy";

        [HarmonyPostfix]
        public static void Postfix(ItemElement __instance, ref int __result)
        {
            if (!ModConfig.EnableEconomy.Value)
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

    [HarmonyPatch(typeof(ConsumableItemElement), nameof(ConsumableItemElement.toItemInfo))]
    internal static class ConsumableItemElementToItemInfoPatch
    {
        private const string Feature = "Economy";

        [HarmonyPostfix]
        public static void Postfix(ConsumableItemElement __instance, ref ItemInfo __result)
        {
            if (!ModConfig.EnableEconomy.Value)
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

    [HarmonyPatch(typeof(MiscellanyItemElement), nameof(MiscellanyItemElement.toItemInfo))]
    internal static class MiscellanyItemElementToItemInfoPatch
    {
        private const string Feature = "Economy";

        [HarmonyPostfix]
        public static void Postfix(MiscellanyItemElement __instance, ref ItemInfo __result)
        {
            if (!ModConfig.EnableEconomy.Value)
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

    [HarmonyPatch(typeof(ItemMasterInfo), nameof(ItemMasterInfo.GetMeanPrice))]
    internal static class ItemMasterInfoGetMeanPricePatch
    {
        private const string Feature = "Economy";

        [HarmonyPostfix]
        public static void Postfix(ItemMasterInfo __instance, ref int __result)
        {
            if (!ModConfig.EnableEconomy.Value)
            {
                return;
            }

            try
            {
                __result = EconomyApplier.ScaleCachedScrapMeanPrice(__instance.MasterID, __result);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"GetMeanPrice postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(IVroom), nameof(IVroom.GetNewItemElement))]
    internal static class IVroomGetNewItemElementPatch
    {
        private const string Feature = "Economy";

        [HarmonyPostfix]
        public static void Postfix(ref ItemElement? __result)
        {
            if (!ModConfig.EnableEconomy.Value || __result == null)
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
