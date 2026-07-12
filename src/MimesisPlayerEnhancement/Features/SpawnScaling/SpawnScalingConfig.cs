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

            ModConfig.PeriodicSpawnWaitMode = ModConfig.CreateTrackedEntry(_category,
                "PeriodicSpawnWaitMode",
                "Vanilla");

            ModConfig.InitialPeriodicSpawnWaitSeconds = ModConfig.CreateTrackedEntry(_category,
                "InitialPeriodicSpawnWaitSeconds",
                60f);

            ModConfig.InitialPeriodicSpawnWaitMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "InitialPeriodicSpawnWaitMinSeconds",
                30f);

            ModConfig.InitialPeriodicSpawnWaitMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "InitialPeriodicSpawnWaitMaxSeconds",
                90f);

            ModConfig.PeriodicSpawnIntervalSeconds = ModConfig.CreateTrackedEntry(_category,
                "PeriodicSpawnIntervalSeconds",
                30f);

            ModConfig.PeriodicSpawnIntervalMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "PeriodicSpawnIntervalMinSeconds",
                20f);

            ModConfig.PeriodicSpawnIntervalMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "PeriodicSpawnIntervalMaxSeconds",
                45f);

            ModConfig.MapPlacedEncounterDelayMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "MapPlacedEncounterDelayMinSeconds",
                5f);

            ModConfig.MapPlacedEncounterDelayMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "MapPlacedEncounterDelayMaxSeconds",
                30f);

            ModConfig.MapPlacedEncounterMinPlayerDistanceMeters = ModConfig.CreateTrackedEntry(_category,
                "MapPlacedEncounterMinPlayerDistanceMeters",
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
            ModConfig.MapPlacedEncounterDelayMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnMapPlacedEncounterDelayChanged(logger, value, ModConfig.MapPlacedEncounterDelayMinSeconds));
            ModConfig.MapPlacedEncounterDelayMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnMapPlacedEncounterDelayChanged(logger, value, ModConfig.MapPlacedEncounterDelayMaxSeconds));
            ModConfig.MapPlacedEncounterMinPlayerDistanceMeters.OnEntryValueChanged.Subscribe((_, value) => OnMapPlacedEncounterMinPlayerDistanceChanged(logger, value));
            ModConfig.AutoScaleOtherSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(ModConfig.AutoScaleOtherSpawnsByPlayerCount));

            ModConfig.MimicSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.MimicSpawnMultiplier));
            ModConfig.BossSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.BossSpawnMultiplier));
            ModConfig.JakoSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.JakoSpawnMultiplier));
            ModConfig.SpecialSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.SpecialSpawnMultiplier));
            ModConfig.TrapSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => ModConfig.OnSpawnMultiplierChanged(logger, value, ModConfig.TrapSpawnMultiplier));
            ModConfig.PeriodicSpawnWaitMode.OnEntryValueChanged.Subscribe((_, value) => OnPeriodicSpawnWaitModeChanged(logger, value));
            ModConfig.InitialPeriodicSpawnWaitSeconds.OnEntryValueChanged.Subscribe((_, value) => OnPeriodicSpawnWaitSecondsChanged(logger, value, ModConfig.InitialPeriodicSpawnWaitSeconds));
            ModConfig.InitialPeriodicSpawnWaitMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnPeriodicSpawnWaitRangeChanged(logger, value, ModConfig.InitialPeriodicSpawnWaitMinSeconds, ModConfig.InitialPeriodicSpawnWaitMaxSeconds));
            ModConfig.InitialPeriodicSpawnWaitMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnPeriodicSpawnWaitRangeChanged(logger, value, ModConfig.InitialPeriodicSpawnWaitMinSeconds, ModConfig.InitialPeriodicSpawnWaitMaxSeconds));
            ModConfig.PeriodicSpawnIntervalSeconds.OnEntryValueChanged.Subscribe((_, value) => OnPeriodicSpawnWaitSecondsChanged(logger, value, ModConfig.PeriodicSpawnIntervalSeconds));
            ModConfig.PeriodicSpawnIntervalMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnPeriodicSpawnWaitRangeChanged(logger, value, ModConfig.PeriodicSpawnIntervalMinSeconds, ModConfig.PeriodicSpawnIntervalMaxSeconds));
            ModConfig.PeriodicSpawnIntervalMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnPeriodicSpawnWaitRangeChanged(logger, value, ModConfig.PeriodicSpawnIntervalMinSeconds, ModConfig.PeriodicSpawnIntervalMaxSeconds));
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
            ModConfig.TrackFloatEntry(ModConfig.InitialPeriodicSpawnWaitSeconds);
            ModConfig.TrackFloatEntry(ModConfig.InitialPeriodicSpawnWaitMinSeconds);
            ModConfig.TrackFloatEntry(ModConfig.InitialPeriodicSpawnWaitMaxSeconds);
            ModConfig.TrackFloatEntry(ModConfig.PeriodicSpawnIntervalSeconds);
            ModConfig.TrackFloatEntry(ModConfig.PeriodicSpawnIntervalMinSeconds);
            ModConfig.TrackFloatEntry(ModConfig.PeriodicSpawnIntervalMaxSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MapPlacedEncounterDelayMinSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MapPlacedEncounterDelayMaxSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MapPlacedEncounterMinPlayerDistanceMeters);
            ModConfig.TrackFloatEntry(ModConfig.OtherSpawnMultiplier);
        }

        private static void OnMapPlacedEncounterDelayChanged(MelonLogger.Instance logger, float value, MelonPreferences_Entry<float> entry)
        {
            if (value < 0f)
            {
                logger.Warning($"{entry.Identifier} must be >= 0; resetting to 0.");
                entry.Value = 0f;
                return;
            }

            float min = ModConfig.MapPlacedEncounterDelayMinSeconds.Value;
            float max = ModConfig.MapPlacedEncounterDelayMaxSeconds.Value;
            if (max < min)
            {
                logger.Warning("MapPlacedEncounterDelayMaxSeconds must be >= MapPlacedEncounterDelayMinSeconds; syncing max to min.");
                ModConfig.MapPlacedEncounterDelayMaxSeconds.Value = min;
            }

            ModConfigFloatHelper.SanitizeEntry(entry);
            ModConfig.NotifyChanged(entry);
        }

        private static void OnMapPlacedEncounterMinPlayerDistanceChanged(MelonLogger.Instance logger, float value)
        {
            if (value < 0f)
            {
                logger.Warning("MapPlacedEncounterMinPlayerDistanceMeters must be >= 0; resetting to 0.");
                ModConfig.MapPlacedEncounterMinPlayerDistanceMeters.Value = 0f;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(ModConfig.MapPlacedEncounterMinPlayerDistanceMeters);
            ModConfig.NotifyChanged(ModConfig.MapPlacedEncounterMinPlayerDistanceMeters);
        }

        private static void OnPeriodicSpawnWaitModeChanged(MelonLogger.Instance logger, string value)
        {
            if (!string.Equals(value, "Vanilla", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "Fixed", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "Random", StringComparison.OrdinalIgnoreCase))
            {
                logger.Warning("PeriodicSpawnWaitMode must be Vanilla, Fixed, or Random; resetting to Vanilla.");
                ModConfig.PeriodicSpawnWaitMode.Value = "Vanilla";
                return;
            }

            ModConfig.NotifyChanged(ModConfig.PeriodicSpawnWaitMode);
        }

        private static void OnPeriodicSpawnWaitSecondsChanged(
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

        private static void OnPeriodicSpawnWaitRangeChanged(
            MelonLogger.Instance logger,
            float value,
            MelonPreferences_Entry<float> minEntry,
            MelonPreferences_Entry<float> maxEntry)
        {
            OnPeriodicSpawnWaitSecondsChanged(logger, value, minEntry);
            OnPeriodicSpawnWaitSecondsChanged(logger, maxEntry.Value, maxEntry);

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
