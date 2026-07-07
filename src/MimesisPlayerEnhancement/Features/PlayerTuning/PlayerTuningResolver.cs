namespace MimesisPlayerEnhancement.Features.PlayerTuning
{
    internal static class PlayerTuningResolver
    {
        internal const float MinMultiplier = 0.1f;
        internal const float MaxMultiplier = 5f;

        internal static bool IsFeatureEnabled => ModConfig.EnablePlayerTuning.Value;

        internal static float MoveSpeedMultiplier =>
            IsFeatureEnabled ? ModConfig.MoveSpeedMultiplier.Value : 1f;

        internal static float NoClipSpeedMultiplier => ModConfig.NoClipSpeedMultiplier.Value;

        internal static float MaxStaminaMultiplier =>
            IsFeatureEnabled ? ModConfig.MaxStaminaMultiplier.Value : 1f;

        internal static float StaminaDrainMultiplier =>
            IsFeatureEnabled ? ModConfig.StaminaDrainMultiplier.Value : 1f;

        internal static float StaminaRegenMultiplier =>
            IsFeatureEnabled ? ModConfig.StaminaRegenMultiplier.Value : 1f;

        internal static float StaminaRegenDelayMultiplier =>
            IsFeatureEnabled ? ModConfig.StaminaRegenDelayMultiplier.Value : 1f;

        internal static float MaxCarryWeightMultiplier =>
            IsFeatureEnabled ? ModConfig.MaxCarryWeightMultiplier.Value : 1f;
    }
}
