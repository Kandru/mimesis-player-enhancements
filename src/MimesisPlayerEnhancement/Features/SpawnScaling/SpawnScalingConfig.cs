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
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_SpawnScaling", "Spawn Scaling");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableSpawnScaling = ModConfig.CreateTrackedEntry(_category,
                "EnableSpawnScaling",
                false,
                "Enable Spawn Scaling",
                "Scale dungeon monster and trap spawn budgets by type. Host only.");

            ModConfig.SpawnScalingPlayerCountScaleRate = ModConfig.CreateTrackedEntry(_category,
                "SpawnScalingPlayerCountScaleRate",
                ScalingMath.DefaultPlayerCountScaleRate,
                "Spawn Player Count Scale Rate",
                "Extra multiplier per player above 4 when an Auto Scale … by Player Count toggle is enabled (0.10 = +10% per extra player, stacks with per-type multipliers). Minimum is 0.");

            ModConfig.AutoScaleMimicSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleMimicSpawnsByPlayerCount",
                true,
                "Auto Scale Mimic Spawns by Player Count",
                "When enabled, apply Spawn Player Count Scale Rate per player above 4 (stacks with Mimic Spawn Multiplier).");

            ModConfig.MimicSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MimicSpawnMultiplier",
                1f,
                "Mimic Spawn Multiplier",
                "Total mimic spawn budget across the run, including periodic spawns (1 = vanilla, 2 = double).");

            ModConfig.AutoScaleBossSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleBossSpawnsByPlayerCount",
                true,
                "Auto Scale Boss Spawns by Player Count",
                "When enabled, apply Spawn Player Count Scale Rate per player above 4 (stacks with Boss Spawn Multiplier).");

            ModConfig.BossSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "BossSpawnMultiplier",
                1f,
                "Boss Spawn Multiplier",
                "Map-placed bosses: activates unused alternate markers and schedules bonus encounters after kill (1 = vanilla, 2 = double).");

            ModConfig.AutoScaleJakoSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleJakoSpawnsByPlayerCount",
                true,
                "Auto Scale Jako Spawns by Player Count",
                "When enabled, apply Spawn Player Count Scale Rate per player above 4 (stacks with Jako Spawn Multiplier).");

            ModConfig.JakoSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "JakoSpawnMultiplier",
                1f,
                "Jako Spawn Multiplier",
                "Total normal-monster threat budget for ambient dungeon spawns (1 = vanilla, 2 = double).");

            ModConfig.AutoScaleSpecialSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleSpecialSpawnsByPlayerCount",
                true,
                "Auto Scale Special Spawns by Player Count",
                "When enabled, apply Spawn Player Count Scale Rate per player above 4 (stacks with Special Spawn Multiplier).");

            ModConfig.SpecialSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "SpecialSpawnMultiplier",
                1f,
                "Special Spawn Multiplier",
                "Special monster budget for periodic spawns and map-placed specials (1 = vanilla, 2 = double).");

            ModConfig.AutoScaleTrapSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleTrapSpawnsByPlayerCount",
                true,
                "Auto Scale Trap Spawns by Player Count",
                "When enabled, apply Spawn Player Count Scale Rate per player above 4 (stacks with Trap Spawn Multiplier).");

            ModConfig.TrapSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "TrapSpawnMultiplier",
                1f,
                "Trap Spawn Multiplier",
                "Map-placed traps: activates unused alternate markers and schedules bonus encounters after trigger/kill (1 = vanilla, 2 = double).");

            ModConfig.PeriodicSpawnWaitMode = ModConfig.CreateTrackedEntry(_category,
                "PeriodicSpawnWaitMode",
                "Vanilla",
                "Periodic Spawn Wait Mode",
                "Controls initial delay before the first ambient jako/mimic wave and the interval between waves. Vanilla uses dungeon data; Fixed and Random use the second-based keys below.");

            ModConfig.InitialPeriodicSpawnWaitSeconds = ModConfig.CreateTrackedEntry(_category,
                "InitialPeriodicSpawnWaitSeconds",
                60f,
                "Initial Periodic Spawn Wait (seconds)",
                "Fixed mode: seconds after dungeon start before the first ambient jako/mimic spawn wave can fire.");

            ModConfig.InitialPeriodicSpawnWaitMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "InitialPeriodicSpawnWaitMinSeconds",
                30f,
                "Initial Periodic Spawn Wait Min (seconds)",
                "Random mode: shortest initial wait before the first ambient spawn wave.");

            ModConfig.InitialPeriodicSpawnWaitMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "InitialPeriodicSpawnWaitMaxSeconds",
                90f,
                "Initial Periodic Spawn Wait Max (seconds)",
                "Random mode: longest initial wait. Actual wait is picked between min and max.");

            ModConfig.PeriodicSpawnIntervalSeconds = ModConfig.CreateTrackedEntry(_category,
                "PeriodicSpawnIntervalSeconds",
                30f,
                "Periodic Spawn Interval (seconds)",
                "Fixed mode: seconds between subsequent ambient jako/mimic spawn waves.");

            ModConfig.PeriodicSpawnIntervalMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "PeriodicSpawnIntervalMinSeconds",
                20f,
                "Periodic Spawn Interval Min (seconds)",
                "Random mode: shortest interval between ambient spawn waves.");

            ModConfig.PeriodicSpawnIntervalMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "PeriodicSpawnIntervalMaxSeconds",
                45f,
                "Periodic Spawn Interval Max (seconds)",
                "Random mode: longest interval between waves. Actual interval is picked between min and max after each wave.");

            ModConfig.MapPlacedEncounterDelayMinSeconds = ModConfig.CreateTrackedEntry(_category,
                "MapPlacedEncounterDelayMinSeconds",
                5f,
                "Map-Placed Encounter Delay Min (seconds)",
                "Shortest wait after a map-placed enemy, trap, or loot marker is cleared before the next bonus encounter from scaling can appear there.");

            ModConfig.MapPlacedEncounterDelayMaxSeconds = ModConfig.CreateTrackedEntry(_category,
                "MapPlacedEncounterDelayMaxSeconds",
                30f,
                "Map-Placed Encounter Delay Max (seconds)",
                "Longest wait for that random delay. Actual delay is picked between min and max.");

            ModConfig.MapPlacedEncounterMinPlayerDistanceMeters = ModConfig.CreateTrackedEntry(_category,
                "MapPlacedEncounterMinPlayerDistanceMeters",
                10f,
                "Map-Placed Encounter Min Player Distance (m)",
                "After the delay, hold the spawn until no living players are within this radius of the marker. 0 = spawn as soon as the delay elapses.");

            ModConfig.AutoScaleOtherSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleOtherSpawnsByPlayerCount",
                true,
                "Auto Scale Other Spawns by Player Count",
                "When enabled, apply Spawn Player Count Scale Rate per player above 4 (stacks with Other Spawn Multiplier).");

            ModConfig.OtherSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "OtherSpawnMultiplier",
                1f,
                "Other Spawn Multiplier",
                "Spawn multiplier for entities that are not mimics, bosses, jakos, specials, or traps.");
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
