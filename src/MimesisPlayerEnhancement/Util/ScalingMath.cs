namespace MimesisPlayerEnhancement.Util;

internal static class ScalingMath
{
    internal const int VanillaPlayerBaseline = 4;

    internal static float GetPlayerScale(int playerCount, bool autoScaleEnabled)
    {
        if (!autoScaleEnabled || playerCount <= VanillaPlayerBaseline)
            return 1f;

        return playerCount / (float)VanillaPlayerBaseline;
    }

    internal static int ScaleCount(int vanilla, float multiplier)
    {
        if (vanilla == 0)
            return 0;

        if (multiplier <= 0f)
            return 0;

        return System.Math.Max(1, (int)System.Math.Round(vanilla * multiplier));
    }

    internal static int ScaleCountWithImplicitBase(int vanilla, float multiplier, int implicitWhenZero)
    {
        int baseCount = vanilla > 0 ? vanilla : implicitWhenZero;
        return ScaleCount(baseCount, multiplier);
    }
}
