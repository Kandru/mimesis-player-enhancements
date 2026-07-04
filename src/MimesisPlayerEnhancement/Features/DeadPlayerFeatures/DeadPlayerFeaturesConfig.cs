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
                "Master toggle for dead-spectator enhancements (mimic possession tuning, monster spectate, and experimental phone ring).");

            ModConfig.EnableMimicPossessionTuning = ModConfig.CreateTrackedEntry(_category,
                "EnableMimicPossessionTuning",
                false,
                "Enable Mimic Possession Tuning",
                "Tune dead-player mimic possession speak duration and cooldown on the host.");

            ModConfig.RandomizeMimicPossessionDuration = ModConfig.CreateTrackedEntry(_category,
                "RandomizeMimicPossessionDuration",
                false,
                "Randomize Mimic Possession Duration",
                "Roll a random speak window per E-possession between min and max seconds below. Host only.");

            ModConfig.MimicPossessionMinTimeSeconds = ModConfig.CreateTrackedEntry(_category,
                "MimicPossessionMinTimeSeconds",
                MimicPossessionResolver.VanillaPossessionDurationSeconds,
                "Mimic Possession Min Time (seconds)",
                "Minimum rolled speak duration in seconds (vanilla is 12). Host only.");

            ModConfig.MimicPossessionMaxTimeSeconds = ModConfig.CreateTrackedEntry(_category,
                "MimicPossessionMaxTimeSeconds",
                MimicPossessionResolver.VanillaPossessionDurationSeconds,
                "Mimic Possession Max Time (seconds)",
                "Maximum rolled speak duration in seconds (vanilla is 12). Host only.");

            ModConfig.MimicPossessionCooltimeMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MimicPossessionCooltimeMultiplier",
                1f,
                "Mimic Possession Cooltime Multiplier",
                "Multiplier for wait time after mimic possession before the next E-possession (1 = vanilla). Host only.");

            ModConfig.EnableDeadPlayerPhoneRing = ModConfig.CreateTrackedEntry(_category,
                "EnableDeadPlayerPhoneRing",
                false,
                "Enable Dead Player Phone Ring (Experimental)",
                "Experimental. Allow dead spectators with this mod to ring nearby idle phones. Requires the mod on the host and on each dead player who participates.");

            ModConfig.DeadPlayerPhoneMaxDistanceMeters = ModConfig.CreateTrackedEntry(_category,
                "DeadPlayerPhoneMaxDistanceMeters",
                DeadPlayerPhoneResolver.GetVanillaPossessionDistanceMeters(),
                "Dead Player Phone Max Distance (meters)",
                "Experimental. Max distance from the spectated player to the phone (defaults to vanilla mimic possession distance).");

            ModConfig.DeadPlayerPhoneMaxLookAngleDegrees = ModConfig.CreateTrackedEntry(_category,
                "DeadPlayerPhoneMaxLookAngleDegrees",
                DeadPlayerPhoneResolver.DefaultLookAngleDegrees,
                "Dead Player Phone Max Look Angle (degrees)",
                "Experimental. Max horizontal camera angle toward the phone or mimic (vanilla default is 45).");

            ModConfig.DeadPlayerPhoneMaxRingTimeSeconds = ModConfig.CreateTrackedEntry(_category,
                "DeadPlayerPhoneMaxRingTimeSeconds",
                DeadPlayerPhoneResolver.DefaultMaxRingTimeSeconds,
                "Dead Player Phone Max Ring Time (seconds)",
                "Experimental. How long a dead-player-initiated ring lasts before the phone returns to idle if unanswered.");

            ModConfig.RandomizeDeadPlayerPhoneTalkTime = ModConfig.CreateTrackedEntry(_category,
                "RandomizeDeadPlayerPhoneTalkTime",
                false,
                "Randomize Dead Player Phone Talk Time",
                "Experimental. Roll a random talk window after pickup between min and max seconds below. Host only.");

            ModConfig.DeadPlayerPhoneTalkMinTimeSeconds = ModConfig.CreateTrackedEntry(_category,
                "DeadPlayerPhoneTalkMinTimeSeconds",
                MimicPossessionResolver.VanillaPossessionDurationSeconds,
                "Dead Player Phone Talk Min Time (seconds)",
                "Experimental. Minimum rolled talk duration after pickup.");

            ModConfig.DeadPlayerPhoneTalkMaxTimeSeconds = ModConfig.CreateTrackedEntry(_category,
                "DeadPlayerPhoneTalkMaxTimeSeconds",
                MimicPossessionResolver.VanillaPossessionDurationSeconds,
                "Dead Player Phone Talk Max Time (seconds)",
                "Experimental. Maximum rolled talk duration after pickup.");

            ModConfig.DeadPlayerPhoneCooldownSeconds = ModConfig.CreateTrackedEntry(_category,
                "DeadPlayerPhoneCooldownSeconds",
                0f,
                "Dead Player Phone Cooldown (seconds)",
                "Experimental. Wait time after a completed talk before the same dead player can ring again (0 = none).");

            ModConfig.EnableMonsterSpectate = ModConfig.CreateTrackedEntry(_category,
                "EnableMonsterSpectate",
                false,
                "Enable Monster Spectate",
                "Allow dead spectators to cycle spectator camera targets to alive monsters in addition to players. Requires the mod on each dead player who participates.");

            ModConfig.SpectateMonstersAfterPlayers = ModConfig.CreateTrackedEntry(_category,
                "SpectateMonstersAfterPlayers",
                true,
                "Spectate Monsters After Players",
                "When enabled, alive players stay first in the prev/next spectator list and monsters are appended after them.");

            ModConfig.IncludeMimicsInMonsterSpectate = ModConfig.CreateTrackedEntry(_category,
                "IncludeMimicsInMonsterSpectate",
                true,
                "Include Mimics In Monster Spectate",
                "Include mimic monsters in the monster spectator target pool.");

            RefreshResolverCaches();
        }

        internal static void RefreshResolverCaches()
        {
            MimicPossessionResolver.RefreshFromConfigRegistration();
            DeadPlayerPhoneResolver.RefreshFromConfigRegistration();
            MonsterSpectateResolver.RefreshFromConfigRegistration();
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableDeadPlayerFeatures.OnEntryValueChanged.Subscribe((_, _) =>
            {
                ModConfig.NotifyChanged(ModConfig.EnableDeadPlayerFeatures);
                DeadPlayerFeaturesRuntime.RefreshFromConfig();
            });
            ModConfig.EnableMimicPossessionTuning.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableMimicPossessionTuning));
            ModConfig.EnableDeadPlayerPhoneRing.OnEntryValueChanged.Subscribe((_, _) =>
            {
                ModConfig.NotifyChanged(ModConfig.EnableDeadPlayerPhoneRing);
                DeadPlayerFeaturesRuntime.RefreshFromConfig();
            });
            ModConfig.RandomizeMimicPossessionDuration.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.RandomizeMimicPossessionDuration));
            ModConfig.MimicPossessionMinTimeSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnMimicPossessionDurationSecondsChanged(logger, value, ModConfig.MimicPossessionMinTimeSeconds));
            ModConfig.MimicPossessionMaxTimeSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnMimicPossessionDurationSecondsChanged(logger, value, ModConfig.MimicPossessionMaxTimeSeconds));
            ModConfig.MimicPossessionCooltimeMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnMimicPossessionCooltimeMultiplierChanged(logger, value));
            ModConfig.DeadPlayerPhoneMaxDistanceMeters.OnEntryValueChanged.Subscribe((_, value) =>
                OnPhoneDistanceChanged(logger, value));
            ModConfig.DeadPlayerPhoneMaxLookAngleDegrees.OnEntryValueChanged.Subscribe((_, value) =>
                OnPhoneAngleChanged(logger, value));
            ModConfig.DeadPlayerPhoneMaxRingTimeSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnPhoneDurationChanged(logger, value, ModConfig.DeadPlayerPhoneMaxRingTimeSeconds));
            ModConfig.RandomizeDeadPlayerPhoneTalkTime.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.RandomizeDeadPlayerPhoneTalkTime));
            ModConfig.DeadPlayerPhoneTalkMinTimeSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnPhoneDurationChanged(logger, value, ModConfig.DeadPlayerPhoneTalkMinTimeSeconds));
            ModConfig.DeadPlayerPhoneTalkMaxTimeSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnPhoneDurationChanged(logger, value, ModConfig.DeadPlayerPhoneTalkMaxTimeSeconds));
            ModConfig.DeadPlayerPhoneCooldownSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnPhoneDurationChanged(logger, value, ModConfig.DeadPlayerPhoneCooldownSeconds));
            ModConfig.EnableMonsterSpectate.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableMonsterSpectate));
            ModConfig.SpectateMonstersAfterPlayers.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.SpectateMonstersAfterPlayers));
            ModConfig.IncludeMimicsInMonsterSpectate.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.IncludeMimicsInMonsterSpectate));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.MimicPossessionMinTimeSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MimicPossessionMaxTimeSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MimicPossessionCooltimeMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.DeadPlayerPhoneMaxDistanceMeters);
            ModConfig.TrackFloatEntry(ModConfig.DeadPlayerPhoneMaxLookAngleDegrees);
            ModConfig.TrackFloatEntry(ModConfig.DeadPlayerPhoneMaxRingTimeSeconds);
            ModConfig.TrackFloatEntry(ModConfig.DeadPlayerPhoneTalkMinTimeSeconds);
            ModConfig.TrackFloatEntry(ModConfig.DeadPlayerPhoneTalkMaxTimeSeconds);
            ModConfig.TrackFloatEntry(ModConfig.DeadPlayerPhoneCooldownSeconds);
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

        private static void OnPhoneDistanceChanged(MelonLogger.Instance logger, float value)
        {
            if (value < DeadPlayerPhoneResolver.MinDistanceMeters)
            {
                logger.Warning(
                    $"DeadPlayerPhoneMaxDistanceMeters must be at least {DeadPlayerPhoneResolver.MinDistanceMeters}; resetting.");
                ModConfig.DeadPlayerPhoneMaxDistanceMeters.Value = DeadPlayerPhoneResolver.MinDistanceMeters;
                return;
            }

            if (value > DeadPlayerPhoneResolver.MaxAllowedDistanceMeters)
            {
                logger.Warning(
                    $"DeadPlayerPhoneMaxDistanceMeters must be at most {DeadPlayerPhoneResolver.MaxAllowedDistanceMeters}; resetting.");
                ModConfig.DeadPlayerPhoneMaxDistanceMeters.Value = DeadPlayerPhoneResolver.MaxAllowedDistanceMeters;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(ModConfig.DeadPlayerPhoneMaxDistanceMeters);
            ModConfig.NotifyChanged(ModConfig.DeadPlayerPhoneMaxDistanceMeters);
        }

        private static void OnPhoneAngleChanged(MelonLogger.Instance logger, float value)
        {
            if (value is <= 0f or > 180f)
            {
                logger.Warning("DeadPlayerPhoneMaxLookAngleDegrees must be between 0 and 180; resetting.");
                ModConfig.DeadPlayerPhoneMaxLookAngleDegrees.Value = DeadPlayerPhoneResolver.DefaultLookAngleDegrees;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(ModConfig.DeadPlayerPhoneMaxLookAngleDegrees);
            ModConfig.NotifyChanged(ModConfig.DeadPlayerPhoneMaxLookAngleDegrees);
        }

        private static void OnPhoneDurationChanged(
            MelonLogger.Instance logger,
            float value,
            MelonPreferences_Entry<float> entry)
        {
            if (value < 0f)
            {
                logger.Warning($"{entry.Identifier} must be >= 0; resetting.");
                entry.Value = 0f;
                return;
            }

            if (value > 0f && value < DeadPlayerPhoneResolver.MinDurationSeconds)
            {
                logger.Warning(
                    $"{entry.Identifier} must be at least {DeadPlayerPhoneResolver.MinDurationSeconds} or 0; resetting.");
                entry.Value = DeadPlayerPhoneResolver.MinDurationSeconds;
                return;
            }

            if (value > DeadPlayerPhoneResolver.MaxDurationSeconds)
            {
                logger.Warning(
                    $"{entry.Identifier} must be at most {DeadPlayerPhoneResolver.MaxDurationSeconds}; resetting.");
                entry.Value = DeadPlayerPhoneResolver.MaxDurationSeconds;
                return;
            }

            float min = ModConfig.DeadPlayerPhoneTalkMinTimeSeconds.Value;
            float max = ModConfig.DeadPlayerPhoneTalkMaxTimeSeconds.Value;
            if (entry == ModConfig.DeadPlayerPhoneTalkMinTimeSeconds && max < min)
            {
                ModConfig.DeadPlayerPhoneTalkMaxTimeSeconds.Value = min;
            }
            else if (entry == ModConfig.DeadPlayerPhoneTalkMaxTimeSeconds && max < min)
            {
                entry.Value = min;
            }

            ModConfigFloatHelper.SanitizeEntry(entry);
            ModConfig.NotifyChanged(entry);
        }
    }
}
