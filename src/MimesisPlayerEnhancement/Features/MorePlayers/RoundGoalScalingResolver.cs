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

        internal static int ComputeCenter(int stageCount)
        {
            if (stageCount <= 0)
            {
                return 0;
            }

            float stageFactor = Mathf.Pow(stageCount, ModConfig.RoundGoalCurveExponent.Value);
            float raw = ModConfig.RoundGoalBasePerZone.Value * stageFactor;
            return ScalingMath.ScaleCount(Mathf.RoundToInt(raw), ModConfig.RoundGoalMoneyMultiplier.Value);
        }

        internal static int ComputeMin(int stageCount)
        {
            int center = ComputeCenter(stageCount);
            if (center <= 0)
            {
                return 0;
            }

            float spread = ModConfig.RoundGoalRandomSpreadPercent.Value / 100f;
            return Mathf.Max(1, Mathf.RoundToInt(center * (1f - spread)));
        }

        internal static int ComputeMax(int stageCount)
        {
            int center = ComputeCenter(stageCount);
            if (center <= 0)
            {
                return 0;
            }

            float spread = ModConfig.RoundGoalRandomSpreadPercent.Value / 100f;
            int min = ComputeMin(stageCount);
            return Mathf.Max(min, Mathf.RoundToInt(center * (1f + spread)));
        }

        internal static int RollQuota(int stageCount)
        {
            int min = ComputeMin(stageCount);
            int max = ComputeMax(stageCount);
            if (min <= 0 || max <= 0)
            {
                return 0;
            }

            return SimpleRandUtil.Next(min, max + 1);
        }
    }
}
