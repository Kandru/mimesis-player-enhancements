namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal static class DungeonTimeResolver
    {
        private const long DaySeconds = 86400L;
        internal const float MinTimeMultiplier = -5f;
        internal const float MaxTimeMultiplier = 5f;
        private const double RateEpsilon = 1e-6d;

        internal static double GetBonusSeconds(int playerCount, DungeonTimeSceneConfig config)
        {
            if (!config.EnableDungeonTime)
            {
                return 0d;
            }

            int baseline = config.DungeonTimeBaselinePlayerCount;
            if (playerCount <= baseline)
            {
                return 0d;
            }

            return (playerCount - baseline) * config.ExtraShiftSecondsPerPlayerAboveBaseline;
        }

        internal static long GetBonusMilliseconds(int playerCount, DungeonTimeSceneConfig config)
        {
            return (long)(GetBonusSeconds(playerCount, config) * 1000d);
        }

        /// <summary>
        /// In-game display span from start→end. Wraps by one day when end is at or before start
        /// (vanilla uses &lt; only; ≤ covers Midnight 00:00→00:00 as a full day).
        /// </summary>
        internal static long GetDisplaySpanSeconds(long startSeconds, long endSeconds)
        {
            long end = endSeconds;
            if (end <= startSeconds)
            {
                end += DaySeconds;
            }

            return end - startSeconds;
        }

        /// <summary>
        /// Real remaining ms sized so effectiveStart→end fills the same way vanillaStart→end did.
        /// </summary>
        internal static long GetPresetAdjustedRemainingMs(
            long vanillaRemainingMs,
            long vanillaStartSeconds,
            long effectiveStartSeconds,
            long endSeconds)
        {
            if (vanillaRemainingMs <= 0)
            {
                return vanillaRemainingMs;
            }

            long vanillaSpan = GetDisplaySpanSeconds(vanillaStartSeconds, endSeconds);
            long effectiveSpan = GetDisplaySpanSeconds(effectiveStartSeconds, endSeconds);
            if (vanillaSpan <= 0 || effectiveSpan <= 0 || vanillaSpan == effectiveSpan)
            {
                return vanillaRemainingMs;
            }

            return (long)(vanillaRemainingMs * (double)effectiveSpan / vanillaSpan);
        }

        /// <summary>
        /// Automatic stretch so start→end still fills an extended real shift:
        /// baseRemaining / (baseRemaining + bonus).
        /// </summary>
        internal static double GetStretchScaleFromBonus(long baseRemainingMs, long bonusMs)
        {
            if (baseRemainingMs <= 0 || bonusMs <= 0)
            {
                return 1d;
            }

            return (double)baseRemainingMs / (baseRemainingMs + bonusMs);
        }

        internal static double GetStretchScale(long baseRemainingMs, long extendedRemainingMs)
        {
            if (baseRemainingMs <= 0 || extendedRemainingMs <= baseRemainingMs)
            {
                return 1d;
            }

            return (double)baseRemainingMs / extendedRemainingMs;
        }

        internal static float ClampTimeMultiplier(float value)
        {
            if (value < MinTimeMultiplier)
            {
                return MinTimeMultiplier;
            }

            if (value > MaxTimeMultiplier)
            {
                return MaxTimeMultiplier;
            }

            return value;
        }

        /// <summary>
        /// Live display rate: stretch × <paramref name="timeMultiplier"/>.
        /// </summary>
        internal static double GetEffectiveDisplayRate(double stretchScale, float timeMultiplier)
        {
            if (stretchScale <= 0d)
            {
                stretchScale = 1d;
            }

            return stretchScale * timeMultiplier;
        }

        internal static bool IsNonVanillaDisplayRate(double effectiveRate) =>
            effectiveRate <= 0d || Math.Abs(effectiveRate - 1d) > RateEpsilon;

        /// <summary>
        /// Clock value to set before vanilla <c>value += deltaMs</c> so the net change is
        /// <c>deltaMs * rate</c>, floored at <paramref name="minValueMs"/>.
        /// </summary>
        internal static long GetClockBeforeAdd(long valueMs, long deltaMs, double rate, long minValueMs = 0)
        {
            if (deltaMs <= 0)
            {
                return valueMs;
            }

            long target = (long)(valueMs + (deltaMs * rate));
            if (target < minValueMs)
            {
                target = minValueMs;
            }

            return target - deltaMs;
        }

        internal static float WrapWorldHours(float hours)
        {
            hours %= 24f;
            if (hours < 0f)
            {
                hours += 24f;
            }

            return hours;
        }

        internal static float TimeSpanToWorldHours(TimeSpan time) =>
            WrapWorldHours((float)time.TotalHours);

        /// <summary>
        /// Clamps continuous display hours to the dungeon start→end span.
        /// Reverse stops at start (00:00 for a midnight start) instead of wrapping toward 24:00.
        /// Forward stops at end (24:00 continuous); sky face maps 24→0 via <see cref="ToClockFaceHours"/>.
        /// </summary>
        internal static double ClampDisplayHours(
            double hours,
            long startSeconds,
            long endSeconds)
        {
            float startHours = startSeconds / 3600f;
            long span = GetDisplaySpanSeconds(startSeconds, endSeconds);
            if (span <= 0)
            {
                return hours;
            }

            double endHours = startHours + (span / 3600d);
            if (hours < startHours)
            {
                return startHours;
            }

            if (hours > endHours)
            {
                return endHours;
            }

            return hours;
        }

        /// <summary>
        /// Maps continuous display hours onto the 0..24 sky/clock face.
        /// Exact end-of-span 24:00 becomes 0 for the outdoor float clock; tram still uses TimeSpan.
        /// </summary>
        internal static float ToClockFaceHours(double clampedHours)
        {
            if (clampedHours <= 0d)
            {
                return 0f;
            }

            double face = clampedHours % 24d;
            if (face < 0d)
            {
                face += 24d;
            }

            return (float)face;
        }

        /// <summary>
        /// Display <see cref="TimeSpan"/> for elapsed game seconds from start.
        /// Midnight start at elapsed 0 is <see cref="TimeSpan.Zero"/> (00:00), never 24:00.
        /// </summary>
        internal static TimeSpan ToDisplayTimeSpan(double elapsedGameSeconds, long startSeconds)
        {
            if (elapsedGameSeconds < 0d)
            {
                elapsedGameSeconds = 0d;
            }

            double totalSeconds = elapsedGameSeconds + startSeconds;
            if (totalSeconds <= 0d)
            {
                return TimeSpan.Zero;
            }

            return TimeSpan.FromSeconds(totalSeconds);
        }

        /// <summary>
        /// True when in-game elapsed has filled the start→end shift span (typically ~24:00).
        /// Midnight presets start at 00:00 and are not treated as ended at t=0.
        /// </summary>
        internal static bool HasReachedOrPassedDisplayEnd(
            double elapsedGameSeconds,
            long startSeconds,
            long endSeconds)
        {
            long span = GetDisplaySpanSeconds(startSeconds, endSeconds);
            return span > 0 && elapsedGameSeconds >= span;
        }
    }
}
