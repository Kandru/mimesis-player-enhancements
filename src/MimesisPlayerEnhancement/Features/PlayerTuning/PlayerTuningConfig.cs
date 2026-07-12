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
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_PlayerTuning");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnablePlayerTuning = ModConfig.CreateTrackedEntry(_category,
                "EnablePlayerTuning",
                false);

            ModConfig.MoveSpeedMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MoveSpeedMultiplier",
                1f);

            ModConfig.NoClipSpeedMultiplier = ModConfig.CreateTrackedEntry(_category,
                "NoClipSpeedMultiplier",
                3f);

            ModConfig.MaxStaminaMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MaxStaminaMultiplier",
                1f);

            ModConfig.StaminaDrainMultiplier = ModConfig.CreateTrackedEntry(_category,
                "StaminaDrainMultiplier",
                1f);

            ModConfig.StaminaRegenMultiplier = ModConfig.CreateTrackedEntry(_category,
                "StaminaRegenMultiplier",
                1f);

            ModConfig.StaminaRegenDelayMultiplier = ModConfig.CreateTrackedEntry(_category,
                "StaminaRegenDelayMultiplier",
                1f);

            ModConfig.MaxCarryWeightMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MaxCarryWeightMultiplier",
                1f);

            ModConfig.DisablePlayerCollision = ModConfig.CreateTrackedEntry(_category,
                "DisablePlayerCollision",
                true);
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
