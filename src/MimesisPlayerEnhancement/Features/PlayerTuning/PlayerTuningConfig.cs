using MelonLoader;

namespace MimesisPlayerEnhancement.Features.PlayerTuning
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_PlayerTuning] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class PlayerTuningConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_PlayerTuning", "Player Tuning");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnablePlayerTuning = ModConfig.CreateTrackedEntry(_category,
                "EnablePlayerTuning",
                false,
                "Enable Player Tuning",
                "Scale player move speed, stamina, and carry weight. Joining clients do not need the mod. Host only.");

            ModConfig.MoveSpeedMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MoveSpeedMultiplier",
                1f,
                "Move Speed Multiplier",
                "Scales walk and run base speed (1 = vanilla, 2 = double).");

            ModConfig.NoClipSpeedMultiplier = ModConfig.CreateTrackedEntry(_category,
                "NoClipSpeedMultiplier",
                3f,
                "Noclip Speed Multiplier",
                "Scales dashboard noclip fly speed relative to the player's current walk/run speed (3 = triple). Only applies while noclip is active.");

            ModConfig.MaxStaminaMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MaxStaminaMultiplier",
                1f,
                "Max Stamina Multiplier",
                "Scales maximum stamina (1 = vanilla, 2 = double).");

            ModConfig.StaminaDrainMultiplier = ModConfig.CreateTrackedEntry(_category,
                "StaminaDrainMultiplier",
                1f,
                "Stamina Drain Multiplier",
                "Scales sprint stamina cost per tick (1 = vanilla, 0.5 = half drain).");

            ModConfig.StaminaRegenMultiplier = ModConfig.CreateTrackedEntry(_category,
                "StaminaRegenMultiplier",
                1f,
                "Stamina Regen Multiplier",
                "Scales stamina recovered per regen tick (1 = vanilla, 2 = double).");

            ModConfig.StaminaRegenDelayMultiplier = ModConfig.CreateTrackedEntry(_category,
                "StaminaRegenDelayMultiplier",
                1f,
                "Stamina Regen Delay Multiplier",
                "Scales wait time before stamina regen starts after sprinting (1 = vanilla, 0.5 = regen starts sooner).");

            ModConfig.MaxCarryWeightMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MaxCarryWeightMultiplier",
                1f,
                "Max Carry Weight Multiplier",
                "Scales carry capacity before encumbrance slows movement (1 = vanilla, 2 = double capacity).");

            ModConfig.DisablePlayerCollision = ModConfig.CreateTrackedEntry(_category,
                "DisablePlayerCollision",
                true,
                "Disable Player Collision",
                "On the local client, disable capsule colliders on other players and mimics so you can walk through them (e.g. crowded tram). Regular monsters and walls remain solid. Local effect only; requires Enable Player Tuning.");
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnablePlayerTuning.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnablePlayerTuning));
            ModConfig.DisablePlayerCollision.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.DisablePlayerCollision));
            ModConfig.MoveSpeedMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, ModConfig.MoveSpeedMultiplier));
            ModConfig.NoClipSpeedMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, ModConfig.NoClipSpeedMultiplier));
            ModConfig.MaxStaminaMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, ModConfig.MaxStaminaMultiplier));
            ModConfig.StaminaDrainMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, ModConfig.StaminaDrainMultiplier));
            ModConfig.StaminaRegenMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, ModConfig.StaminaRegenMultiplier));
            ModConfig.StaminaRegenDelayMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, ModConfig.StaminaRegenDelayMultiplier));
            ModConfig.MaxCarryWeightMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, ModConfig.MaxCarryWeightMultiplier));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.MoveSpeedMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.NoClipSpeedMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.MaxStaminaMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.StaminaDrainMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.StaminaRegenMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.StaminaRegenDelayMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.MaxCarryWeightMultiplier);
        }

        private static void OnPlayerTuningMultiplierChanged(
            MelonLogger.Instance logger,
            float value,
            MelonPreferences_Entry<float> entry)
        {
            if (value < PlayerTuningResolver.MinMultiplier)
            {
                logger.Warning(
                    $"{entry.Identifier} must be at least {PlayerTuningResolver.MinMultiplier}; resetting.");
                entry.Value = PlayerTuningResolver.MinMultiplier;
                return;
            }

            if (value > PlayerTuningResolver.MaxMultiplier)
            {
                logger.Warning(
                    $"{entry.Identifier} must be at most {PlayerTuningResolver.MaxMultiplier}; resetting.");
                entry.Value = PlayerTuningResolver.MaxMultiplier;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(entry);
            ModConfig.NotifyChanged(entry);
        }
    }
}
