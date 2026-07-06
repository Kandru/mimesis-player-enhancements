using System;

namespace MimesisPlayerEnhancement.Features.Economy.Patches
{
    [HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.RefreshTargetCurrency))]
    internal static class GameSessionInfoRefreshTargetCurrencyPatch
    {
        private const string Feature = "Economy";

        [HarmonyPostfix]
        public static void Postfix(GameSessionInfo __instance)
        {
            try
            {
                EconomyApplier.ApplyRoundGoal(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"RefreshTargetCurrency postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GameSessionInfo), "ClampTargetCurrencyToMin")]
    internal static class GameSessionInfoClampTargetCurrencyToMinPatch
    {
        private const string Feature = "Economy";

        [HarmonyPostfix]
        public static void Postfix(GameSessionInfo __instance)
        {
            try
            {
                EconomyApplier.ApplyRoundGoal(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"ClampTargetCurrencyToMin postfix failed — {ex.Message}");
            }
        }
    }
}
