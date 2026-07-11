using System.Reflection;

namespace MimesisPlayerEnhancement.Features.MorePlayers.Patches
{
    [HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.RefreshTargetCurrency))]
    internal static class GameSessionInfoRefreshTargetCurrencyPatch
    {
        private const string Feature = "MorePlayers";

        private static readonly FieldInfo TargetCurrencyField =
            AccessTools.Field(typeof(GameSessionInfo), "_targetCurrency")
            ?? throw new InvalidOperationException("GameSessionInfo._targetCurrency not found");

        [HarmonyPostfix]
        public static void Postfix(GameSessionInfo __instance, int stageCount)
        {
            try
            {
                if (!RoundGoalScalingResolver.ShouldApply())
                {
                    return;
                }

                int vanilla = (int)(TargetCurrencyField.GetValue(__instance) ?? 0);
                int quota = RoundGoalScalingResolver.RollQuota(stageCount);
                if (quota <= 0)
                {
                    return;
                }

                TargetCurrencyField.SetValue(__instance, quota);
                if (quota != vanilla)
                {
                    ModLog.Info(Feature, $"Round goal applied — stage={stageCount}, quota={quota}");
                }
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
        private const string Feature = "MorePlayers";

        private static readonly FieldInfo TargetCurrencyField =
            AccessTools.Field(typeof(GameSessionInfo), "_targetCurrency")
            ?? throw new InvalidOperationException("GameSessionInfo._targetCurrency not found");

        [HarmonyPostfix]
        public static void Postfix(GameSessionInfo __instance, int stageCount)
        {
            try
            {
                if (!RoundGoalScalingResolver.ShouldApply())
                {
                    return;
                }

                int vanilla = (int)(TargetCurrencyField.GetValue(__instance) ?? 0);
                int quota = RoundGoalScalingResolver.ComputeMin(stageCount);
                if (quota <= 0)
                {
                    return;
                }

                TargetCurrencyField.SetValue(__instance, quota);
                if (quota != vanilla)
                {
                    ModLog.Info(Feature, $"Round goal clamped — stage={stageCount}, quota={quota}");
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"ClampTargetCurrencyToMin postfix failed — {ex.Message}");
            }
        }
    }
}
