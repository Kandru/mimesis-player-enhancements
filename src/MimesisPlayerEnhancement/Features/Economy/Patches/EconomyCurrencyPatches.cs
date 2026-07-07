namespace MimesisPlayerEnhancement.Features.Economy.Patches
{
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
