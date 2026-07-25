using MelonLoader;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_SpawnScaling] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order (category → entries → validation → floats → migration) is driven by
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

            ModConfig.SpawnScalingPlayerCountScaleRate = ModConfig.CreateTrackedEntry(_category,
                "SpawnScalingPlayerCountScaleRate",
                ScalingMath.DefaultPlayerCountScaleRate);

            ModConfig.AutoScaleMimicSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleMimicSpawnsByPlayerCount",
                true);

            ModConfig.MimicSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MimicSpawnMultiplier",
                1f);

            ModConfig.AutoScaleBossSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleBossSpawnsByPlayerCount",
                true);

            ModConfig.BossSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "BossSpawnMultiplier",
                1f);

            ModConfig.AutoScaleJakoSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleJakoSpawnsByPlayerCount",
                true);

            ModConfig.JakoSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "JakoSpawnMultiplier",
                1f);

            ModConfig.AutoScaleSpecialSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleSpecialSpawnsByPlayerCount",
                true);

            ModConfig.SpecialSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "SpecialSpawnMultiplier",
                1f);

            ModConfig.AutoScaleTrapSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleTrapSpawnsByPlayerCount",
                true);

            ModConfig.TrapSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "TrapSpawnMultiplier",
                1f);

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

            ModConfig.AutoScaleOtherSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleOtherSpawnsByPlayerCount",
                true);

            ModConfig.OtherSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "OtherSpawnMultiplier",
                1f);
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableSpawnScaling.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.EnableSpawnScaling));
            ModConfig.SpawnScalingPlayerCountScaleRate.OnEntryValueChanged.Subscribe((_, value) =>
                ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.SpawnScalingPlayerCountScaleRate));
            ModConfig.AutoScaleMimicSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleMimicSpawnsByPlayerCount));
            ModConfig.AutoScaleBossSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleBossSpawnsByPlayerCount));
            ModConfig.AutoScaleJakoSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleJakoSpawnsByPlayerCount));
            ModConfig.AutoScaleSpecialSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleSpecialSpawnsByPlayerCount));
            ModConfig.AutoScaleTrapSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleTrapSpawnsByPlayerCount));
            ModConfig.BonusEncounterDelayMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnBonusEncounterDelayChanged(logger, value, ModConfig.BonusEncounterDelayMinSeconds));
            ModConfig.BonusEncounterDelayMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnBonusEncounterDelayChanged(logger, value, ModConfig.BonusEncounterDelayMaxSeconds));
            ModConfig.BonusEncounterMinPlayerDistanceMeters.OnEntryValueChanged.Subscribe((_, value) => OnBonusEncounterMinPlayerDistanceChanged(logger, value));
            ModConfig.AutoScaleOtherSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleOtherSpawnsByPlayerCount));

            ModConfig.MimicSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.MimicSpawnMultiplier));
            ModConfig.BossSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.BossSpawnMultiplier));
            ModConfig.JakoSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.JakoSpawnMultiplier));
            ModConfig.SpecialSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.SpecialSpawnMultiplier));
            ModConfig.TrapSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.TrapSpawnMultiplier));
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
            ModConfig.OtherSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.OtherSpawnMultiplier));
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.SpawnScalingPlayerCountScaleRate);
            ModConfig.TrackFloatEntry(ModConfig.MimicSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.BossSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.JakoSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.SpecialSpawnMultiplier);
            ModConfig.TrackFloatEntry(ModConfig.TrapSpawnMultiplier);
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
