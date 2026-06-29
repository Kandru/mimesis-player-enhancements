using System;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.RoomEntryDelay
{
    internal static class RoomEntryDelayResolver
    {
        internal const float MinMultiplier = 0.1f;
        internal const float MaxMultiplier = 10f;
        private const float VanillaEpsilon = 0.001f;

        internal static bool ShouldApply =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() => ModConfig.EnableRoomEntryDelay.Value);

        internal static float GetMultiplier() => ModConfig.RoomEntryDelayMultiplier.Value;

        internal static bool IsVanillaMultiplier(float multiplier) =>
            Math.Abs(multiplier - 1f) < VanillaEpsilon;

        internal static long ScaleDelayMilliseconds(long originalMs, float multiplier)
        {
            if (originalMs <= 0)
            {
                return 0;
            }

            return Math.Max(0L, (long)(originalMs * multiplier));
        }

        internal static float ScaleDurationSeconds(float originalSeconds, float multiplier)
        {
            if (originalSeconds <= 0f)
            {
                return 0f;
            }

            return Math.Max(0f, originalSeconds * multiplier);
        }
    }
}
