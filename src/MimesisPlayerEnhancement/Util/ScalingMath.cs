namespace MimesisPlayerEnhancement.Util
{
    internal static class ScalingMath
    {
        internal const int VanillaPlayerBaseline = 4;
        internal const float DefaultPerPlayerMultiplier = 0.10f;

        /// <summary>
        /// Additive player-count scaling:
        /// <c>general + max(0, players − baseline) × perPlayer</c>.
        /// </summary>
        internal static float GetAdditiveMultiplier(
            float generalMultiplier,
            float perPlayerMultiplier,
            int playerCount,
            int baselinePlayerCount)
        {
            int baseline = System.Math.Max(1, baselinePlayerCount);
            int extraPlayers = playerCount - baseline;
            if (extraPlayers <= 0 || perPlayerMultiplier <= 0f)
            {
                return System.Math.Max(0f, generalMultiplier);
            }

            return System.Math.Max(0f, generalMultiplier + extraPlayers * perPlayerMultiplier);
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
