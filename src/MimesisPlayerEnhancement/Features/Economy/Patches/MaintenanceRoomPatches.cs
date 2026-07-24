namespace MimesisPlayerEnhancement.Features.Economy.Patches
{
    // game@0.3.1 Assembly-CSharp/MaintenanceRoom.cs:L491-500
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

    // game@0.3.1 Assembly-CSharp/MaintenanceRoom.cs:L62-82
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

    // game@0.3.1 Assembly-CSharp/MaintenanceRoom.cs:L757-895
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

    // game@0.3.1 Assembly-CSharp/MaintenanceRoom.cs:L95-115
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

    // game@0.3.1 Assembly-CSharp/MaintenanceRoom.cs:L117-147
    [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.OnRequestStartSession))]
    internal static class MaintenanceRoomOnRequestStartSessionPatch
    {
        private const string Feature = "Economy";

        private static int _retainedCurrencyAmount;

        [HarmonyPrefix]
        public static void Prefix(MaintenanceRoom __instance)
        {
            try
            {
                if (!EconomyApplier.ShouldRetainUnspentCurrency())
                {
                    return;
                }

                _retainedCurrencyAmount = __instance.Currency;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnRequestStartSession prefix failed — {ex.Message}");
            }
        }

        [HarmonyPostfix]
        public static void Postfix(MaintenanceRoom __instance)
        {
            try
            {
                if (!EconomyApplier.ShouldRetainUnspentCurrency())
                {
                    return;
                }

                int retained = _retainedCurrencyAmount;
                if (retained <= 0)
                {
                    return;
                }

                MaintenanceRoomAccess.SetCurrency(__instance, retained);
                EconomyLog.InfoRetainedCurrency(retained, __instance.CurrentStage);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnRequestStartSession postfix failed — {ex.Message}");
            }
        }
    }
}
