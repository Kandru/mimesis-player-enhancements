using System;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;

namespace MimesisPlayerEnhancement;

/// <summary>
/// MelonPreferences-backed configuration. Values are stored in
/// UserData/MimesisPlayerEnhancement.cfg (separate from the global MelonPreferences.cfg).
/// </summary>
public static class ModConfig
{
    private const string CategoryId = "MimesisPlayerEnhancement";

    /// <summary>Fired when any preference value changes (UI save, file reload, or programmatic update).</summary>
    public static event Action? Changed;

    public static bool IsInitialized { get; private set; }

    public static string FilePath { get; private set; } = "";

    public static MelonPreferences_Category Category { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableMorePlayers { get; private set; } = null!;
    public static MelonPreferences_Entry<int> MaxPlayers { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableMoreVoices { get; private set; } = null!;
    public static MelonPreferences_Entry<int> MaxVoiceEvents { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnablePersistence { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableStatistics { get; private set; } = null!;
    public static MelonPreferences_Entry<int> SessionReconnectGraceMinutes { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> ShowStatisticsToasts { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableJoinAnytime { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableSpawnScaling { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleMimicSpawnsByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> MimicSpawnMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleBossSpawnsByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> BossSpawnMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleJakoSpawnsByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> JakoSpawnMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleSpecialSpawnsByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> SpecialSpawnMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleTrapSpawnsByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> TrapSpawnMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<float> FixedSpawnRespawnDelayMinSeconds { get; private set; } = null!;
    public static MelonPreferences_Entry<float> FixedSpawnRespawnDelayMaxSeconds { get; private set; } = null!;
    public static MelonPreferences_Entry<float> FixedSpawnRespawnMinPlayerDistanceMeters { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleOtherSpawnsByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> OtherSpawnMultiplier { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableLootMultiplicator { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleMapConsumableLootByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> MapConsumableLootMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleMapEquipmentLootByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> MapEquipmentLootMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleMapMiscellanyLootByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> MapMiscellanyLootMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleDropConsumableLootByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> DropConsumableLootMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleDropEquipmentLootByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> DropEquipmentLootMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleDropMiscellanyLootByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> DropMiscellanyLootMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleTriggerConsumableLootByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> TriggerConsumableLootMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleTriggerEquipmentLootByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> TriggerEquipmentLootMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleTriggerMiscellanyLootByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> TriggerMiscellanyLootMultiplier { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableDebugLogging { get; private set; } = null!;

    public static void Initialize(MelonLogger.Instance logger)
    {
        Category = MelonPreferences.CreateCategory(CategoryId, "Mimesis Player Enhancement");
        FilePath = Path.Combine(MelonEnvironment.UserDataDirectory, "MimesisPlayerEnhancement.cfg");
        Category.SetFilePath(FilePath);

        EnableMorePlayers = Category.CreateEntry(
            "EnableMorePlayers",
            true,
            "Enable More Players",
            "Raise the multiplayer player cap above 4.");

        MaxPlayers = Category.CreateEntry(
            "MaxPlayers",
            999,
            "Max Players",
            "Maximum players in a session including the host (1 = solo, 2 = host + 1 client, etc.).");

        EnableMoreVoices = Category.CreateEntry(
            "EnableMoreVoices",
            true,
            "Enable More Voices",
            "Raise per-player voice recording limits.");

        MaxVoiceEvents = Category.CreateEntry(
            "MaxVoiceEvents",
            3000,
            "Max Voice Events",
            "Maximum stored voice events per player (default game limit is much lower).");

        EnablePersistence = Category.CreateEntry(
            "EnablePersistence",
            true,
            "Enable Voice Persistence",
            "Save and restore mimic voice recordings across save/load.");

        EnableStatistics = Category.CreateEntry(
            "EnableStatistics",
            true,
            "Enable Player Statistics",
            "Track per-session and global player statistics per save slot.");

        SessionReconnectGraceMinutes = Category.CreateEntry(
            "SessionReconnectGraceMinutes",
            5,
            "Session Reconnect Grace (minutes)",
            "Reuse the previous session when a player reconnects within this many minutes.");

        ShowStatisticsToasts = Category.CreateEntry(
            "ShowStatisticsToasts",
            true,
            "Show Statistics Toasts",
            "Show mod stats toasts in plain English (session intro for you, global stats on join/leave). Does not replace the game's own connect messages.");

        EnableJoinAnytime = Category.CreateEntry(
            "EnableJoinAnytime",
            true,
            "Enable Join Anytime",
            "Allow players to join a session after it has already started.");

        EnableSpawnScaling = Category.CreateEntry(
            "EnableSpawnScaling",
            true,
            "Enable Spawn Scaling",
            "Scale dungeon monster spawn budgets by type. Host only.");

        AutoScaleMimicSpawnsByPlayerCount = Category.CreateEntry(
            "AutoScaleMimicSpawnsByPlayerCount",
            true,
            "Auto Scale Mimic Spawns By Player Count",
            "When enabled, multiply mimic spawn budgets by player count / 4 for sessions with more than 4 players (stacks with MimicSpawnMultiplier).");

        MimicSpawnMultiplier = Category.CreateEntry(
            "MimicSpawnMultiplier",
            1f,
            "Mimic Spawn Multiplier",
            "Mimic spawn budget multiplier (1 = vanilla, 2 = double).");

        AutoScaleBossSpawnsByPlayerCount = Category.CreateEntry(
            "AutoScaleBossSpawnsByPlayerCount",
            true,
            "Auto Scale Boss Spawns By Player Count",
            "When enabled, multiply boss spawn budgets by player count / 4 for sessions with more than 4 players (stacks with BossSpawnMultiplier).");

        BossSpawnMultiplier = Category.CreateEntry(
            "BossSpawnMultiplier",
            1f,
            "Boss Spawn Multiplier",
            "Boss spawn budget multiplier (1 = vanilla, 2 = double).");

        AutoScaleJakoSpawnsByPlayerCount = Category.CreateEntry(
            "AutoScaleJakoSpawnsByPlayerCount",
            true,
            "Auto Scale Jako Spawns By Player Count",
            "When enabled, multiply jako spawn budgets by player count / 4 for sessions with more than 4 players (stacks with JakoSpawnMultiplier).");

        JakoSpawnMultiplier = Category.CreateEntry(
            "JakoSpawnMultiplier",
            1f,
            "Jako Spawn Multiplier",
            "Jako (normal monster) spawn budget multiplier (1 = vanilla, 2 = double).");

        AutoScaleSpecialSpawnsByPlayerCount = Category.CreateEntry(
            "AutoScaleSpecialSpawnsByPlayerCount",
            true,
            "Auto Scale Special Spawns By Player Count",
            "When enabled, multiply special spawn budgets by player count / 4 for sessions with more than 4 players (stacks with SpecialSpawnMultiplier).");

        SpecialSpawnMultiplier = Category.CreateEntry(
            "SpecialSpawnMultiplier",
            1f,
            "Special Spawn Multiplier",
            "Special monster spawn budget multiplier (1 = vanilla, 2 = double).");

        AutoScaleTrapSpawnsByPlayerCount = Category.CreateEntry(
            "AutoScaleTrapSpawnsByPlayerCount",
            true,
            "Auto Scale Trap Spawns By Player Count",
            "When enabled, multiply trap spawn counts by player count / 4 for sessions with more than 4 players (stacks with TrapSpawnMultiplier).");

        TrapSpawnMultiplier = Category.CreateEntry(
            "TrapSpawnMultiplier",
            1f,
            "Trap Spawn Multiplier",
            "Trap spawn multiplier (1 = vanilla, 2 = double). Map-placed traps use unused markers first, then respawn at the same marker when gone.");

        FixedSpawnRespawnDelayMinSeconds = Category.CreateEntry(
            "FixedSpawnRespawnDelayMinSeconds",
            5f,
            "Fixed Spawn Respawn Delay Min Seconds",
            "Minimum random delay before a map-placed monster or trap respawns at the same marker when no unused markers remain.");

        FixedSpawnRespawnDelayMaxSeconds = Category.CreateEntry(
            "FixedSpawnRespawnDelayMaxSeconds",
            30f,
            "Fixed Spawn Respawn Delay Max Seconds",
            "Maximum random delay before a map-placed monster or trap respawns at the same marker when no unused markers remain.");

        FixedSpawnRespawnMinPlayerDistanceMeters = Category.CreateEntry(
            "FixedSpawnRespawnMinPlayerDistanceMeters",
            10f,
            "Fixed Spawn Respawn Min Player Distance Meters",
            "After the respawn delay, wait until no players are within this distance (meters) before spawning at the marker. Set to 0 to respawn immediately.");

        AutoScaleOtherSpawnsByPlayerCount = Category.CreateEntry(
            "AutoScaleOtherSpawnsByPlayerCount",
            true,
            "Auto Scale Other Spawns By Player Count",
            "When enabled, multiply other spawn counts by player count / 4 for sessions with more than 4 players (stacks with OtherSpawnMultiplier).");

        OtherSpawnMultiplier = Category.CreateEntry(
            "OtherSpawnMultiplier",
            1f,
            "Other Spawn Multiplier",
            "Spawn multiplier for entities that are not mimics, bosses, jakos, specials, or traps.");

        EnableLootMultiplicator = Category.CreateEntry(
            "EnableLootMultiplicator",
            true,
            "Enable Loot Multiplicator",
            "Scale how much loot appears in a run. Host only. See each Map/Drop/Trigger entry below for what it affects.");

        AutoScaleMapConsumableLootByPlayerCount = Category.CreateEntry(
            "AutoScaleMapConsumableLootByPlayerCount",
            true,
            "Auto Scale Map Consumable Loot By Player Count",
            "Map loot = items placed on the dungeon map (spawn markers, shelves, floors). Consumables = ammo, healing, and other used-up items. When enabled, multiply by player count / 4 above 4 players (stacks with MapConsumableLootMultiplier).");

        MapConsumableLootMultiplier = Category.CreateEntry(
            "MapConsumableLootMultiplier",
            1f,
            "Map Consumable Loot Multiplier",
            "Multiplier for consumables on map spawn points: stack size and respawn count at room load. 1 = vanilla, 2 = double. Fixed map loot (specific item at a marker) may also use unused loot markers and respawn at the same spot. Random loot pools only get stack/respawn scaling.");

        AutoScaleMapEquipmentLootByPlayerCount = Category.CreateEntry(
            "AutoScaleMapEquipmentLootByPlayerCount",
            true,
            "Auto Scale Map Equipment Loot By Player Count",
            "Map loot = items placed on the dungeon map. Equipment = tools, weapons, and gear you equip. When enabled, multiply by player count / 4 above 4 players (stacks with MapEquipmentLootMultiplier).");

        MapEquipmentLootMultiplier = Category.CreateEntry(
            "MapEquipmentLootMultiplier",
            1f,
            "Map Equipment Loot Multiplier",
            "Multiplier for equipment on map spawn points: stack size and respawn count at room load. 1 = vanilla, 2 = double. Fixed map loot (specific item at a marker) may also use unused loot markers and respawn at the same spot. Random loot pools only get stack/respawn scaling.");

        AutoScaleMapMiscellanyLootByPlayerCount = Category.CreateEntry(
            "AutoScaleMapMiscellanyLootByPlayerCount",
            true,
            "Auto Scale Map Miscellany Loot By Player Count",
            "Map loot = items placed on the dungeon map. Miscellany = other pickup items (keys, misc objects). When enabled, multiply by player count / 4 above 4 players (stacks with MapMiscellanyLootMultiplier).");

        MapMiscellanyLootMultiplier = Category.CreateEntry(
            "MapMiscellanyLootMultiplier",
            1f,
            "Map Miscellany Loot Multiplier",
            "Multiplier for miscellany on map spawn points: stack size and respawn count at room load. 1 = vanilla, 2 = double. Fixed map loot (specific item at a marker) may also use unused loot markers and respawn at the same spot. Random loot pools only get stack/respawn scaling. Random pools use the dominant item type in the pool to pick a multiplier.");

        AutoScaleDropConsumableLootByPlayerCount = Category.CreateEntry(
            "AutoScaleDropConsumableLootByPlayerCount",
            true,
            "Auto Scale Drop Consumable Loot By Player Count",
            "Drop loot = items from enemy death tables when killed. Consumables = ammo, healing, and other used-up items. When enabled, multiply by player count / 4 above 4 players (stacks with DropConsumableLootMultiplier).");

        DropConsumableLootMultiplier = Category.CreateEntry(
            "DropConsumableLootMultiplier",
            1f,
            "Drop Consumable Loot Multiplier",
            "Multiplier for consumables in enemy death drops: duplicates extra item IDs in the drop list and scales consumable stack count on spawn. 1 = vanilla, 2 = double.");

        AutoScaleDropEquipmentLootByPlayerCount = Category.CreateEntry(
            "AutoScaleDropEquipmentLootByPlayerCount",
            true,
            "Auto Scale Drop Equipment Loot By Player Count",
            "Drop loot = items from enemy death tables when killed. Equipment = tools, weapons, and gear you equip. When enabled, multiply by player count / 4 above 4 players (stacks with DropEquipmentLootMultiplier).");

        DropEquipmentLootMultiplier = Category.CreateEntry(
            "DropEquipmentLootMultiplier",
            1f,
            "Drop Equipment Loot Multiplier",
            "Multiplier for equipment in enemy death drops: duplicates extra item IDs in the drop list. Stack scaling on spawn is best-effort for non-consumables. 1 = vanilla, 2 = double.");

        AutoScaleDropMiscellanyLootByPlayerCount = Category.CreateEntry(
            "AutoScaleDropMiscellanyLootByPlayerCount",
            true,
            "Auto Scale Drop Miscellany Loot By Player Count",
            "Drop loot = items from enemy death tables when killed. Miscellany = other pickup items. When enabled, multiply by player count / 4 above 4 players (stacks with DropMiscellanyLootMultiplier).");

        DropMiscellanyLootMultiplier = Category.CreateEntry(
            "DropMiscellanyLootMultiplier",
            1f,
            "Drop Miscellany Loot Multiplier",
            "Multiplier for miscellany in enemy death drops: duplicates extra item IDs in the drop list. Stack scaling on spawn is best-effort for non-consumables. 1 = vanilla, 2 = double.");

        AutoScaleTriggerConsumableLootByPlayerCount = Category.CreateEntry(
            "AutoScaleTriggerConsumableLootByPlayerCount",
            true,
            "Auto Scale Trigger Consumable Loot By Player Count",
            "Trigger loot = items spawned by map events/trigger volumes (EventAction spawns only). Consumables = ammo, healing, and other used-up items. When enabled, multiply by player count / 4 above 4 players (stacks with TriggerConsumableLootMultiplier).");

        TriggerConsumableLootMultiplier = Category.CreateEntry(
            "TriggerConsumableLootMultiplier",
            1f,
            "Trigger Consumable Loot Multiplier",
            "Multiplier for consumables from map events/triggers: scales consumable stack count when the item spawns. 1 = vanilla, 2 = double.");

        AutoScaleTriggerEquipmentLootByPlayerCount = Category.CreateEntry(
            "AutoScaleTriggerEquipmentLootByPlayerCount",
            true,
            "Auto Scale Trigger Equipment Loot By Player Count",
            "Trigger loot = items spawned by map events/trigger volumes (EventAction spawns only). Equipment = tools, weapons, and gear you equip. When enabled, multiply by player count / 4 above 4 players (stacks with TriggerEquipmentLootMultiplier).");

        TriggerEquipmentLootMultiplier = Category.CreateEntry(
            "TriggerEquipmentLootMultiplier",
            1f,
            "Trigger Equipment Loot Multiplier",
            "Multiplier for equipment from map events/triggers: stack scaling on spawn is best-effort for non-consumables. 1 = vanilla, 2 = double.");

        AutoScaleTriggerMiscellanyLootByPlayerCount = Category.CreateEntry(
            "AutoScaleTriggerMiscellanyLootByPlayerCount",
            true,
            "Auto Scale Trigger Miscellany Loot By Player Count",
            "Trigger loot = items spawned by map events/trigger volumes (EventAction spawns only). Miscellany = other pickup items. When enabled, multiply by player count / 4 above 4 players (stacks with TriggerMiscellanyLootMultiplier).");

        TriggerMiscellanyLootMultiplier = Category.CreateEntry(
            "TriggerMiscellanyLootMultiplier",
            1f,
            "Trigger Miscellany Loot Multiplier",
            "Multiplier for miscellany from map events/triggers: stack scaling on spawn is best-effort for non-consumables. 1 = vanilla, 2 = double.");

        EnableDebugLogging = Category.CreateEntry(
            "EnableDebugLogging",
            false,
            "Enable Debug Logging",
            "Emit verbose diagnostic lines to the MelonLoader console.");

        if (MaxPlayers.Value < 1)
        {
            logger.Warning("MaxPlayers must be at least 1; resetting to 1.");
            MaxPlayers.Value = 1;
        }

        MaxPlayers.OnEntryValueChanged.Subscribe((_, value) =>
        {
            if (value < 1)
            {
                logger.Warning("MaxPlayers must be at least 1; resetting to 1.");
                MaxPlayers.Value = 1;
                return;
            }

            NotifyChanged();
        });

        MaxVoiceEvents.OnEntryValueChanged.Subscribe((_, value) =>
        {
            if (value < 1)
            {
                logger.Warning("MaxVoiceEvents must be at least 1; resetting to 1.");
                MaxVoiceEvents.Value = 1;
                return;
            }

            NotifyChanged();
        });

        EnableMorePlayers.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        EnableMoreVoices.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        EnablePersistence.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());

        SessionReconnectGraceMinutes.OnEntryValueChanged.Subscribe((_, value) =>
        {
            if (value < 1)
            {
                logger.Warning("SessionReconnectGraceMinutes must be at least 1; resetting to 1.");
                SessionReconnectGraceMinutes.Value = 1;
                return;
            }

            NotifyChanged();
        });

        EnableStatistics.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        ShowStatisticsToasts.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        EnableJoinAnytime.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        EnableSpawnScaling.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleMimicSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleBossSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleJakoSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleSpecialSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleTrapSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        FixedSpawnRespawnDelayMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnFixedSpawnRespawnDelayChanged(logger, value, FixedSpawnRespawnDelayMinSeconds));
        FixedSpawnRespawnDelayMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnFixedSpawnRespawnDelayChanged(logger, value, FixedSpawnRespawnDelayMaxSeconds));
        FixedSpawnRespawnMinPlayerDistanceMeters.OnEntryValueChanged.Subscribe((_, value) => OnFixedSpawnRespawnMinPlayerDistanceChanged(logger, value));
        AutoScaleOtherSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());

        EnableLootMultiplicator.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleMapConsumableLootByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleMapEquipmentLootByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleMapMiscellanyLootByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleDropConsumableLootByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleDropEquipmentLootByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleDropMiscellanyLootByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleTriggerConsumableLootByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleTriggerEquipmentLootByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleTriggerMiscellanyLootByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());

        MimicSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, MimicSpawnMultiplier));
        BossSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, BossSpawnMultiplier));
        JakoSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, JakoSpawnMultiplier));
        SpecialSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, SpecialSpawnMultiplier));
        TrapSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, TrapSpawnMultiplier));
        OtherSpawnMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, OtherSpawnMultiplier));

        MapConsumableLootMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, MapConsumableLootMultiplier));
        MapEquipmentLootMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, MapEquipmentLootMultiplier));
        MapMiscellanyLootMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, MapMiscellanyLootMultiplier));
        DropConsumableLootMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, DropConsumableLootMultiplier));
        DropEquipmentLootMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, DropEquipmentLootMultiplier));
        DropMiscellanyLootMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, DropMiscellanyLootMultiplier));
        TriggerConsumableLootMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, TriggerConsumableLootMultiplier));
        TriggerEquipmentLootMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, TriggerEquipmentLootMultiplier));
        TriggerMiscellanyLootMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, TriggerMiscellanyLootMultiplier));

        EnableDebugLogging.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());

        IsInitialized = true;
    }

    private static void OnFixedSpawnRespawnDelayChanged(MelonLogger.Instance logger, float value, MelonPreferences_Entry<float> entry)
    {
        if (value < 0f)
        {
            logger.Warning($"{entry.Identifier} must be >= 0; resetting to 0.");
            entry.Value = 0f;
            return;
        }

        float min = FixedSpawnRespawnDelayMinSeconds.Value;
        float max = FixedSpawnRespawnDelayMaxSeconds.Value;
        if (max < min)
        {
            logger.Warning("FixedSpawnRespawnDelayMaxSeconds must be >= FixedSpawnRespawnDelayMinSeconds; syncing max to min.");
            FixedSpawnRespawnDelayMaxSeconds.Value = min;
        }

        NotifyChanged();
    }

    private static void OnFixedSpawnRespawnMinPlayerDistanceChanged(MelonLogger.Instance logger, float value)
    {
        if (value < 0f)
        {
            logger.Warning("FixedSpawnRespawnMinPlayerDistanceMeters must be >= 0; resetting to 0.");
            FixedSpawnRespawnMinPlayerDistanceMeters.Value = 0f;
            return;
        }

        NotifyChanged();
    }

    private static void OnSpawnMultiplierChanged(MelonLogger.Instance logger, float value, MelonPreferences_Entry<float> entry)
    {
        if (value < 0f)
        {
            logger.Warning($"{entry.Identifier} must be >= 0; resetting to 0.");
            entry.Value = 0f;
            return;
        }

        NotifyChanged();
    }

    private static void NotifyChanged() => Changed?.Invoke();
}
