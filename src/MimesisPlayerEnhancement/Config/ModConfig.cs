using System;
using System.Collections.Generic;
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
    public static MelonPreferences_Entry<int> MaxIndoorVoiceEvents { get; private set; } = null!;
    public static MelonPreferences_Entry<int> MaxDeathMatchVoiceEvents { get; private set; } = null!;
    public static MelonPreferences_Entry<int> MaxOutdoorVoiceEvents { get; private set; } = null!;

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

    public static MelonPreferences_Entry<bool> EnableMoneyMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleStartupMoneyByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> StartupMoneyMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleRoundGoalMoneyByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> RoundGoalMoneyMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleScrapSellValueByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> ScrapSellValueMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleShopBuyPriceByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> ShopBuyPriceMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleShopItemsByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> ShopItemsMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<int> ShopDiscountMinPercent { get; private set; } = null!;
    public static MelonPreferences_Entry<int> ShopDiscountMaxPercent { get; private set; } = null!;
    public static MelonPreferences_Entry<int> ShopDiscountChancePercent { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleReinforcePriceByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> ReinforcePriceMultiplier { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableDungeonTime { get; private set; } = null!;
    public static MelonPreferences_Entry<int> DungeonTimeBaselinePlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<float> ExtraShiftSecondsPerPlayerAboveBaseline { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableDungeonSizeScaling { get; private set; } = null!;
    public static MelonPreferences_Entry<float> DungeonSizeMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> AutoScaleDungeonSizeByPlayerCount { get; private set; } = null!;
    public static MelonPreferences_Entry<int> DungeonSizeBaselinePlayerCount { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableSpectatorTransition { get; private set; } = null!;
    public static MelonPreferences_Entry<float> DyingWaitTimeMultiplier { get; private set; } = null!;
    public static MelonPreferences_Entry<float> DeadCameraDurationMultiplier { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableDungeonRandomizer { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> RandomizeDungeonPick { get; private set; } = null!;
    public static MelonPreferences_Entry<string> DungeonPickPoolMode { get; private set; } = null!;
    public static MelonPreferences_Entry<string> DungeonAllowlist { get; private set; } = null!;
    public static MelonPreferences_Entry<string> DungeonBlocklist { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> IgnoreDungeonExcludeList { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> RandomizeLayoutFlow { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> RandomizeMapVariant { get; private set; } = null!;
    public static MelonPreferences_Entry<bool> RandomizeDungeonSeed { get; private set; } = null!;

    public static MelonPreferences_Entry<bool> EnableDebugLogging { get; private set; } = null!;

    private static readonly List<MelonPreferences_Entry<float>> FloatEntries = new();

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

        MaxIndoorVoiceEvents = Category.CreateEntry(
            "MaxIndoorVoiceEvents",
            3000,
            "Max Indoor Voice Events",
            "Maximum stored voice events per player in indoor dungeon runs (default game limit is much lower).");

        MaxDeathMatchVoiceEvents = Category.CreateEntry(
            "MaxDeathMatchVoiceEvents",
            3000,
            "Max Deathmatch Voice Events",
            "Maximum stored voice events per player in deathmatch (default game limit is much lower).");

        MaxOutdoorVoiceEvents = Category.CreateEntry(
            "MaxOutdoorVoiceEvents",
            3000,
            "Max Outdoor Voice Events",
            "Maximum stored voice events per player outdoors (default game limit is much lower).");

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

        EnableMoneyMultiplier = Category.CreateEntry(
            "EnableMoneyMultiplier",
            true,
            "Enable Money Multiplier",
            "Scale startup money, round goal quota, scrap/sell values, shop buy prices, shop item count, and reinforce costs. Host only.");

        AutoScaleStartupMoneyByPlayerCount = Category.CreateEntry(
            "AutoScaleStartupMoneyByPlayerCount",
            true,
            "Auto Scale Startup Money By Player Count",
            "When enabled, multiply startup money by player count / 4 for sessions with more than 4 players (stacks with StartupMoneyMultiplier).");

        StartupMoneyMultiplier = Category.CreateEntry(
            "StartupMoneyMultiplier",
            1f,
            "Startup Money Multiplier",
            "Starting maintenance-room currency on a new game or session reset (1 = vanilla, 2 = double).");

        AutoScaleRoundGoalMoneyByPlayerCount = Category.CreateEntry(
            "AutoScaleRoundGoalMoneyByPlayerCount",
            true,
            "Auto Scale Round Goal Money By Player Count",
            "When enabled, multiply the stage target currency (quota) by player count / 4 for sessions with more than 4 players (stacks with RoundGoalMoneyMultiplier).");

        RoundGoalMoneyMultiplier = Category.CreateEntry(
            "RoundGoalMoneyMultiplier",
            1f,
            "Round Goal Money Multiplier",
            "Target currency required to finish a stage (1 = vanilla, 2 = double).");

        AutoScaleScrapSellValueByPlayerCount = Category.CreateEntry(
            "AutoScaleScrapSellValueByPlayerCount",
            true,
            "Auto Scale Scrap Sell Value By Player Count",
            "When enabled, multiply item scrap/sell values by player count / 4 for sessions with more than 4 players (stacks with ScrapSellValueMultiplier).");

        ScrapSellValueMultiplier = Category.CreateEntry(
            "ScrapSellValueMultiplier",
            1f,
            "Scrap Sell Value Multiplier",
            "Currency earned when scrapping items and item value counted toward the tram quota (1 = vanilla, 2 = double).");

        AutoScaleShopBuyPriceByPlayerCount = Category.CreateEntry(
            "AutoScaleShopBuyPriceByPlayerCount",
            true,
            "Auto Scale Shop Buy Price By Player Count",
            "When enabled, multiply maintenance shop buy prices by player count / 4 for sessions with more than 4 players (stacks with ShopBuyPriceMultiplier).");

        ShopBuyPriceMultiplier = Category.CreateEntry(
            "ShopBuyPriceMultiplier",
            1f,
            "Shop Buy Price Multiplier",
            "Maintenance shop and vending-machine kiosk purchase cost multiplier (1 = vanilla, 2 = double). Applied when shop items are initialized each maintenance round.");

        AutoScaleShopItemsByPlayerCount = Category.CreateEntry(
            "AutoScaleShopItemsByPlayerCount",
            true,
            "Auto Scale Shop Items By Player Count",
            "When enabled, multiply maintenance shop item count by player count / 4 for sessions with more than 4 players (stacks with ShopItemsMultiplier).");

        ShopItemsMultiplier = Category.CreateEntry(
            "ShopItemsMultiplier",
            1f,
            "Shop Items Multiplier",
            "Number of unique items offered in the maintenance shop (1 = vanilla, 2 = double). Extra items are rolled from vending-machine shop groups on the map.");

        ShopDiscountMinPercent = Category.CreateEntry(
            "ShopDiscountMinPercent",
            0,
            "Shop Discount Min Percent",
            "Minimum shop discount percentage when a discount is rolled (0-100). Only used when ShopDiscountChancePercent is above 0.");

        ShopDiscountMaxPercent = Category.CreateEntry(
            "ShopDiscountMaxPercent",
            100,
            "Shop Discount Max Percent",
            "Maximum shop discount percentage when a discount is rolled (0-100). Must be >= ShopDiscountMinPercent.");

        ShopDiscountChancePercent = Category.CreateEntry(
            "ShopDiscountChancePercent",
            0,
            "Shop Discount Chance Percent",
            "Chance per shop item to receive a discount between min and max percent (0 = vanilla shop discounts, 100 = every item discounted).");

        AutoScaleReinforcePriceByPlayerCount = Category.CreateEntry(
            "AutoScaleReinforcePriceByPlayerCount",
            true,
            "Auto Scale Reinforce Price By Player Count",
            "When enabled, multiply item reinforcement costs by player count / 4 for sessions with more than 4 players (stacks with ReinforcePriceMultiplier).");

        ReinforcePriceMultiplier = Category.CreateEntry(
            "ReinforcePriceMultiplier",
            1f,
            "Reinforce Price Multiplier",
            "Maintenance item reinforcement cost multiplier (1 = vanilla, 2 = double).");

        EnableDungeonTime = Category.CreateEntry(
            "EnableDungeonTime",
            true,
            "Enable Dungeon Time",
            "Extend dungeon shift length on the host when player count exceeds the baseline.");

        DungeonTimeBaselinePlayerCount = Category.CreateEntry(
            "DungeonTimeBaselinePlayerCount",
            4,
            "Dungeon Time Baseline Player Count",
            "No extra shift time at or below this player count (vanilla is 4). Minimum is 1.");

        ExtraShiftSecondsPerPlayerAboveBaseline = Category.CreateEntry(
            "ExtraShiftSecondsPerPlayerAboveBaseline",
            10f,
            "Extra Shift Seconds Per Player Above Baseline",
            "Real seconds added to the shift deadline for each player above the baseline. Minimum is 0.");

        EnableDungeonSizeScaling = Category.CreateEntry(
            "EnableDungeonSizeScaling",
            true,
            "Enable Dungeon Size Scaling",
            "Scale procedural dungeon length. Applied on each machine during DunGen generation so layouts stay in sync.");

        DungeonSizeMultiplier = Category.CreateEntry(
            "DungeonSizeMultiplier",
            1f,
            "Dungeon Size Multiplier",
            "Base dungeon length multiplier (1 = vanilla, 2 = double). Stacks with Auto Scale Dungeon Size By Player Count.");

        AutoScaleDungeonSizeByPlayerCount = Category.CreateEntry(
            "AutoScaleDungeonSizeByPlayerCount",
            true,
            "Auto Scale Dungeon Size By Player Count",
            "When enabled, multiply dungeon size by player count / baseline above the baseline (e.g. 8 players with baseline 4 = ×2; stacks with DungeonSizeMultiplier).");

        DungeonSizeBaselinePlayerCount = Category.CreateEntry(
            "DungeonSizeBaselinePlayerCount",
            4,
            "Dungeon Size Baseline Player Count",
            "No player-count size bonus at or below this count (vanilla is 4). Minimum is 1.");

        EnableSpectatorTransition = Category.CreateEntry(
            "EnableSpectatorTransition",
            true,
            "Enable Spectator Transition",
            "Shorten downed time and dead-camera duration before entering spectator mode.");

        DyingWaitTimeMultiplier = Category.CreateEntry(
            "DyingWaitTimeMultiplier",
            1f,
            "Dying Wait Time Multiplier",
            "Scales server down/dying time before spectator (1 = vanilla, 0 = instant). Also shortens the teammate revive window. Host only.");

        DeadCameraDurationMultiplier = Category.CreateEntry(
            "DeadCameraDurationMultiplier",
            1f,
            "Dead Camera Duration Multiplier",
            "Scales local dead-camera transition time before spectator (1 = vanilla, 0 = instant). Applies on each machine with the mod loaded.");

        EnableDungeonRandomizer = Category.CreateEntry(
            "EnableDungeonRandomizer",
            false,
            "Enable Dungeon Randomizer",
            "Randomize dungeon selection on the host: tram dungeon pick, layout flow, map variant, and procedural seed. Host only.");

        RandomizeDungeonPick = Category.CreateEntry(
            "RandomizeDungeonPick",
            true,
            "Randomize Dungeon Pick",
            "Override which dungeon master ID is rolled on the tram.");

        DungeonPickPoolMode = Category.CreateEntry(
            "DungeonPickPoolMode",
            "WidenVanilla",
            "Dungeon Pick Pool Mode",
            "WidenVanilla = keep cycle weights but allow repeats sooner; AllActiveUniform = pick uniformly from all active dungeons (ignores cycle table).");

        DungeonAllowlist = Category.CreateEntry(
            "DungeonAllowlist",
            "",
            "Dungeon Allowlist",
            "Comma-separated dungeon master IDs. When non-empty, only these IDs are eligible.");

        DungeonBlocklist = Category.CreateEntry(
            "DungeonBlocklist",
            "",
            "Dungeon Blocklist",
            "Comma-separated dungeon master IDs to exclude from the pool.");

        IgnoreDungeonExcludeList = Category.CreateEntry(
            "IgnoreDungeonExcludeList",
            true,
            "Ignore Dungeon Exclude List",
            "When using WidenVanilla, do not exclude recently played dungeons from the tram roll.");

        RandomizeLayoutFlow = Category.CreateEntry(
            "RandomizeLayoutFlow",
            true,
            "Randomize Layout Flow",
            "Pick DunGen layout flows uniformly from each dungeon's candidates instead of using weighted vanilla rolls.");

        RandomizeMapVariant = Category.CreateEntry(
            "RandomizeMapVariant",
            true,
            "Randomize Map Variant",
            "Pick map variants uniformly from each dungeon's MapIDs instead of vanilla selection.");

        RandomizeDungeonSeed = Category.CreateEntry(
            "RandomizeDungeonSeed",
            true,
            "Randomize Dungeon Seed",
            "Replace the procedural dungeon seed with a new random value when a dungeon is chosen.");

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

        SanitizeShopDiscountPercents(logger);
        OnDungeonPickPoolModeChanged(logger, DungeonPickPoolMode.Value);

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

        MaxIndoorVoiceEvents.OnEntryValueChanged.Subscribe((_, value) =>
        {
            if (value < 1)
            {
                logger.Warning("MaxIndoorVoiceEvents must be at least 1; resetting to 1.");
                MaxIndoorVoiceEvents.Value = 1;
                return;
            }

            NotifyChanged();
        });

        MaxDeathMatchVoiceEvents.OnEntryValueChanged.Subscribe((_, value) =>
        {
            if (value < 1)
            {
                logger.Warning("MaxDeathMatchVoiceEvents must be at least 1; resetting to 1.");
                MaxDeathMatchVoiceEvents.Value = 1;
                return;
            }

            NotifyChanged();
        });

        MaxOutdoorVoiceEvents.OnEntryValueChanged.Subscribe((_, value) =>
        {
            if (value < 1)
            {
                logger.Warning("MaxOutdoorVoiceEvents must be at least 1; resetting to 1.");
                MaxOutdoorVoiceEvents.Value = 1;
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

        EnableMoneyMultiplier.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleStartupMoneyByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleRoundGoalMoneyByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleScrapSellValueByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleShopBuyPriceByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleShopItemsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        AutoScaleReinforcePriceByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());

        StartupMoneyMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, StartupMoneyMultiplier));
        RoundGoalMoneyMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, RoundGoalMoneyMultiplier));
        ScrapSellValueMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, ScrapSellValueMultiplier));
        ShopBuyPriceMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, ShopBuyPriceMultiplier));
        ShopItemsMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, ShopItemsMultiplier));
        ShopDiscountMinPercent.OnEntryValueChanged.Subscribe((_, value) => OnShopDiscountPercentChanged(logger, value, ShopDiscountMinPercent));
        ShopDiscountMaxPercent.OnEntryValueChanged.Subscribe((_, value) => OnShopDiscountPercentChanged(logger, value, ShopDiscountMaxPercent));
        ShopDiscountChancePercent.OnEntryValueChanged.Subscribe((_, value) => OnShopDiscountPercentChanged(logger, value, ShopDiscountChancePercent));
        ReinforcePriceMultiplier.OnEntryValueChanged.Subscribe((_, value) => OnSpawnMultiplierChanged(logger, value, ReinforcePriceMultiplier));

        EnableDungeonTime.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        DungeonTimeBaselinePlayerCount.OnEntryValueChanged.Subscribe((_, value) =>
        {
            if (value < 1)
            {
                logger.Warning("DungeonTimeBaselinePlayerCount must be at least 1; resetting to 1.");
                DungeonTimeBaselinePlayerCount.Value = 1;
                return;
            }

            NotifyChanged();
        });
        ExtraShiftSecondsPerPlayerAboveBaseline.OnEntryValueChanged.Subscribe((_, value) =>
            OnExtraShiftSecondsPerPlayerChanged(logger, value));

        EnableDungeonSizeScaling.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        DungeonSizeMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
            OnSpawnMultiplierChanged(logger, value, DungeonSizeMultiplier));
        AutoScaleDungeonSizeByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        DungeonSizeBaselinePlayerCount.OnEntryValueChanged.Subscribe((_, value) =>
        {
            if (value < 1)
            {
                logger.Warning("DungeonSizeBaselinePlayerCount must be at least 1; resetting to 1.");
                DungeonSizeBaselinePlayerCount.Value = 1;
                return;
            }

            NotifyChanged();
        });

        EnableSpectatorTransition.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        DyingWaitTimeMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
            OnSpawnMultiplierChanged(logger, value, DyingWaitTimeMultiplier));
        DeadCameraDurationMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
            OnSpawnMultiplierChanged(logger, value, DeadCameraDurationMultiplier));

        EnableDungeonRandomizer.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        RandomizeDungeonPick.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        DungeonPickPoolMode.OnEntryValueChanged.Subscribe((_, value) => OnDungeonPickPoolModeChanged(logger, value));
        DungeonAllowlist.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        DungeonBlocklist.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        IgnoreDungeonExcludeList.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        RandomizeLayoutFlow.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        RandomizeMapVariant.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
        RandomizeDungeonSeed.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());

        EnableDebugLogging.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());

        RegisterFloatEntries();
        ModConfigFloatHelper.SanitizeAll(FloatEntries);
        NormalizeSavedFloats();

        IsInitialized = true;
    }

    public static void NormalizeSavedFloats() =>
        ModConfigFloatHelper.NormalizeSavedFloats(FilePath, FloatEntries);

    internal static void SanitizeFloatEntries() =>
        ModConfigFloatHelper.SanitizeAll(FloatEntries);

    private static void RegisterFloatEntries()
    {
        FloatEntries.AddRange(new[]
        {
            MimicSpawnMultiplier,
            BossSpawnMultiplier,
            JakoSpawnMultiplier,
            SpecialSpawnMultiplier,
            TrapSpawnMultiplier,
            FixedSpawnRespawnDelayMinSeconds,
            FixedSpawnRespawnDelayMaxSeconds,
            FixedSpawnRespawnMinPlayerDistanceMeters,
            OtherSpawnMultiplier,
            MapConsumableLootMultiplier,
            MapEquipmentLootMultiplier,
            MapMiscellanyLootMultiplier,
            DropConsumableLootMultiplier,
            DropEquipmentLootMultiplier,
            DropMiscellanyLootMultiplier,
            TriggerConsumableLootMultiplier,
            TriggerEquipmentLootMultiplier,
            TriggerMiscellanyLootMultiplier,
            StartupMoneyMultiplier,
            RoundGoalMoneyMultiplier,
            ScrapSellValueMultiplier,
            ShopBuyPriceMultiplier,
            ShopItemsMultiplier,
            ReinforcePriceMultiplier,
            ExtraShiftSecondsPerPlayerAboveBaseline,
            DungeonSizeMultiplier,
            DyingWaitTimeMultiplier,
            DeadCameraDurationMultiplier,
        });
    }

    private static void OnExtraShiftSecondsPerPlayerChanged(MelonLogger.Instance logger, float value)
    {
        if (value < 0f)
        {
            logger.Warning("ExtraShiftSecondsPerPlayerAboveBaseline must be >= 0; resetting to 0.");
            ExtraShiftSecondsPerPlayerAboveBaseline.Value = 0f;
            return;
        }

        ModConfigFloatHelper.SanitizeEntry(ExtraShiftSecondsPerPlayerAboveBaseline);
        NotifyChanged();
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

        ModConfigFloatHelper.SanitizeEntry(entry);
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

        ModConfigFloatHelper.SanitizeEntry(FixedSpawnRespawnMinPlayerDistanceMeters);
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

        ModConfigFloatHelper.SanitizeEntry(entry);
        NotifyChanged();
    }

    private static void SanitizeShopDiscountPercents(MelonLogger.Instance logger)
    {
        OnShopDiscountPercentChanged(logger, ShopDiscountMinPercent.Value, ShopDiscountMinPercent);
        OnShopDiscountPercentChanged(logger, ShopDiscountMaxPercent.Value, ShopDiscountMaxPercent);
        OnShopDiscountPercentChanged(logger, ShopDiscountChancePercent.Value, ShopDiscountChancePercent);
    }

    private static void OnShopDiscountPercentChanged(MelonLogger.Instance logger, int value, MelonPreferences_Entry<int> entry)
    {
        if (value < 0)
        {
            logger.Warning($"{entry.Identifier} must be >= 0; resetting to 0.");
            entry.Value = 0;
            return;
        }

        if (value > 100)
        {
            logger.Warning($"{entry.Identifier} must be <= 100; resetting to 100.");
            entry.Value = 100;
            return;
        }

        if (ShopDiscountMaxPercent.Value < ShopDiscountMinPercent.Value)
        {
            logger.Warning("ShopDiscountMaxPercent must be >= ShopDiscountMinPercent; syncing max to min.");
            ShopDiscountMaxPercent.Value = ShopDiscountMinPercent.Value;
        }

        NotifyChanged();
    }

    private static void OnDungeonPickPoolModeChanged(MelonLogger.Instance logger, string value)
    {
        if (!string.Equals(value, "WidenVanilla", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(value, "AllActiveUniform", StringComparison.OrdinalIgnoreCase))
        {
            logger.Warning("DungeonPickPoolMode must be WidenVanilla or AllActiveUniform; resetting to WidenVanilla.");
            DungeonPickPoolMode.Value = "WidenVanilla";
            return;
        }

        NotifyChanged();
    }

    private static void NotifyChanged() => Changed?.Invoke();
}
