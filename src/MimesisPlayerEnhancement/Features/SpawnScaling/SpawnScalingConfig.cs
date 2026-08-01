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

            ModConfig.BossSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "BossSpawnMultiplier",
                1f);

            ModConfig.BossSpawnPerPlayerMultiplier = ModConfig.CreateTrackedEntry(_category,
                "BossSpawnPerPlayerMultiplier",
                ScalingMath.DefaultPerPlayerMultiplier);

            ModConfig.JakoSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "JakoSpawnMultiplier",
                1f);

            ModConfig.JakoSpawnPerPlayerMultiplier = ModConfig.CreateTrackedEntry(_category,
                "JakoSpawnPerPlayerMultiplier",
                ScalingMath.DefaultPerPlayerMultiplier);

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

            ModConfig.AmbientMonsterWaveMode = ModConfig.CreateTrackedEntry(_category,
                "AmbientMonsterWaveMode",
                "Vanilla");

            ModConfig.AmbientMonsterWaveInitialDelaySeconds = ModConfig.CreateTrackedEntry(_category,
                "AmbientMonsterWaveInitialDelaySeconds",
                60f);

            ModConfig.AmbientMonsterWaveInitialDelayMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "AmbientMonsterWaveInitialDelayMinSeconds",
                30f);

            ModConfig.AmbientMonsterWaveInitialDelayMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "AmbientMonsterWaveInitialDelayMaxSeconds",
                90f);

            ModConfig.AmbientMonsterWaveIntervalSeconds = ModConfig.CreateTrackedEntry(_category,
                "AmbientMonsterWaveIntervalSeconds",
                30f);

            ModConfig.AmbientMonsterWaveIntervalMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "AmbientMonsterWaveIntervalMinSeconds",
                20f);

            ModConfig.AmbientMonsterWaveIntervalMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "AmbientMonsterWaveIntervalMaxSeconds",
                45f);

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
            ModConfig.BossSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.BossSpawnMultiplier));
            ModConfig.BossSpawnPerPlayerMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.BossSpawnPerPlayerMultiplier));
            ModConfig.JakoSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.JakoSpawnMultiplier));
            ModConfig.JakoSpawnPerPlayerMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.JakoSpawnPerPlayerMultiplier));
            ModConfig.SpecialSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.SpecialSpawnMultiplier));
            ModConfig.SpecialSpawnPerPlayerMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.SpecialSpawnPerPlayerMultiplier));
            ModConfig.TrapSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.TrapSpawnMultiplier));
            ModConfig.TrapSpawnPerPlayerMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.TrapSpawnPerPlayerMultiplier));
            ModConfig.OtherSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.OtherSpawnMultiplier));
            ModConfig.OtherSpawnPerPlayerMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.OtherSpawnPerPlayerMultiplier));

            ModConfig.BonusEncounterDelayMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnBonusEncounterDelayChanged(logger, value, ModConfig.BonusEncounterDelayMinSeconds));
            ModConfig.BonusEncounterDelayMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnBonusEncounterDelayChanged(logger, value, ModConfig.BonusEncounterDelayMaxSeconds));
            ModConfig.BonusEncounterMinPlayerDistanceMeters.OnEntryValueChanged.Subscribe((_, value) => OnBonusEncounterMinPlayerDistanceChanged(logger, value));
            ModConfig.TrapRespawnMode.OnEntryValueChanged.Subscribe((_, value) => OnTrapRespawnModeChanged(logger, value));
            ModConfig.TrapRespawnDelaySeconds.OnEntryValueChanged.Subscribe((_, value) => OnTrapRespawnDelayChanged(logger, value, ModConfig.TrapRespawnDelaySeconds));
            ModConfig.TrapRespawnDelayMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnTrapRespawnDelayRangeChanged(logger, value, ModConfig.TrapRespawnDelayMinSeconds, ModConfig.TrapRespawnDelayMaxSeconds));
            ModConfig.TrapRespawnDelayMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnTrapRespawnDelayRangeChanged(logger, value, ModConfig.TrapRespawnDelayMinSeconds, ModConfig.TrapRespawnDelayMaxSeconds));
            ModConfig.TrapRespawnMinPlayerDistanceMeters.OnEntryValueChanged.Subscribe((_, value) => OnTrapRespawnMinPlayerDistanceChanged(logger, value));
            ModConfig.AmbientMonsterWaveMode.OnEntryValueChanged.Subscribe((_, value) => OnAmbientMonsterWaveModeChanged(logger, value));
            ModConfig.AmbientMonsterWaveInitialDelaySeconds.OnEntryValueChanged.Subscribe((_, value) => OnAmbientMonsterWaveSecondsChanged(logger, value, ModConfig.AmbientMonsterWaveInitialDelaySeconds));
            ModConfig.AmbientMonsterWaveInitialDelayMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnAmbientMonsterWaveRangeChanged(logger, value, ModConfig.AmbientMonsterWaveInitialDelayMinSeconds, ModConfig.AmbientMonsterWaveInitialDelayMaxSeconds));
            ModConfig.AmbientMonsterWaveInitialDelayMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnAmbientMonsterWaveRangeChanged(logger, value, ModConfig.AmbientMonsterWaveInitialDelayMinSeconds, ModConfig.AmbientMonsterWaveInitialDelayMaxSeconds));
            ModConfig.AmbientMonsterWaveIntervalSeconds.OnEntryValueChanged.Subscribe((_, value) => OnAmbientMonsterWaveSecondsChanged(logger, value, ModConfig.AmbientMonsterWaveIntervalSeconds));
            ModConfig.AmbientMonsterWaveIntervalMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnAmbientMonsterWaveRangeChanged(logger, value, ModConfig.AmbientMonsterWaveIntervalMinSeconds, ModConfig.AmbientMonsterWaveIntervalMaxSeconds));
            ModConfig.AmbientMonsterWaveIntervalMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnAmbientMonsterWaveRangeChanged(logger, value, ModConfig.AmbientMonsterWaveIntervalMinSeconds, ModConfig.AmbientMonsterWaveIntervalMaxSeconds));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.MimicSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.MimicSpawnPerPlayerMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.BossSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.BossSpawnPerPlayerMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.JakoSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.JakoSpawnPerPlayerMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.SpecialSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.SpecialSpawnPerPlayerMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.TrapSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.TrapSpawnPerPlayerMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.TrapRespawnDelaySeconds);
            ModConfig.TrackFloatEntry(ModConfig.TrapRespawnDelayMinSeconds);
            ModConfig.TrackFloatEntry(ModConfig.TrapRespawnDelayMaxSeconds);
            ModConfig.TrackFloatEntry(ModConfig.TrapRespawnMinPlayerDistanceMeters);
            ModConfig.TrackFloatEntry(ModConfig.AmbientMonsterWaveInitialDelaySeconds);
            ModConfig.TrackFloatEntry(ModConfig.AmbientMonsterWaveInitialDelayMinSeconds);
            ModConfig.TrackFloatEntry(ModConfig.AmbientMonsterWaveInitialDelayMaxSeconds);
            ModConfig.TrackFloatEntry(ModConfig.AmbientMonsterWaveIntervalSeconds);
            ModConfig.TrackFloatEntry(ModConfig.AmbientMonsterWaveIntervalMinSeconds);
            ModConfig.TrackFloatEntry(ModConfig.AmbientMonsterWaveIntervalMaxSeconds);
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

        private static void OnAmbientMonsterWaveModeChanged(MelonLogger.Instance logger, string value)
        {
            if (!string.Equals(value, "Vanilla", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "Fixed", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "Random", StringComparison.OrdinalIgnoreCase))
            {
                logger.Warning("AmbientMonsterWaveMode must be Vanilla, Fixed, or Random; resetting to Vanilla.");
                ModConfig.AmbientMonsterWaveMode.Value = "Vanilla";
                return;
            }

            ModConfig.NotifyChanged(ModConfig.AmbientMonsterWaveMode);
        }

        private static void OnAmbientMonsterWaveSecondsChanged(
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

        private static void OnAmbientMonsterWaveRangeChanged(
            MelonLogger.Instance logger,
            float value,
            MelonPreferences_Entry<float> minEntry,
            MelonPreferences_Entry<float> maxEntry)
        {
            OnAmbientMonsterWaveSecondsChanged(logger, value, minEntry);
            OnAmbientMonsterWaveSecondsChanged(logger, maxEntry.Value, maxEntry);

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
