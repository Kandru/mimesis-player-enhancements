using MelonLoader;

namespace MimesisPlayerEnhancement.Features.MimicTuning
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_MimicTuning] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class MimicTuningConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_MimicTuning", "Mimic Tuning");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableMimicTuning = ModConfig.CreateTrackedEntry(_category,
                "EnableMimicTuning",
                false,
                "Enable Mimic Tuning",
                "Tune dead-player mimic possession speak duration and cooldown on the host.");

            ModConfig.RandomizeMimicPossessionDuration = ModConfig.CreateTrackedEntry(_category,
                "RandomizeMimicPossessionDuration",
                false,
                "Randomize Mimic Possession Duration",
                "Roll a random speak window per E-possession between min and max seconds below. Host only.");

            ModConfig.MimicPossessionMinTimeSeconds = ModConfig.CreateTrackedEntry(_category,
                "MimicPossessionMinTimeSeconds",
                MimicTuningResolver.VanillaPossessionDurationSeconds,
                "Mimic Possession Min Time (seconds)",
                "Minimum rolled speak duration in seconds (vanilla is 12). Host only.");

            ModConfig.MimicPossessionMaxTimeSeconds = ModConfig.CreateTrackedEntry(_category,
                "MimicPossessionMaxTimeSeconds",
                MimicTuningResolver.VanillaPossessionDurationSeconds,
                "Mimic Possession Max Time (seconds)",
                "Maximum rolled speak duration in seconds (vanilla is 12). Host only.");

            ModConfig.MimicPossessionCooltimeMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MimicPossessionCooltimeMultiplier",
                1f,
                "Mimic Possession Cooltime Multiplier",
                "Multiplier for wait time after mimic possession before the next E-possession (1 = vanilla). Host only.");
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableMimicTuning.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableMimicTuning));
            ModConfig.RandomizeMimicPossessionDuration.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.RandomizeMimicPossessionDuration));
            ModConfig.MimicPossessionMinTimeSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnMimicPossessionDurationSecondsChanged(logger, value, ModConfig.MimicPossessionMinTimeSeconds));
            ModConfig.MimicPossessionMaxTimeSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnMimicPossessionDurationSecondsChanged(logger, value, ModConfig.MimicPossessionMaxTimeSeconds));
            ModConfig.MimicPossessionCooltimeMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnMimicPossessionCooltimeMultiplierChanged(logger, value));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.MimicPossessionMinTimeSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MimicPossessionMaxTimeSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MimicPossessionCooltimeMultiplier);
        }

        internal static void MigrateLegacyKeys(MelonLogger.Instance logger)
        {
            bool migrated = false;
            migrated |= TryMigrateLegacyMimicPossessionTimeMultiplier(
                "MimicPossessionMinTimeMultiplier",
                ModConfig.MimicPossessionMinTimeSeconds);
            migrated |= TryMigrateLegacyMimicPossessionTimeMultiplier(
                "MimicPossessionMaxTimeMultiplier",
                ModConfig.MimicPossessionMaxTimeSeconds);

            if (migrated)
            {
                logger.Msg(
                    "Mimic Tuning config migrated — possession min/max multiplier keys converted to seconds.");
            }
        }

        private static bool TryMigrateLegacyMimicPossessionTimeMultiplier(
            string legacyKey,
            MelonPreferences_Entry<float> targetEntry)
        {
            if (_category.GetEntry<float>(legacyKey) is not MelonPreferences_Entry<float> legacyEntry)
            {
                return false;
            }

            targetEntry.Value = legacyEntry.Value
                * MimicTuningResolver.VanillaPossessionDurationSeconds;
            return true;
        }

        private static void OnMimicPossessionDurationSecondsChanged(
            MelonLogger.Instance logger,
            float value,
            MelonPreferences_Entry<float> entry)
        {
            if (value < MimicTuningResolver.MinDurationSeconds)
            {
                logger.Warning(
                    $"{entry.Identifier} must be at least {MimicTuningResolver.MinDurationSeconds}; resetting.");
                entry.Value = MimicTuningResolver.MinDurationSeconds;
                return;
            }

            if (value > MimicTuningResolver.MaxDurationSeconds)
            {
                logger.Warning(
                    $"{entry.Identifier} must be at most {MimicTuningResolver.MaxDurationSeconds}; resetting.");
                entry.Value = MimicTuningResolver.MaxDurationSeconds;
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
            if (value < MimicTuningResolver.MinCooltimeMultiplier)
            {
                logger.Warning(
                    $"MimicPossessionCooltimeMultiplier must be at least {MimicTuningResolver.MinCooltimeMultiplier}; resetting.");
                ModConfig.MimicPossessionCooltimeMultiplier.Value = MimicTuningResolver.MinCooltimeMultiplier;
                return;
            }

            if (value > MimicTuningResolver.MaxCooltimeMultiplier)
            {
                logger.Warning(
                    $"MimicPossessionCooltimeMultiplier must be at most {MimicTuningResolver.MaxCooltimeMultiplier}; resetting.");
                ModConfig.MimicPossessionCooltimeMultiplier.Value = MimicTuningResolver.MaxCooltimeMultiplier;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(ModConfig.MimicPossessionCooltimeMultiplier);
            ModConfig.NotifyChanged(ModConfig.MimicPossessionCooltimeMultiplier);
        }
    }
}
