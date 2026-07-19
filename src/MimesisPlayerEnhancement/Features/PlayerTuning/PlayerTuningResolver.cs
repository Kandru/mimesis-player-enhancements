namespace MimesisPlayerEnhancement.Features.PlayerTuning
{
    internal static class PlayerTuningResolver
    {
        internal const float MinMultiplier = 0.1f;
        internal const float MaxMultiplier = 5f;

        internal static bool IsFeatureEnabled => GetIsFeatureEnabled(PlayerTuningConfigSnapshot.CaptureFromModConfig());

        internal static float MoveSpeedMultiplier =>
            GetMoveSpeedMultiplier(PlayerTuningConfigSnapshot.CaptureFromModConfig());

        internal static float NoClipSpeedMultiplier =>
            GetNoClipSpeedMultiplier(PlayerTuningConfigSnapshot.CaptureFromModConfig());

        internal static float MaxStaminaMultiplier =>
            GetMaxStaminaMultiplier(PlayerTuningConfigSnapshot.CaptureFromModConfig());

        internal static float StaminaDrainMultiplier =>
            GetStaminaDrainMultiplier(PlayerTuningConfigSnapshot.CaptureFromModConfig());

        internal static float StaminaRegenMultiplier =>
            GetStaminaRegenMultiplier(PlayerTuningConfigSnapshot.CaptureFromModConfig());

        internal static float StaminaRegenDelayMultiplier =>
            GetStaminaRegenDelayMultiplier(PlayerTuningConfigSnapshot.CaptureFromModConfig());

        internal static float MaxCarryWeightMultiplier =>
            GetMaxCarryWeightMultiplier(PlayerTuningConfigSnapshot.CaptureFromModConfig());

        internal static bool DisablePlayerCollision =>
            GetDisablePlayerCollision(PlayerTuningConfigSnapshot.CaptureFromModConfig());

        internal static bool GetIsFeatureEnabled(PlayerTuningConfigSnapshot config) => config.Enabled;

        internal static float GetMoveSpeedMultiplier(PlayerTuningConfigSnapshot config) =>
            config.Enabled ? config.MoveSpeedMultiplier : 1f;

        internal static float GetNoClipSpeedMultiplier(PlayerTuningConfigSnapshot config) =>
            config.NoClipSpeedMultiplier;

        internal static float GetMaxStaminaMultiplier(PlayerTuningConfigSnapshot config) =>
            config.Enabled ? config.MaxStaminaMultiplier : 1f;

        internal static float GetStaminaDrainMultiplier(PlayerTuningConfigSnapshot config) =>
            config.Enabled ? config.StaminaDrainMultiplier : 1f;

        internal static float GetStaminaRegenMultiplier(PlayerTuningConfigSnapshot config) =>
            config.Enabled ? config.StaminaRegenMultiplier : 1f;

        internal static float GetStaminaRegenDelayMultiplier(PlayerTuningConfigSnapshot config) =>
            config.Enabled ? config.StaminaRegenDelayMultiplier : 1f;

        internal static float GetMaxCarryWeightMultiplier(PlayerTuningConfigSnapshot config) =>
            config.Enabled ? config.MaxCarryWeightMultiplier : 1f;

        internal static bool GetDisablePlayerCollision(PlayerTuningConfigSnapshot config) =>
            config.Enabled && config.DisablePlayerCollision;
    }
}
