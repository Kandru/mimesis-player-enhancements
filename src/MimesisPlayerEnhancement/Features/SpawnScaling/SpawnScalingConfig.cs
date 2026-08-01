using MelonLoader;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_SpawnScaling] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order (category → entries → validation → floats) is driven by
    /// <see cref="ModConfig.Initialize"/> to keep TOML section/entry order unchanged.
    /// </summary>
    internal static class SpawnScalingConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_SpawnScaling");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableSpawnScaling = ModConfig.CreateTrackedEntry(_category,
                "EnableSpawnScaling",
                false);

            ModConfig.SpawnScalingBaselinePlayerCount = ModConfig.CreateTrackedEntry(_category,
                "SpawnScalingBaselinePlayerCount",
                ScalingMath.VanillaPlayerBaseline);

            ModConfig.MimicSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MimicSpawnMultiplier",
                1f);

            ModConfig.MimicSpawnPerPlayerMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MimicSpawnPerPlayerMultiplier",
                ScalingMath.DefaultPerPlayerMultiplier);

            ModConfig.MimicWaveMode = ModConfig.CreateTrackedEntry(_category,
                "MimicWaveMode",
                "Vanilla");

            ModConfig.MimicWaveInitialDelaySeconds = ModConfig.CreateTrackedEntry(_category,
                "MimicWaveInitialDelaySeconds",
                60f);

            ModConfig.MimicWaveInitialDelayMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "MimicWaveInitialDelayMinSeconds",
                30f);

            ModConfig.MimicWaveInitialDelayMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "MimicWaveInitialDelayMaxSeconds",
                90f);

            ModConfig.MimicWaveIntervalSeconds = ModConfig.CreateTrackedEntry(_category,
                "MimicWaveIntervalSeconds",
                30f);

            ModConfig.MimicWaveIntervalMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "MimicWaveIntervalMinSeconds",
                20f);

            ModConfig.MimicWaveIntervalMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "MimicWaveIntervalMaxSeconds",
                45f);

            ModConfig.BossSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "BossSpawnMultiplier",
                1f);

            ModConfig.BossSpawnPerPlayerMultiplier = ModConfig.CreateTrackedEntry(_category,
                "BossSpawnPerPlayerMultiplier",
                ScalingMath.DefaultPerPlayerMultiplier);

            ModConfig.GruntSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "GruntSpawnMultiplier",
                1f);

            ModConfig.GruntSpawnPerPlayerMultiplier = ModConfig.CreateTrackedEntry(_category,
                "GruntSpawnPerPlayerMultiplier",
                ScalingMath.DefaultPerPlayerMultiplier);

            ModConfig.GruntWaveMode = ModConfig.CreateTrackedEntry(_category,
                "GruntWaveMode",
                "Vanilla");

            ModConfig.GruntWaveInitialDelaySeconds = ModConfig.CreateTrackedEntry(_category,
                "GruntWaveInitialDelaySeconds",
                60f);

            ModConfig.GruntWaveInitialDelayMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "GruntWaveInitialDelayMinSeconds",
                30f);

            ModConfig.GruntWaveInitialDelayMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "GruntWaveInitialDelayMaxSeconds",
                90f);

            ModConfig.GruntWaveIntervalSeconds = ModConfig.CreateTrackedEntry(_category,
                "GruntWaveIntervalSeconds",
                30f);

            ModConfig.GruntWaveIntervalMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "GruntWaveIntervalMinSeconds",
                20f);

            ModConfig.GruntWaveIntervalMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "GruntWaveIntervalMaxSeconds",
                45f);

            ModConfig.SpecialSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "SpecialSpawnMultiplier",
                1f);

            ModConfig.SpecialSpawnPerPlayerMultiplier = ModConfig.CreateTrackedEntry(_category,
                "SpecialSpawnPerPlayerMultiplier",
                ScalingMath.DefaultPerPlayerMultiplier);

            ModConfig.TrapSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "TrapSpawnMultiplier",
                1f);

            ModConfig.TrapSpawnPerPlayerMultiplier = ModConfig.CreateTrackedEntry(_category,
                "TrapSpawnPerPlayerMultiplier",
                ScalingMath.DefaultPerPlayerMultiplier);

            ModConfig.TrapRespawnMode = ModConfig.CreateTrackedEntry(_category,
                "TrapRespawnMode",
                "Vanilla");

            ModConfig.TrapRespawnDelaySeconds = ModConfig.CreateTrackedEntry(_category,
                "TrapRespawnDelaySeconds",
                5f);

            ModConfig.TrapRespawnDelayMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "TrapRespawnDelayMinSeconds",
                5f);

            ModConfig.TrapRespawnDelayMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "TrapRespawnDelayMaxSeconds",
                30f);

            ModConfig.TrapRespawnMinPlayerDistanceMeters = ModConfig.CreateTrackedEntry(_category,
                "TrapRespawnMinPlayerDistanceMeters",
                10f);

            ModConfig.BonusEncounterDelayMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "BonusEncounterDelayMinSeconds",
                5f);

            ModConfig.BonusEncounterDelayMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "BonusEncounterDelayMaxSeconds",
                30f);

            ModConfig.BonusEncounterMinPlayerDistanceMeters = ModConfig.CreateTrackedEntry(_category,
                "BonusEncounterMinPlayerDistanceMeters",
                10f);

            ModConfig.OtherSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "OtherSpawnMultiplier",
                1f);

            ModConfig.OtherSpawnPerPlayerMultiplier = ModConfig.CreateTrackedEntry(_category,
                "OtherSpawnPerPlayerMultiplier",
                ScalingMath.DefaultPerPlayerMultiplier);
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableSpawnScaling.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableSpawnScaling));
            ModConfig.SpawnScalingBaselinePlayerCount.OnEntryValueChanged.Subscribe((_, value) =>
                ModConfig.OnBaselinePlayerCountChanged(logger, value, ModConfig.SpawnScalingBaselinePlayerCount));

            ModConfig.MimicSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.MimicSpawnMultiplier));
            ModConfig.MimicSpawnPerPlayerMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.MimicSpawnPerPlayerMultiplier));
            ModConfig.MimicWaveMode.OnEntryValueChanged.Subscribe((_, value) => OnWaveModeChanged(logger, value, ModConfig.MimicWaveMode));
            ModConfig.MimicWaveInitialDelaySeconds.OnEntryValueChanged.Subscribe((_, value) => OnWaveSecondsChanged(logger, value, ModConfig.MimicWaveInitialDelaySeconds));
            ModConfig.MimicWaveInitialDelayMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnWaveRangeChanged(logger, value, ModConfig.MimicWaveInitialDelayMinSeconds, ModConfig.MimicWaveInitialDelayMaxSeconds));
            ModConfig.MimicWaveInitialDelayMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnWaveRangeChanged(logger, value, ModConfig.MimicWaveInitialDelayMinSeconds, ModConfig.MimicWaveInitialDelayMaxSeconds));
            ModConfig.MimicWaveIntervalSeconds.OnEntryValueChanged.Subscribe((_, value) => OnWaveSecondsChanged(logger, value, ModConfig.MimicWaveIntervalSeconds));
            ModConfig.MimicWaveIntervalMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnWaveRangeChanged(logger, value, ModConfig.MimicWaveIntervalMinSeconds, ModConfig.MimicWaveIntervalMaxSeconds));
            ModConfig.MimicWaveIntervalMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnWaveRangeChanged(logger, value, ModConfig.MimicWaveIntervalMinSeconds, ModConfig.MimicWaveIntervalMaxSeconds));

            ModConfig.BossSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.BossSpawnMultiplier));
            ModConfig.BossSpawnPerPlayerMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.BossSpawnPerPlayerMultiplier));

            ModConfig.GruntSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.GruntSpawnMultiplier));
            ModConfig.GruntSpawnPerPlayerMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.GruntSpawnPerPlayerMultiplier));
            ModConfig.GruntWaveMode.OnEntryValueChanged.Subscribe((_, value) => OnWaveModeChanged(logger, value, ModConfig.GruntWaveMode));
            ModConfig.GruntWaveInitialDelaySeconds.OnEntryValueChanged.Subscribe((_, value) => OnWaveSecondsChanged(logger, value, ModConfig.GruntWaveInitialDelaySeconds));
            ModConfig.GruntWaveInitialDelayMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnWaveRangeChanged(logger, value, ModConfig.GruntWaveInitialDelayMinSeconds, ModConfig.GruntWaveInitialDelayMaxSeconds));
            ModConfig.GruntWaveInitialDelayMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnWaveRangeChanged(logger, value, ModConfig.GruntWaveInitialDelayMinSeconds, ModConfig.GruntWaveInitialDelayMaxSeconds));
            ModConfig.GruntWaveIntervalSeconds.OnEntryValueChanged.Subscribe((_, value) => OnWaveSecondsChanged(logger, value, ModConfig.GruntWaveIntervalSeconds));
            ModConfig.GruntWaveIntervalMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnWaveRangeChanged(logger, value, ModConfig.GruntWaveIntervalMinSeconds, ModConfig.GruntWaveIntervalMaxSeconds));
            ModConfig.GruntWaveIntervalMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnWaveRangeChanged(logger, value, ModConfig.GruntWaveIntervalMinSeconds, ModConfig.GruntWaveIntervalMaxSeconds));

            ModConfig.SpecialSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.SpecialSpawnMultiplier));
            ModConfig.SpecialSpawnPerPlayerMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.SpecialSpawnPerPlayerMultiplier));
            ModConfig.TrapSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.TrapSpawnMultiplier));
            ModConfig.TrapSpawnPerPlayerMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.TrapSpawnPerPlayerMultiplier));
            ModConfig.TrapRespawnMode.OnEntryValueChanged.Subscribe((_, value) => OnTrapRespawnModeChanged(logger, value));
            ModConfig.TrapRespawnDelaySeconds.OnEntryValueChanged.Subscribe((_, value) => OnTrapRespawnDelayChanged(logger, value, ModConfig.TrapRespawnDelaySeconds));
            ModConfig.TrapRespawnDelayMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnTrapRespawnDelayRangeChanged(logger, value, ModConfig.TrapRespawnDelayMinSeconds, ModConfig.TrapRespawnDelayMaxSeconds));
            ModConfig.TrapRespawnDelayMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnTrapRespawnDelayRangeChanged(logger, value, ModConfig.TrapRespawnDelayMinSeconds, ModConfig.TrapRespawnDelayMaxSeconds));
            ModConfig.TrapRespawnMinPlayerDistanceMeters.OnEntryValueChanged.Subscribe((_, value) => OnTrapRespawnMinPlayerDistanceChanged(logger, value));
            ModConfig.BonusEncounterDelayMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnBonusEncounterDelayChanged(logger, value, ModConfig.BonusEncounterDelayMinSeconds));
            ModConfig.BonusEncounterDelayMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnBonusEncounterDelayChanged(logger, value, ModConfig.BonusEncounterDelayMaxSeconds));
            ModConfig.BonusEncounterMinPlayerDistanceMeters.OnEntryValueChanged.Subscribe((_, value) => OnBonusEncounterMinPlayerDistanceChanged(logger, value));
            ModConfig.OtherSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.OtherSpawnMultiplier));
            ModConfig.OtherSpawnPerPlayerMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.OtherSpawnPerPlayerMultiplier));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.MimicSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.MimicSpawnPerPlayerMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.MimicWaveInitialDelaySeconds);
            ModConfig.TrackFloatEntry(ModConfig.MimicWaveInitialDelayMinSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MimicWaveInitialDelayMaxSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MimicWaveIntervalSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MimicWaveIntervalMinSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MimicWaveIntervalMaxSeconds);
            ModConfig.TrackFloatEntry(ModConfig.BossSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.BossSpawnPerPlayerMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.GruntSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.GruntSpawnPerPlayerMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.GruntWaveInitialDelaySeconds);
            ModConfig.TrackFloatEntry(ModConfig.GruntWaveInitialDelayMinSeconds);
            ModConfig.TrackFloatEntry(ModConfig.GruntWaveInitialDelayMaxSeconds);
            ModConfig.TrackFloatEntry(ModConfig.GruntWaveIntervalSeconds);
            ModConfig.TrackFloatEntry(ModConfig.GruntWaveIntervalMinSeconds);
            ModConfig.TrackFloatEntry(ModConfig.GruntWaveIntervalMaxSeconds);
            ModConfig.TrackFloatEntry(ModConfig.SpecialSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.SpecialSpawnPerPlayerMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.TrapSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.TrapSpawnPerPlayerMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.TrapRespawnDelaySeconds);
            ModConfig.TrackFloatEntry(ModConfig.TrapRespawnDelayMinSeconds);
            ModConfig.TrackFloatEntry(ModConfig.TrapRespawnDelayMaxSeconds);
            ModConfig.TrackFloatEntry(ModConfig.TrapRespawnMinPlayerDistanceMeters);
            ModConfig.TrackFloatEntry(ModConfig.BonusEncounterDelayMinSeconds);
            ModConfig.TrackFloatEntry(ModConfig.BonusEncounterDelayMaxSeconds);
            ModConfig.TrackFloatEntry(ModConfig.BonusEncounterMinPlayerDistanceMeters);
            ModConfig.TrackFloatEntry(ModConfig.OtherSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.OtherSpawnPerPlayerMultiplier);
        }

        private static void OnBonusEncounterDelayChanged(MelonLogger.Instance logger, float value, MelonPreferences_Entry<float> entry)
        {
            if (value < 0f)
            {
                logger.Warning($"{entry.Identifier} must be >= 0; resetting to 0.");
                entry.Value = 0f;
                return;
            }

            float min = ModConfig.BonusEncounterDelayMinSeconds.Value;
            float max = ModConfig.BonusEncounterDelayMaxSeconds.Value;
            if (max < min)
            {
                logger.Warning("BonusEncounterDelayMaxSeconds must be >= BonusEncounterDelayMinSeconds; syncing max to min.");
                ModConfig.BonusEncounterDelayMaxSeconds.Value = min;
            }

            ModConfigFloatHelper.SanitizeEntry(entry);
            ModConfig.NotifyChanged(entry);
        }

        private static void OnBonusEncounterMinPlayerDistanceChanged(MelonLogger.Instance logger, float value)
        {
            if (value < 0f)
            {
                logger.Warning("BonusEncounterMinPlayerDistanceMeters must be >= 0; resetting to 0.");
                ModConfig.BonusEncounterMinPlayerDistanceMeters.Value = 0f;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(ModConfig.BonusEncounterMinPlayerDistanceMeters);
            ModConfig.NotifyChanged(ModConfig.BonusEncounterMinPlayerDistanceMeters);
        }

        private static void OnTrapRespawnMinPlayerDistanceChanged(MelonLogger.Instance logger, float value)
        {
            if (value < 0f)
            {
                logger.Warning("TrapRespawnMinPlayerDistanceMeters must be >= 0; resetting to 0.");
                ModConfig.TrapRespawnMinPlayerDistanceMeters.Value = 0f;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(ModConfig.TrapRespawnMinPlayerDistanceMeters);
            ModConfig.NotifyChanged(ModConfig.TrapRespawnMinPlayerDistanceMeters);
        }

        private static void OnTrapRespawnModeChanged(MelonLogger.Instance logger, string value)
        {
            if (!string.Equals(value, "Vanilla", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "Fixed", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "Random", StringComparison.OrdinalIgnoreCase))
            {
                logger.Warning("TrapRespawnMode must be Vanilla, Fixed, or Random; resetting to Vanilla.");
                ModConfig.TrapRespawnMode.Value = "Vanilla";
                return;
            }

            ModConfig.NotifyChanged(ModConfig.TrapRespawnMode);
        }

        private static void OnTrapRespawnDelayChanged(MelonLogger.Instance logger, float value, MelonPreferences_Entry<float> entry)
        {
            if (value < 0f)
            {
                logger.Warning($"{entry.Identifier} must be >= 0; resetting to 0.");
                entry.Value = 0f;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(entry);
            ModConfig.NotifyChanged(entry);
        }

        private static void OnTrapRespawnDelayRangeChanged(
            MelonLogger.Instance logger,
            float value,
            MelonPreferences_Entry<float> minEntry,
            MelonPreferences_Entry<float> maxEntry)
        {
            OnTrapRespawnDelayChanged(logger, value, minEntry);
            OnTrapRespawnDelayChanged(logger, maxEntry.Value, maxEntry);

            float min = minEntry.Value;
            float max = maxEntry.Value;
            if (max < min)
            {
                logger.Warning($"{maxEntry.Identifier} must be >= {minEntry.Identifier}; syncing max to min.");
                maxEntry.Value = min;
            }

            ModConfig.NotifyChanged(minEntry);
            ModConfig.NotifyChanged(maxEntry);
        }

        private static void OnWaveModeChanged(
            MelonLogger.Instance logger,
            string value,
            MelonPreferences_Entry<string> entry)
        {
            if (!string.Equals(value, "Vanilla", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "Fixed", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "Random", StringComparison.OrdinalIgnoreCase))
            {
                logger.Warning($"{entry.Identifier} must be Vanilla, Fixed, or Random; resetting to Vanilla.");
                entry.Value = "Vanilla";
                return;
            }

            ModConfig.NotifyChanged(entry);
        }

        private static void OnWaveSecondsChanged(
            MelonLogger.Instance logger,
            float value,
            MelonPreferences_Entry<float> entry)
        {
            if (value < 0f)
            {
                logger.Warning($"{entry.Identifier} must be >= 0; resetting to 0.");
                entry.Value = 0f;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(entry);
            ModConfig.NotifyChanged(entry);
        }

        private static void OnWaveRangeChanged(
            MelonLogger.Instance logger,
            float value,
            MelonPreferences_Entry<float> minEntry,
            MelonPreferences_Entry<float> maxEntry)
        {
            OnWaveSecondsChanged(logger, value, minEntry);
            OnWaveSecondsChanged(logger, maxEntry.Value, maxEntry);

            float min = minEntry.Value;
            float max = maxEntry.Value;
            if (max < min)
            {
                logger.Warning($"{maxEntry.Identifier} must be >= {minEntry.Identifier}; syncing max to min.");
                maxEntry.Value = min;
            }

            ModConfig.NotifyChanged(minEntry);
            ModConfig.NotifyChanged(maxEntry);
        }
    }
}
