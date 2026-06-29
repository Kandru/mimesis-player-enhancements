namespace MimesisPlayerEnhancement.Features.PlayerTuning
{
    internal static class PlayerTuningLog
    {
        private const string Feature = "PlayerTuning";

        internal static void InfoAppliedRuntimeTuning()
        {
            ModLog.Info(
                Feature,
                $"Runtime tuning applied — speed={PlayerTuningResolver.MoveSpeedMultiplier:0.##}×, " +
                $"maxStamina={PlayerTuningResolver.MaxStaminaMultiplier:0.##}×, " +
                $"drain={PlayerTuningResolver.StaminaDrainMultiplier:0.##}×, " +
                $"regen={PlayerTuningResolver.StaminaRegenMultiplier:0.##}×, " +
                $"regenDelay={PlayerTuningResolver.StaminaRegenDelayMultiplier:0.##}×, " +
                $"carryWeight={PlayerTuningResolver.MaxCarryWeightMultiplier:0.##}×");
        }

        internal static void DebugRestoredRuntimeTuning(string reason)
        {
            ModLog.Debug(Feature, $"Runtime tuning restored — {reason}");
        }

        internal static void DebugRefreshedPlayers(int playerCount)
        {
            ModLog.Debug(Feature, $"Refreshed stats for {playerCount} player(s)");
        }

        internal static void DebugSkipped(string reason)
        {
            ModLog.Debug(Feature, $"Skipped — {reason}");
        }
    }
}
