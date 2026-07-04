namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.MimicPossession
{
    internal static class MimicPossessionLog
    {
        private const string Feature = "DeadPlayerFeatures";

        internal static void DebugPossessionDurationRolled(int mimicActorId, long vanillaMs, long rolledMs)
        {
            ModLog.Debug(
                Feature,
                $"Possession duration rolled — actor={mimicActorId}, {vanillaMs}ms -> {rolledMs}ms " +
                $"({ModConfig.MimicPossessionMinTimeSeconds.Value:0.##}-{ModConfig.MimicPossessionMaxTimeSeconds.Value:0.##}s range)");
        }

        internal static void DebugCooltimeScaled(long vanillaMs, long scaledMs, float multiplier)
        {
            ModLog.Debug(
                Feature,
                $"Possession cooltime scaled — {vanillaMs}ms -> {scaledMs}ms ({multiplier:0.##}×)");
        }
    }
}
