namespace MimesisPlayerEnhancement.Util
{
    internal static class ScalingMath
    {
        internal const int VanillaPlayerBaseline = 4;
        internal const float DefaultPlayerCountScaleRate = 0.10f;

        internal static float GetPlayerScale(int playerCount, bool autoScaleEnabled, float scaleRatePerExtraPlayer)
        {
            if (!autoScaleEnabled || playerCount <= VanillaPlayerBaseline)
            {
                return 1f;
            }

            return 1f + (playerCount - VanillaPlayerBaseline) * scaleRatePerExtraPlayer;
        }

        internal static int ScaleCount(int vanilla, float multiplier)
        {
            return vanilla == 0 ? 0 : multiplier <= 0f ? 0 : System.Math.Max(1, (int)System.Math.Round(vanilla * multiplier));
        }

        internal static int ScaleCountWithImplicitBase(int vanilla, float multiplier, int implicitWhenZero)
        {
            int baseCount = vanilla > 0 ? vanilla : implicitWhenZero;
            return ScaleCount(baseCount, multiplier);
        }
    }
}
