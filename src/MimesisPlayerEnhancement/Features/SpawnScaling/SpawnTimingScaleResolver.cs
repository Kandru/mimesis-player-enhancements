namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class SpawnTimingScaleResolver
    {
        internal const int MaxSpawnRate = 10000;

        internal static int ScaleTryCount(int vanilla, float multiplier)
        {
            return ScalingMath.ScaleCount(vanilla, multiplier);
        }

        internal static int ScaleRate(int vanilla, float multiplier)
        {
            if (vanilla <= 0 || multiplier <= 1f)
            {
                return vanilla;
            }

            return Math.Min(MaxSpawnRate, (int)Math.Round(vanilla * multiplier));
        }
    }
}
