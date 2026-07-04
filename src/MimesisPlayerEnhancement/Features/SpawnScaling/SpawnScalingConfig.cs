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
                "Scale dungeon monster spawn budgets by type. Host only.");

            ModConfig.SpawnScalingPlayerCountScaleRate = ModConfig.CreateTrackedEntry(_category,
                "SpawnScalingPlayerCountScaleRate",
                ScalingMath.DefaultPlayerCountScaleRate,
                "Player Count Scale Rate",
                "Extra multiplier per player above 4 when an Auto Scale … By Player Count toggle is enabled (0.10 = +10% per extra player, stacks with per-type multipliers). Minimum is 0.");

            ModConfig.AutoScaleMimicSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleMimicSpawnsByPlayerCount",
                true,
                "Auto Scale Mimic Spawns By Player Count",
                "When enabled, apply SpawnScalingPlayerCountScaleRate per player above 4 (stacks with MimicSpawnMultiplier).");

            ModConfig.MimicSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "MimicSpawnMultiplier",
                1f,
                "Mimic Spawn Multiplier",
                "Total mimic spawn budget across the run, including periodic spawns (1 = vanilla, 2 = double).");

            ModConfig.AutoScaleBossSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleBossSpawnsByPlayerCount",
                true,
                "Auto Scale Boss Spawns By Player Count",
                "When enabled, apply SpawnScalingPlayerCountScaleRate per player above 4 (stacks with BossSpawnMultiplier).");

            ModConfig.BossSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "BossSpawnMultiplier",
                1f,
                "Boss Spawn Multiplier",
                "Map-placed bosses: activates unused alternate markers and schedules bonus encounters after kill (1 = vanilla, 2 = double).");

            ModConfig.AutoScaleJakoSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleJakoSpawnsByPlayerCount",
                true,
                "Auto Scale Jako Spawns By Player Count",
                "When enabled, apply SpawnScalingPlayerCountScaleRate per player above 4 (stacks with JakoSpawnMultiplier).");

            ModConfig.JakoSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "JakoSpawnMultiplier",
                1f,
                "Jako Spawn Multiplier",
                "Total normal-monster threat budget for ambient dungeon spawns (1 = vanilla, 2 = double).");

            ModConfig.AutoScaleSpecialSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleSpecialSpawnsByPlayerCount",
                true,
                "Auto Scale Special Spawns By Player Count",
                "When enabled, apply SpawnScalingPlayerCountScaleRate per player above 4 (stacks with SpecialSpawnMultiplier).");

            ModConfig.SpecialSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "SpecialSpawnMultiplier",
                1f,
                "Special Spawn Multiplier",
                "Special monster budget for periodic spawns and map-placed specials (1 = vanilla, 2 = double).");

            ModConfig.AutoScaleTrapSpawnsByPlayerCount = ModConfig.CreateTrackedEntry(_category,
                "AutoScaleTrapSpawnsByPlayerCount",
                true,
                "Auto Scale Trap Spawns By Player Count",
                "When enabled, apply SpawnScalingPlayerCountScaleRate per player above 4 (stacks with TrapSpawnMultiplier).");

            ModConfig.TrapSpawnMultiplier = ModConfig.CreateTrackedEntry(_category,
                "TrapSpawnMultiplier",
                1f,
                "Trap Spawn Multiplier",
                "Map-placed traps: activates unused alternate markers and schedules bonus encounters after trigger/kill (1 = vanilla, 2 = double).");

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
                "Auto Scale Other Spawns By Player Count",
                "When enabled, apply SpawnScalingPlayerCountScaleRate per player above 4 (stacks with OtherSpawnMultiplier).");

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
            ModConfig.TrackFloatEntry(ModConfig.MapPlacedEncounterDelayMinSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MapPlacedEncounterDelayMaxSeconds);
            ModConfig.TrackFloatEntry(ModConfig.MapPlacedEncounterMinPlayerDistanceMeters);
            ModConfig.TrackFloatEntry(ModConfig.OtherSpawnMultiplier);
        }

        internal static void MigrateLegacyKeys(MelonLogger.Instance logger)
        {
            bool migrated = false;
            migrated |= TryMigrateLegacyFloatKey("FixedSpawnRespawnDelayMinSeconds", ModConfig.MapPlacedEncounterDelayMinSeconds);
            migrated |= TryMigrateLegacyFloatKey("FixedSpawnRespawnDelayMaxSeconds", ModConfig.MapPlacedEncounterDelayMaxSeconds);
            migrated |= TryMigrateLegacyFloatKey("FixedSpawnRespawnMinPlayerDistanceMeters", ModConfig.MapPlacedEncounterMinPlayerDistanceMeters);

            if (migrated)
            {
                logger.Msg(
                    "Spawn Scaling config migrated — FixedSpawnRespawn* keys copied to MapPlacedEncounter* keys.");
            }
        }

        private static bool TryMigrateLegacyFloatKey(
            string legacyKey,
            MelonPreferences_Entry<float> targetEntry)
        {
            if (_category.GetEntry<float>(legacyKey) is not MelonPreferences_Entry<float> legacyEntry)
            {
                return false;
            }

            targetEntry.Value = legacyEntry.Value;
            return true;
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
    }
}
