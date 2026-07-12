using MelonLoader;

namespace MimesisPlayerEnhancement.Features.MimicTuning
{
    internal static class MimicTuningConfig
    {
        internal const string SectionId = "MimesisPlayerEnhancement_MimicTuning";

        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory(SectionId);
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableMimicTuning = ModConfig.CreateTrackedEntry(_category,
                "EnableMimicTuning",
                false);

            ModConfig.MimicVoiceTuningMode = ModConfig.CreateTrackedEntry(_category,
                "MimicVoiceTuningMode",
                nameof(MimicVoiceTuningMode.Vanilla));

            ModConfig.PeriodicVoiceIntervalMultiplier = ModConfig.CreateTrackedEntry(_category,
                "PeriodicVoiceIntervalMultiplier",
                1f);

            ModConfig.PlayerVoiceResponseChancePercent = ModConfig.CreateTrackedEntry(_category,
                "PlayerVoiceResponseChancePercent",
                100);

            ModConfig.PlayerVoiceResponseCooldownSeconds = ModConfig.CreateTrackedEntry(_category,
                "PlayerVoiceResponseCooldownSeconds",
                3f);

            ModConfig.PlayerVoiceResponseDelayMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "PlayerVoiceResponseDelayMinSeconds",
                0.2f);

            ModConfig.PlayerVoiceResponseDelayMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "PlayerVoiceResponseDelayMaxSeconds",
                0.2f);

            ModConfig.PlayerVoiceResponseMaxDistance = ModConfig.CreateTrackedEntry(_category,
                "PlayerVoiceResponseMaxDistance",
                20f);

            ModConfig.MimicInventoryCopyMode = ModConfig.CreateTrackedEntry(_category,
                "MimicInventoryCopyMode",
                nameof(MimicInventoryCopyMode.Vanilla));

            ModConfig.MimicInventoryCopyPickRule = ModConfig.CreateTrackedEntry(_category,
                "MimicInventoryCopyPickRule",
                nameof(BTTargetPickRule.MinDistance));

            ModConfig.EnableMimicPossessionTuning = ModConfig.CreateTrackedEntry(_category,
                "EnableMimicPossessionTuning",
                false);

            ModConfig.RandomizeMimicPossessionDuration = ModConfig.CreateTrackedEntry(_category,
                "RandomizeMimicPossessionDuration",
                false);

            ModConfig.MimicPossessionMinTimeSeconds = ModConfig.CreateTrackedEntry(_category,
                "MimicPossessionMinTimeSeconds",
                MimicPossessionResolver.VanillaPossessionDurationSeconds);

            ModConfig.MimicPossessionMaxTimeSeconds = ModConfig.CreateTrackedEntry(_category,
                "MimicPossessionMaxTimeSeconds",
                MimicPossessionResolver.VanillaPossessionDurationSeconds);

            ModConfig.MimicPossessionCooltimeMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MimicPossessionCooltimeMultiplier",
                1f);

            MimicVoiceTuningResolver.RefreshConfigCache();
            MimicInventoryCopyResolver.RefreshConfigCache();
            MimicPossessionResolver.RefreshConfigCache();
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableMimicTuning.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableMimicTuning));
            ModConfig.MimicVoiceTuningMode.OnEntryValueChanged.Subscribe((_, value) =>
                OnVoiceModeChanged(logger, value));
            ModConfig.PeriodicVoiceIntervalMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnIntervalMultiplierChanged(logger, value));
            ModConfig.PlayerVoiceResponseChancePercent.OnEntryValueChanged.Subscribe((_, value) =>
                OnChanceChanged(logger, value));
            ModConfig.PlayerVoiceResponseCooldownSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnCooldownChanged(logger, value));
            ModConfig.PlayerVoiceResponseDelayMinSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnDelayRangeChanged(logger, value, ModConfig.PlayerVoiceResponseDelayMinSeconds, ModConfig.PlayerVoiceResponseDelayMaxSeconds));
            ModConfig.PlayerVoiceResponseDelayMaxSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnDelayRangeChanged(logger, value, ModConfig.PlayerVoiceResponseDelayMinSeconds, ModConfig.PlayerVoiceResponseDelayMaxSeconds));
            ModConfig.PlayerVoiceResponseMaxDistance.OnEntryValueChanged.Subscribe((_, value) =>
                OnDistanceChanged(logger, value));
            ModConfig.MimicInventoryCopyMode.OnEntryValueChanged.Subscribe((_, value) =>
                OnInventoryModeChanged(logger, value));
            ModConfig.MimicInventoryCopyPickRule.OnEntryValueChanged.Subscribe((_, value) =>
                OnInventoryPickRuleChanged(logger, value));
            ModConfig.EnableMimicPossessionTuning.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableMimicPossessionTuning));
            ModConfig.RandomizeMimicPossessionDuration.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.RandomizeMimicPossessionDuration));
            ModConfig.MimicPossessionMinTimeSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnMimicPossessionDurationSecondsChanged(logger, value, ModConfig.MimicPossessionMinTimeSeconds));
            ModConfig.MimicPossessionMaxTimeSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnMimicPossessionDurationSecondsChanged(logger, value, ModConfig.MimicPossessionMaxTimeSeconds));
            ModConfig.MimicPossessionCooltimeMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnMimicPossessionCooltimeMultiplierChanged(logger, value));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.PeriodicVoiceIntervalMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.PlayerVoiceResponseCooldownSeconds);
            ModConfig.TrackFloatEntry(ModConfig.PlayerVoiceResponseDelayMinSeconds);
            ModConfig.TrackFloatEntry(ModConfig.PlayerVoiceResponseDelayMaxSeconds);
            ModConfig.TrackFloatEntry(ModConfig.PlayerVoiceResponseMaxDistance);
            ModConfig.TrackFloatEntry(ModConfig.MimicPossessionMinTimeSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MimicPossessionMaxTimeSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MimicPossessionCooltimeMultiplier);
        }

        private static void OnVoiceModeChanged(MelonLogger.Instance logger, string value)
        {
            if (!IsValidVoiceMode(value))
            {
                logger.Warning("MimicVoiceTuningMode must be Vanilla or Custom; resetting to Vanilla.");
                ModConfig.MimicVoiceTuningMode.Value = nameof(MimicVoiceTuningMode.Vanilla);
                return;
            }

            ModConfig.NotifyChanged(ModConfig.MimicVoiceTuningMode);
        }

        private static void OnInventoryModeChanged(MelonLogger.Instance logger, string value)
        {
            if (!IsValidInventoryMode(value))
            {
                logger.Warning("MimicInventoryCopyMode must be Vanilla or Custom; resetting to Vanilla.");
                ModConfig.MimicInventoryCopyMode.Value = nameof(MimicInventoryCopyMode.Vanilla);
                return;
            }

            ModConfig.NotifyChanged(ModConfig.MimicInventoryCopyMode);
        }

        private static void OnInventoryPickRuleChanged(MelonLogger.Instance logger, string value)
        {
            if (!IsValidPickRule(value))
            {
                logger.Warning("MimicInventoryCopyPickRule must be MinDistance, MaxDistance, or Random; resetting to MinDistance.");
                ModConfig.MimicInventoryCopyPickRule.Value = nameof(BTTargetPickRule.MinDistance);
                return;
            }

            ModConfig.NotifyChanged(ModConfig.MimicInventoryCopyPickRule);
        }

        private static void OnIntervalMultiplierChanged(MelonLogger.Instance logger, float value)
        {
            if (value < 0.05f)
            {
                logger.Warning("PeriodicVoiceIntervalMultiplier must be >= 0.05; resetting to 0.05.");
                ModConfig.PeriodicVoiceIntervalMultiplier.Value = 0.05f;
                return;
            }

            ModConfig.NotifyChanged(ModConfig.PeriodicVoiceIntervalMultiplier);
        }

        private static void OnChanceChanged(MelonLogger.Instance logger, int value)
        {
            if (value is < 0 or > 100)
            {
                logger.Warning("PlayerVoiceResponseChancePercent must be 0–100; clamping.");
                ModConfig.PlayerVoiceResponseChancePercent.Value = Math.Clamp(value, 0, 100);
                return;
            }

            ModConfig.NotifyChanged(ModConfig.PlayerVoiceResponseChancePercent);
        }

        private static void OnCooldownChanged(MelonLogger.Instance logger, float value)
        {
            if (value < 0f)
            {
                logger.Warning("PlayerVoiceResponseCooldownSeconds must be >= 0; resetting to 0.");
                ModConfig.PlayerVoiceResponseCooldownSeconds.Value = 0f;
                return;
            }

            ModConfig.NotifyChanged(ModConfig.PlayerVoiceResponseCooldownSeconds);
        }

        private static void OnDistanceChanged(MelonLogger.Instance logger, float value)
        {
            if (value < 1f)
            {
                logger.Warning("PlayerVoiceResponseMaxDistance must be >= 1; resetting to 1.");
                ModConfig.PlayerVoiceResponseMaxDistance.Value = 1f;
                return;
            }

            ModConfig.NotifyChanged(ModConfig.PlayerVoiceResponseMaxDistance);
        }

        private static void OnDelayRangeChanged(
            MelonLogger.Instance logger,
            float value,
            MelonPreferences_Entry<float> minEntry,
            MelonPreferences_Entry<float> maxEntry)
        {
            if (value < 0f)
            {
                logger.Warning($"{minEntry.Identifier} must be >= 0; resetting to 0.");
                minEntry.Value = 0f;
                return;
            }

            if (maxEntry.Value < minEntry.Value)
            {
                maxEntry.Value = minEntry.Value;
            }

            ModConfig.NotifyChanged(minEntry);
        }

        private static bool IsValidVoiceMode(string value) =>
            string.Equals(value, nameof(MimicVoiceTuningMode.Vanilla), StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, nameof(MimicVoiceTuningMode.Custom), StringComparison.OrdinalIgnoreCase);

        private static bool IsValidInventoryMode(string value) =>
            string.Equals(value, nameof(MimicInventoryCopyMode.Vanilla), StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, nameof(MimicInventoryCopyMode.Custom), StringComparison.OrdinalIgnoreCase);

        private static bool IsValidPickRule(string value) =>
            string.Equals(value, nameof(BTTargetPickRule.MinDistance), StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, nameof(BTTargetPickRule.MaxDistance), StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, nameof(BTTargetPickRule.Random), StringComparison.OrdinalIgnoreCase);

        private static void OnMimicPossessionDurationSecondsChanged(
            MelonLogger.Instance logger,
            float value,
            MelonPreferences_Entry<float> entry)
        {
            if (value < MimicPossessionResolver.MinDurationSeconds)
            {
                logger.Warning(
                    $"{entry.Identifier} must be at least {MimicPossessionResolver.MinDurationSeconds}; resetting.");
                entry.Value = MimicPossessionResolver.MinDurationSeconds;
                return;
            }

            if (value > MimicPossessionResolver.MaxDurationSeconds)
            {
                logger.Warning(
                    $"{entry.Identifier} must be at most {MimicPossessionResolver.MaxDurationSeconds}; resetting.");
                entry.Value = MimicPossessionResolver.MaxDurationSeconds;
                return;
            }

            float min = ModConfig.MimicPossessionMinTimeSeconds.Value;
            float max = ModConfig.MimicPossessionMaxTimeSeconds.Value;
            if (max < min)
            {
                logger.Warning(
                    "MimicPossessionMaxTimeSeconds must be >= MimicPossessionMinTimeSeconds; syncing max to min.");
                ModConfig.MimicPossessionMaxTimeSeconds.Value = min;
            }

            ModConfigFloatHelper.SanitizeEntry(entry);
            ModConfig.NotifyChanged(entry);
        }

        private static void OnMimicPossessionCooltimeMultiplierChanged(MelonLogger.Instance logger, float value)
        {
            if (value < MimicPossessionResolver.MinCooltimeMultiplier)
            {
                logger.Warning(
                    $"MimicPossessionCooltimeMultiplier must be at least {MimicPossessionResolver.MinCooltimeMultiplier}; resetting.");
                ModConfig.MimicPossessionCooltimeMultiplier.Value = MimicPossessionResolver.MinCooltimeMultiplier;
                return;
            }

            if (value > MimicPossessionResolver.MaxCooltimeMultiplier)
            {
                logger.Warning(
                    $"MimicPossessionCooltimeMultiplier must be at most {MimicPossessionResolver.MaxCooltimeMultiplier}; resetting.");
                ModConfig.MimicPossessionCooltimeMultiplier.Value = MimicPossessionResolver.MaxCooltimeMultiplier;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(ModConfig.MimicPossessionCooltimeMultiplier);
            ModConfig.NotifyChanged(ModConfig.MimicPossessionCooltimeMultiplier);
        }
    }
}
