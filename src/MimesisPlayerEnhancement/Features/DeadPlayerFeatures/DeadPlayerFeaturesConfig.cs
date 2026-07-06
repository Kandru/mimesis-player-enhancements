using MelonLoader;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures
{
    internal static class DeadPlayerFeaturesConfig
    {
        private const string SectionId = "MimesisPlayerEnhancement_DeadPlayerFeatures";
        private const string LegacySectionId = "MimesisPlayerEnhancement_MimicTuning";

        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory(SectionId, "Dead Player Features");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableDeadPlayerFeatures = ModConfig.CreateTrackedEntry(_category,
                "EnableDeadPlayerFeatures",
                false,
                "Enable Dead Player Features",
                "Master toggle for dead-spectator mimic possession tuning (speak duration and cooldown). Host only.");

            ModConfig.EnableMimicPossessionTuning = ModConfig.CreateTrackedEntry(_category,
                "EnableMimicPossessionTuning",
                false,
                "Enable Mimic Possession Tuning",
                "Tune dead-player mimic possession speak duration and cooldown on the host.");

            ModConfig.RandomizeMimicPossessionDuration = ModConfig.CreateTrackedEntry(_category,
                "RandomizeMimicPossessionDuration",
                false,
                "Randomize Mimic Possession Duration",
                "Roll a random speak window per E-possession between min and max seconds below.");

            ModConfig.MimicPossessionMinTimeSeconds = ModConfig.CreateTrackedEntry(_category,
                "MimicPossessionMinTimeSeconds",
                MimicPossessionResolver.VanillaPossessionDurationSeconds,
                "Mimic Possession Min Time (seconds)",
                "Minimum rolled speak duration in seconds (vanilla is 12).");

            ModConfig.MimicPossessionMaxTimeSeconds = ModConfig.CreateTrackedEntry(_category,
                "MimicPossessionMaxTimeSeconds",
                MimicPossessionResolver.VanillaPossessionDurationSeconds,
                "Mimic Possession Max Time (seconds)",
                "Maximum rolled speak duration in seconds (vanilla is 12).");

            ModConfig.MimicPossessionCooltimeMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MimicPossessionCooltimeMultiplier",
                1f,
                "Mimic Possession Cooltime Multiplier",
                "Multiplier for wait time after mimic possession before the next E-possession (1 = vanilla).");

            RefreshResolverCaches();
        }

        internal static void RefreshResolverCaches()
        {
            MimicPossessionResolver.RefreshFromConfigRegistration();
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableDeadPlayerFeatures.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableDeadPlayerFeatures));
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
            ModConfig.TrackFloatEntry(ModConfig.MimicPossessionMinTimeSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MimicPossessionMaxTimeSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MimicPossessionCooltimeMultiplier);
        }

        internal static void MigrateLegacyKeys(MelonLogger.Instance logger)
        {
            bool migrated = false;
            migrated |= TryMigrateLegacySection(logger);
            migrated |= TryMigrateLegacyMimicPossessionTimeMultiplier(
                "MimicPossessionMinTimeMultiplier",
                ModConfig.MimicPossessionMinTimeSeconds);
            migrated |= TryMigrateLegacyMimicPossessionTimeMultiplier(
                "MimicPossessionMaxTimeMultiplier",
                ModConfig.MimicPossessionMaxTimeSeconds);
            migrated |= TryMigrateLegacyEnableMimicTuning();

            if (migrated)
            {
                logger.Msg("Dead Player Features config migrated from legacy Mimic Tuning section.");
            }

            RefreshResolverCaches();
        }

        private static bool TryMigrateLegacySection(MelonLogger.Instance logger)
        {
            MelonPreferences_Category? legacy = MelonPreferences.GetCategory(LegacySectionId);
            if (legacy == null)
            {
                return false;
            }

            bool changed = false;
            changed |= CopyBoolLegacy(legacy, "EnableMimicTuning", ModConfig.EnableDeadPlayerFeatures);
            changed |= CopyBoolLegacy(legacy, "RandomizeMimicPossessionDuration", ModConfig.RandomizeMimicPossessionDuration);
            changed |= CopyFloatLegacy(legacy, "MimicPossessionMinTimeSeconds", ModConfig.MimicPossessionMinTimeSeconds);
            changed |= CopyFloatLegacy(legacy, "MimicPossessionMaxTimeSeconds", ModConfig.MimicPossessionMaxTimeSeconds);
            changed |= CopyFloatLegacy(legacy, "MimicPossessionCooltimeMultiplier", ModConfig.MimicPossessionCooltimeMultiplier);

            if (changed)
            {
                ModConfig.EnableMimicPossessionTuning.Value = ModConfig.EnableDeadPlayerFeatures.Value;
            }

            return changed;
        }

        private static bool TryMigrateLegacyEnableMimicTuning()
        {
            if (_category.GetEntry<bool>("EnableMimicTuning") is not MelonPreferences_Entry<bool> legacyEnable)
            {
                return false;
            }

            ModConfig.EnableDeadPlayerFeatures.Value = legacyEnable.Value;
            ModConfig.EnableMimicPossessionTuning.Value = legacyEnable.Value;
            return true;
        }

        private static bool CopyBoolLegacy(
            MelonPreferences_Category legacy,
            string key,
            MelonPreferences_Entry<bool> target)
        {
            if (legacy.GetEntry<bool>(key) is not MelonPreferences_Entry<bool> entry)
            {
                return false;
            }

            target.Value = entry.Value;
            return true;
        }

        private static bool CopyFloatLegacy(
            MelonPreferences_Category legacy,
            string key,
            MelonPreferences_Entry<float> target)
        {
            if (legacy.GetEntry<float>(key) is not MelonPreferences_Entry<float> entry)
            {
                return false;
            }

            target.Value = entry.Value;
            return true;
        }

        private static bool TryMigrateLegacyMimicPossessionTimeMultiplier(
            string legacyKey,
            MelonPreferences_Entry<float> targetEntry)
        {
            MelonPreferences_Entry<float>? source =
                _category.GetEntry<float>(legacyKey) as MelonPreferences_Entry<float>
                ?? MelonPreferences.GetCategory(LegacySectionId)?.GetEntry<float>(legacyKey)
                    as MelonPreferences_Entry<float>;

            if (source == null)
            {
                return false;
            }

            targetEntry.Value = source.Value * MimicPossessionResolver.VanillaPossessionDurationSeconds;
            return true;
        }

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
