using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    internal static class RoundGoalScalingResolver
    {
        internal const float DefaultBasePerZone = 200f;
        internal const float DefaultCurveExponent = 0.9f;
        internal const int DefaultRandomSpreadPercent = 10;
        internal const float MinCurveExponent = 0.1f;
        internal const float MaxCurveExponent = 2f;

        internal static bool ShouldApply()
        {
            return ModConfig.EnableMorePlayers.Value
                && ModConfig.EnableScalingRoundGoals.Value
                && HostApplyGate.ShouldApplyHostOnlyFeature();
        }

        internal static int ComputeCenter(int stageCount) =>
            ComputeCenter(
                stageCount,
                ModConfig.RoundGoalBasePerZone.Value,
                ModConfig.RoundGoalCurveExponent.Value,
                ModConfig.RoundGoalMoneyMultiplier.Value);

        internal static int ComputeCenter(int stageCount, float basePerZone, float curveExponent, float moneyMultiplier)
        {
            if (stageCount <= 0)
            {
                return 0;
            }

            float stageFactor = Mathf.Pow(stageCount, curveExponent);
            float raw = basePerZone * stageFactor;
            return ScalingMath.ScaleCount(Mathf.RoundToInt(raw), moneyMultiplier);
        }

        internal static int ComputeMin(int stageCount) =>
            ComputeMin(
                stageCount,
                ModConfig.RoundGoalBasePerZone.Value,
                ModConfig.RoundGoalCurveExponent.Value,
                ModConfig.RoundGoalMoneyMultiplier.Value,
                ModConfig.RoundGoalRandomSpreadPercent.Value);

        internal static int ComputeMin(
            int stageCount,
            float basePerZone,
            float curveExponent,
            float moneyMultiplier,
            int spreadPercent)
        {
            int center = ComputeCenter(stageCount, basePerZone, curveExponent, moneyMultiplier);
            if (center <= 0)
            {
                return 0;
            }

            float spread = spreadPercent / 100f;
            return Mathf.Max(1, Mathf.RoundToInt(center * (1f - spread)));
        }

        internal static int ComputeMax(int stageCount) =>
            ComputeMax(
                stageCount,
                ModConfig.RoundGoalBasePerZone.Value,
                ModConfig.RoundGoalCurveExponent.Value,
                ModConfig.RoundGoalMoneyMultiplier.Value,
                ModConfig.RoundGoalRandomSpreadPercent.Value);

        internal static int ComputeMax(
            int stageCount,
            float basePerZone,
            float curveExponent,
            float moneyMultiplier,
            int spreadPercent)
        {
            int center = ComputeCenter(stageCount, basePerZone, curveExponent, moneyMultiplier);
            if (center <= 0)
            {
                return 0;
            }

            float spread = spreadPercent / 100f;
            int min = ComputeMin(stageCount, basePerZone, curveExponent, moneyMultiplier, spreadPercent);
            return Mathf.Max(min, Mathf.RoundToInt(center * (1f + spread)));
        }

        internal static int RollQuota(int stageCount) =>
            RollQuota(
                stageCount,
                ModConfig.RoundGoalBasePerZone.Value,
                ModConfig.RoundGoalCurveExponent.Value,
                ModConfig.RoundGoalMoneyMultiplier.Value,
                ModConfig.RoundGoalRandomSpreadPercent.Value);

        internal static int RollQuota(
            int stageCount,
            float basePerZone,
            float curveExponent,
            float moneyMultiplier,
            int spreadPercent)
        {
            int min = ComputeMin(stageCount, basePerZone, curveExponent, moneyMultiplier, spreadPercent);
            int max = ComputeMax(stageCount, basePerZone, curveExponent, moneyMultiplier, spreadPercent);
            if (min <= 0 || max <= 0)
            {
                return 0;
            }

            return SimpleRandUtil.Next(min, max + 1);
        }
    }
}
