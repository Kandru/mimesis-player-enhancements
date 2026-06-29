namespace MimesisPlayerEnhancement.Features.PlayerTuning
{
    internal static class PlayerTuningResolver
    {
        internal const float MinMultiplier = 0.1f;
        internal const float MaxMultiplier = 5f;

        internal static bool IsFeatureEnabled => ModConfig.EnablePlayerTuning.Value;

        internal static float MoveSpeedMultiplier => ModConfig.MoveSpeedMultiplier.Value;

        internal static float MaxStaminaMultiplier => ModConfig.MaxStaminaMultiplier.Value;

        internal static float StaminaDrainMultiplier => ModConfig.StaminaDrainMultiplier.Value;

        internal static float StaminaRegenMultiplier => ModConfig.StaminaRegenMultiplier.Value;

        internal static float StaminaRegenDelayMultiplier => ModConfig.StaminaRegenDelayMultiplier.Value;

        internal static float MaxCarryWeightMultiplier => ModConfig.MaxCarryWeightMultiplier.Value;
    }
}
