namespace MimesisPlayerEnhancement.Features.Economy.Patches
{
    [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.TryGetShopItemPrice))]
    internal static class MaintenanceRoomTryGetShopItemPricePatch
    {
        private const string Feature = "Economy";

        [HarmonyPrefix]
        public static void Prefix(MaintenanceRoom __instance)
        {
            try
            {
                MaintenanceShopApplier.EnsureApplied(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"TryGetShopItemPrice prefix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.InitShopItems))]
    internal static class MaintenanceRoomInitShopItemsPatch
    {
        private const string Feature = "Economy";

        [HarmonyPrefix]
        public static void Prefix(MaintenanceRoom __instance)
        {
            try
            {
                MaintenanceShopApplier.PrepareForShopInit(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"InitShopItems prefix failed — {ex.Message}");
            }
        }

        [HarmonyPostfix]
        public static void Postfix(MaintenanceRoom __instance)
        {
            try
            {
                MaintenanceShopApplier.ApplyAfterShopInit(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"InitShopItems postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.ApplyLoadedGameData))]
    internal static class MaintenanceRoomApplyLoadedGameDataPatch
    {
        private const string Feature = "Economy";

        [HarmonyPostfix]
        public static void Postfix(MaintenanceRoom __instance)
        {
            try
            {
                MaintenanceShopApplier.ApplyAfterLoad(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"ApplyLoadedGameData postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.OnEnterChannel))]
    internal static class MaintenanceRoomOnEnterChannelPatch
    {
        private const string Feature = "Economy";

        [HarmonyPrefix]
        public static void Prefix(MaintenanceRoom __instance)
        {
            try
            {
                MaintenanceShopApplier.EnsureApplied(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnEnterChannel prefix failed — {ex.Message}");
            }
        }
    }
}
