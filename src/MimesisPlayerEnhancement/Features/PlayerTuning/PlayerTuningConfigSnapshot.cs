namespace MimesisPlayerEnhancement.Features.PlayerTuning
{
    internal readonly struct PlayerTuningConfigSnapshot
    {
        internal PlayerTuningConfigSnapshot(
            bool enabled,
            float moveSpeedMultiplier,
            float noClipSpeedMultiplier,
            float maxStaminaMultiplier,
            float staminaDrainMultiplier,
            float staminaRegenMultiplier,
            float staminaRegenDelayMultiplier,
            float maxCarryWeightMultiplier,
            bool disablePlayerCollision)
        {
            Enabled = enabled;
            MoveSpeedMultiplier = moveSpeedMultiplier;
            NoClipSpeedMultiplier = noClipSpeedMultiplier;
            MaxStaminaMultiplier = maxStaminaMultiplier;
            StaminaDrainMultiplier = staminaDrainMultiplier;
            StaminaRegenMultiplier = staminaRegenMultiplier;
            StaminaRegenDelayMultiplier = staminaRegenDelayMultiplier;
            MaxCarryWeightMultiplier = maxCarryWeightMultiplier;
            DisablePlayerCollision = disablePlayerCollision;
        }

        internal bool Enabled { get; }

        internal float MoveSpeedMultiplier { get; }

        internal float NoClipSpeedMultiplier { get; }

        internal float MaxStaminaMultiplier { get; }

        internal float StaminaDrainMultiplier { get; }

        internal float StaminaRegenMultiplier { get; }

        internal float StaminaRegenDelayMultiplier { get; }

        internal float MaxCarryWeightMultiplier { get; }

        internal bool DisablePlayerCollision { get; }

        internal static PlayerTuningConfigSnapshot CaptureFromModConfig()
        {
            return new PlayerTuningConfigSnapshot(
                ModConfig.EnablePlayerTuning.Value,
                ModConfig.MoveSpeedMultiplier.Value,
                ModConfig.NoClipSpeedMultiplier.Value,
                ModConfig.MaxStaminaMultiplier.Value,
                ModConfig.StaminaDrainMultiplier.Value,
                ModConfig.StaminaRegenMultiplier.Value,
                ModConfig.StaminaRegenDelayMultiplier.Value,
                ModConfig.MaxCarryWeightMultiplier.Value,
                ModConfig.DisablePlayerCollision.Value);
        }
    }
}
