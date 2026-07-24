namespace MimesisPlayerEnhancement.Features.PlayerTuning
{
    internal static class PlayerTuningLog
    {
        private const string Feature = "PlayerTuning";

        internal static void InfoAppliedRuntimeTuning(PlayerTuningConfigSnapshot config)
        {
            ModLog.Info(
                Feature,
                $"Runtime tuning applied — speed={PlayerTuningResolver.GetMoveSpeedMultiplier(config):0.##}×, " +
                $"maxStamina={PlayerTuningResolver.GetMaxStaminaMultiplier(config):0.##}×, " +
                $"drain={PlayerTuningResolver.GetStaminaDrainMultiplier(config):0.##}×, " +
                $"regen={PlayerTuningResolver.GetStaminaRegenMultiplier(config):0.##}×, " +
                $"regenDelay={PlayerTuningResolver.GetStaminaRegenDelayMultiplier(config):0.##}×, " +
                $"carryWeight={PlayerTuningResolver.GetMaxCarryWeightMultiplier(config):0.##}×");
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

        internal static void DebugUpdatedPlayerCollision(int actorCount, bool disabled)
        {
            ModLog.Debug(
                Feature,
                $"Pass-through collision {(disabled ? "disabled" : "restored")} on {actorCount} player/mimic actor(s)");
        }
    }
}
