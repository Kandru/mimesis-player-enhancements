namespace MimesisPlayerEnhancement.Features.PlayerTuning
{
    internal static class PlayerTuningWeightPenaltyLogic
    {
        internal static int ComputeRate(int totalWeight, int effectiveMaxCarryWeight, int minThresholdMoveSpeedRate)
        {
            if (totalWeight <= 0 || effectiveMaxCarryWeight <= 0)
            {
                return 0;
            }

            double x = Math.Min((double)totalWeight / effectiveMaxCarryWeight, 1.0);
            double thresholdFactor = 1.0 - minThresholdMoveSpeedRate * 0.0001;
            return (int)(Math.Min(Math.Pow(x, 3.0) * thresholdFactor, thresholdFactor) * 10000.0);
        }
    }
}
