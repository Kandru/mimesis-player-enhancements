using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;

namespace MimesisPlayerEnhancement
{
    /// <summary>
    /// MelonPreferences-backed configuration. Values are stored in
    /// UserData/MimesisPlayerEnhancement.cfg (separate from the global MelonPreferences.cfg).
    /// Global settings use the [MimesisPlayerEnhancement] section; each feature has
    /// its own [MimesisPlayerEnhancement_FeatureName] section.
    /// </summary>
    public static class ModConfig
    {
        private const string MainCategoryId = "MimesisPlayerEnhancement";

        /// <summary>Fired when any preference value changes (UI save, file reload, or programmatic update).</summary>
        public static event Action? Changed;

        /// <summary>Increments whenever configuration values change at runtime.</summary>
        public static int Version => ModConfigRegistry.Version;

        public static bool IsInitialized { get; private set; }

        public static string FilePath { get; private set; } = "";

        public static MelonPreferences_Category MainCategory { get; private set; } = null!;

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
        public static MelonPreferences_Entry<bool> ShowPlayerAnnouncements { get; private set; } = null!;
        public static MelonPreferences_Entry<float> ModToastDurationSeconds { get; private set; } = null!;

        public static MelonPreferences_Entry<bool> EnableJoinAnytime { get; private set; } = null!;
        public static MelonPreferences_Entry<int> JoinConnectionGraceSeconds { get; private set; } = null!;

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
        public static MelonPreferences_Entry<float> MapPlacedEncounterDelayMinSeconds { get; private set; } = null!;
        public static MelonPreferences_Entry<float> MapPlacedEncounterDelayMaxSeconds { get; private set; } = null!;
        public static MelonPreferences_Entry<float> MapPlacedEncounterMinPlayerDistanceMeters { get; private set; } = null!;
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
        public static MelonPreferences_Entry<string> LootItemFilterMode { get; private set; } = null!;
        public static MelonPreferences_Entry<string> LootAllowlist { get; private set; } = null!;
        public static MelonPreferences_Entry<string> LootBlocklist { get; private set; } = null!;
        public static MelonPreferences_Entry<int> ConvertFakeActorDyingDropChancePercent { get; private set; } = null!;

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

        public static MelonPreferences_Entry<bool> EnableRoomEntryDelay { get; private set; } = null!;
        public static MelonPreferences_Entry<float> RoomEntryDelayMultiplier { get; private set; } = null!;

        public static MelonPreferences_Entry<bool> EnableMimicTuning { get; private set; } = null!;
        public static MelonPreferences_Entry<bool> RandomizeMimicPossessionDuration { get; private set; } = null!;
        public static MelonPreferences_Entry<float> MimicPossessionMinTimeSeconds { get; private set; } = null!;
        public static MelonPreferences_Entry<float> MimicPossessionMaxTimeSeconds { get; private set; } = null!;
        public static MelonPreferences_Entry<float> MimicPossessionCooltimeMultiplier { get; private set; } = null!;

        public static MelonPreferences_Entry<bool> EnablePlayerTuning { get; private set; } = null!;
        public static MelonPreferences_Entry<float> MoveSpeedMultiplier { get; private set; } = null!;
        public static MelonPreferences_Entry<float> MaxStaminaMultiplier { get; private set; } = null!;
        public static MelonPreferences_Entry<float> StaminaDrainMultiplier { get; private set; } = null!;
        public static MelonPreferences_Entry<float> StaminaRegenMultiplier { get; private set; } = null!;
        public static MelonPreferences_Entry<float> StaminaRegenDelayMultiplier { get; private set; } = null!;
        public static MelonPreferences_Entry<float> MaxCarryWeightMultiplier { get; private set; } = null!;

        public static MelonPreferences_Entry<bool> EnableDungeonRandomizer { get; private set; } = null!;
        public static MelonPreferences_Entry<bool> RandomizeDungeonPick { get; private set; } = null!;
        public static MelonPreferences_Entry<string> DungeonPickPoolMode { get; private set; } = null!;
        public static MelonPreferences_Entry<string> DungeonAllowlist { get; private set; } = null!;
        public static MelonPreferences_Entry<string> DungeonBlocklist { get; private set; } = null!;
        public static MelonPreferences_Entry<bool> IgnoreDungeonExcludeList { get; private set; } = null!;
        public static MelonPreferences_Entry<bool> RandomizeLayoutFlow { get; private set; } = null!;
        public static MelonPreferences_Entry<bool> RandomizeMapVariant { get; private set; } = null!;
        public static MelonPreferences_Entry<bool> RandomizeDungeonSeed { get; private set; } = null!;

        public static MelonPreferences_Entry<bool> EnableWebDashboard { get; private set; } = null!;
        public static MelonPreferences_Entry<string> WebDashboardListenAddress { get; private set; } = null!;
        public static MelonPreferences_Entry<int> WebDashboardListenPort { get; private set; } = null!;

        public static MelonPreferences_Entry<bool> EnableDebugLogging { get; private set; } = null!;

        public static MelonPreferences_Entry<bool> EnableExtendedSaveSlots { get; private set; } = null!;


        private static MelonPreferences_Category _mainCategory = null!;
        private static MelonPreferences_Category _morePlayersCategory = null!;
        private static MelonPreferences_Category _moreVoicesCategory = null!;
        private static MelonPreferences_Category _persistenceCategory = null!;
        private static MelonPreferences_Category _statisticsCategory = null!;
        private static MelonPreferences_Category _playerAnnouncementsCategory = null!;
        private static MelonPreferences_Category _joinAnytimeCategory = null!;
        private static MelonPreferences_Category _spawnScalingCategory = null!;
        private static MelonPreferences_Category _lootMultiplicatorCategory = null!;
        private static MelonPreferences_Category _moneyMultiplierCategory = null!;
        private static MelonPreferences_Category _dungeonTimeCategory = null!;
        private static MelonPreferences_Category _roomEntryDelayCategory = null!;
        private static MelonPreferences_Category _mimicTuningCategory = null!;
        private static MelonPreferences_Category _playerTuningCategory = null!;
        private static MelonPreferences_Category _dungeonRandomizerCategory = null!;
        private static MelonPreferences_Category _webDashboardCategory = null!;
        private static MelonPreferences_Category _extendedSaveSlotsCategory = null!;

        private static readonly List<MelonPreferences_Entry<float>> FloatEntries = [];

        public static void Initialize(MelonLogger.Instance logger)
        {
            ModConfigRegistry.ClearRegistrationOrder();
            FilePath = Path.Combine(MelonEnvironment.UserDataDirectory, "MimesisPlayerEnhancement.cfg");
            SparseTomlConfig.RepairTomletCompatibility(FilePath);

            _mainCategory = CreateCategory(MainCategoryId, "Mimesis Player Enhancement");
            MainCategory = _mainCategory;
            _morePlayersCategory = CreateCategory("MimesisPlayerEnhancement_MorePlayers", "More Players");
            _moreVoicesCategory = CreateCategory("MimesisPlayerEnhancement_MoreVoices", "More Voices");
            _persistenceCategory = CreateCategory("MimesisPlayerEnhancement_Persistence", "Persistence");
            _statisticsCategory = CreateCategory("MimesisPlayerEnhancement_Statistics", "Statistics");
            _playerAnnouncementsCategory = CreateCategory("MimesisPlayerEnhancement_PlayerAnnouncements", "Player Announcements");
            _joinAnytimeCategory = CreateCategory("MimesisPlayerEnhancement_JoinAnytime", "Join Anytime");
            _spawnScalingCategory = CreateCategory("MimesisPlayerEnhancement_SpawnScaling", "Spawn Scaling");
            _lootMultiplicatorCategory = CreateCategory("MimesisPlayerEnhancement_LootMultiplicator", "Loot Multiplicator");
            _moneyMultiplierCategory = CreateCategory("MimesisPlayerEnhancement_MoneyMultiplier", "Money Multiplier");
            _dungeonTimeCategory = CreateCategory("MimesisPlayerEnhancement_DungeonTime", "Dungeon Time");
            _roomEntryDelayCategory = CreateCategory("MimesisPlayerEnhancement_RoomEntryDelay", "Room Entry Delay");
            _mimicTuningCategory = CreateCategory("MimesisPlayerEnhancement_MimicTuning", "Mimic Tuning");
            _playerTuningCategory = CreateCategory("MimesisPlayerEnhancement_PlayerTuning", "Player Tuning");
            _dungeonRandomizerCategory = CreateCategory("MimesisPlayerEnhancement_DungeonRandomizer", "Dungeon Randomizer");
            _webDashboardCategory = CreateCategory("MimesisPlayerEnhancement_WebDashboard", "Web Dashboard");
            _extendedSaveSlotsCategory = CreateCategory("MimesisPlayerEnhancement_ExtendedSaveSlots", "Extended Save Slots");

            ModToastDurationSeconds = CreateTrackedEntry(_mainCategory, 
                "ModToastDurationSeconds",
                5f,
                "Mod Toast Duration (seconds)",
                "How long [PlayerEnhancements] toasts stay visible before fading. Vanilla join/leave toasts are unchanged (~2 seconds). Each player controls this locally.");

            EnableDebugLogging = CreateTrackedEntry(_mainCategory, 
                "EnableDebugLogging",
                false,
                "Enable Debug Logging",
                "Emit verbose diagnostic lines to the MelonLoader console.");

            EnableMorePlayers = CreateTrackedEntry(_morePlayersCategory, 
                "EnableMorePlayers",
                false,
                "Enable More Players",
                "Raise the multiplayer player cap above 4.");

            MaxPlayers = CreateTrackedEntry(_morePlayersCategory, 
                "MaxPlayers",
                32,
                "Max Players",
                "Maximum players in a session including the host (1 = solo, 2 = host + 1 client, etc.).");

            EnableMoreVoices = CreateTrackedEntry(_moreVoicesCategory, 
                "EnableMoreVoices",
                true,
                "Enable More Voices",
                "Raise per-player voice recording limits.");

            MaxIndoorVoiceEvents = CreateTrackedEntry(_moreVoicesCategory, 
                "MaxIndoorVoiceEvents",
                3000,
                "Max Indoor Voice Events",
                "Maximum stored voice events per player in indoor dungeon runs (default game limit is much lower).");

            MaxDeathMatchVoiceEvents = CreateTrackedEntry(_moreVoicesCategory, 
                "MaxDeathMatchVoiceEvents",
                3000,
                "Max Deathmatch Voice Events",
                "Maximum stored voice events per player in deathmatch (default game limit is much lower).");

            MaxOutdoorVoiceEvents = CreateTrackedEntry(_moreVoicesCategory, 
                "MaxOutdoorVoiceEvents",
                3000,
                "Max Outdoor Voice Events",
                "Maximum stored voice events per player outdoors (default game limit is much lower).");

            EnablePersistence = CreateTrackedEntry(_persistenceCategory, 
                "EnablePersistence",
                true,
                "Enable Voice Persistence",
                "Save and restore mimic voice recordings across save/load.");

            EnableStatistics = CreateTrackedEntry(_statisticsCategory, 
                "EnableStatistics",
                true,
                "Enable Player Statistics",
                "Track per-session and global player statistics per save slot.");

            SessionReconnectGraceMinutes = CreateTrackedEntry(_statisticsCategory, 
                "SessionReconnectGraceMinutes",
                5,
                "Session Reconnect Grace (minutes)",
                "Reuse the previous session when a player reconnects within this many minutes.");

            ShowStatisticsToasts = CreateTrackedEntry(_statisticsCategory, 
                "ShowStatisticsToasts",
                true,
                "Show Statistics Toasts",
                "Show mod stats toasts in plain English (session intro for you, global stats on join/leave). Does not replace the game's own connect messages.");

            ShowPlayerAnnouncements = CreateTrackedEntry(_playerAnnouncementsCategory, 
                "ShowPlayerAnnouncements",
                true,
                "Show Player Announcements",
                "Show in-game toasts for dungeon run settings, boss spawns, and your per-map stats when you die. Does not replace the game's own messages.");

            EnableJoinAnytime = CreateTrackedEntry(_joinAnytimeCategory, 
                "EnableJoinAnytime",
                true,
                "Enable Join Anytime",
                "Allow players to join a session after it has already started.");

            JoinConnectionGraceSeconds = CreateTrackedEntry(_joinAnytimeCategory, 
                "JoinConnectionGraceSeconds",
                30,
                "Join Connection Grace Seconds",
                "When a player connects, block tram departure for this many seconds. Players who fail to finish loading are kicked (host is never kicked).");

            EnableExtendedSaveSlots = CreateTrackedEntry(_extendedSaveSlotsCategory,
                "EnableExtendedSaveSlots",
                false,
                "Enable Extended Save Slots",
                "When enabled, replaces the separate New/Load Tram menus with a unified save picker (up to 99 manual slots). When disabled, vanilla New/Load Tram behavior is used.");

            EnableSpawnScaling = CreateTrackedEntry(_spawnScalingCategory, 
                "EnableSpawnScaling",
                false,
                "Enable Spawn Scaling",
                "Scale dungeon monster spawn budgets by type. Host only.");

            AutoScaleMimicSpawnsByPlayerCount = CreateTrackedEntry(_spawnScalingCategory, 
                "AutoScaleMimicSpawnsByPlayerCount",
                true,
                "Auto Scale Mimic Spawns By Player Count",
                "When enabled, multiply mimic spawn budgets by player count / 4 for sessions with more than 4 players (stacks with MimicSpawnMultiplier).");

            MimicSpawnMultiplier = CreateTrackedEntry(_spawnScalingCategory, 
                "MimicSpawnMultiplier",
                1f,
                "Mimic Spawn Multiplier",
                "Total mimic spawn budget across the run, including periodic spawns (1 = vanilla, 2 = double).");

            AutoScaleBossSpawnsByPlayerCount = CreateTrackedEntry(_spawnScalingCategory, 
                "AutoScaleBossSpawnsByPlayerCount",
                true,
                "Auto Scale Boss Spawns By Player Count",
                "When enabled, multiply boss spawn budgets by player count / 4 for sessions with more than 4 players (stacks with BossSpawnMultiplier).");

            BossSpawnMultiplier = CreateTrackedEntry(_spawnScalingCategory, 
                "BossSpawnMultiplier",
                1f,
                "Boss Spawn Multiplier",
                "Map-placed bosses: activates unused alternate markers and schedules bonus encounters after kill (1 = vanilla, 2 = double).");

            AutoScaleJakoSpawnsByPlayerCount = CreateTrackedEntry(_spawnScalingCategory, 
                "AutoScaleJakoSpawnsByPlayerCount",
                true,
                "Auto Scale Jako Spawns By Player Count",
                "When enabled, multiply jako spawn budgets by player count / 4 for sessions with more than 4 players (stacks with JakoSpawnMultiplier).");

            JakoSpawnMultiplier = CreateTrackedEntry(_spawnScalingCategory, 
                "JakoSpawnMultiplier",
                1f,
                "Jako Spawn Multiplier",
                "Total normal-monster threat budget for ambient dungeon spawns (1 = vanilla, 2 = double).");

            AutoScaleSpecialSpawnsByPlayerCount = CreateTrackedEntry(_spawnScalingCategory, 
                "AutoScaleSpecialSpawnsByPlayerCount",
                true,
                "Auto Scale Special Spawns By Player Count",
                "When enabled, multiply special spawn budgets by player count / 4 for sessions with more than 4 players (stacks with SpecialSpawnMultiplier).");

            SpecialSpawnMultiplier = CreateTrackedEntry(_spawnScalingCategory, 
                "SpecialSpawnMultiplier",
                1f,
                "Special Spawn Multiplier",
                "Special monster budget for periodic spawns and map-placed specials (1 = vanilla, 2 = double).");

            AutoScaleTrapSpawnsByPlayerCount = CreateTrackedEntry(_spawnScalingCategory, 
                "AutoScaleTrapSpawnsByPlayerCount",
                true,
                "Auto Scale Trap Spawns By Player Count",
                "When enabled, multiply trap spawn counts by player count / 4 for sessions with more than 4 players (stacks with TrapSpawnMultiplier).");

            TrapSpawnMultiplier = CreateTrackedEntry(_spawnScalingCategory, 
                "TrapSpawnMultiplier",
                1f,
                "Trap Spawn Multiplier",
                "Map-placed traps: activates unused alternate markers and schedules bonus encounters after trigger/kill (1 = vanilla, 2 = double).");

            MapPlacedEncounterDelayMinSeconds = CreateTrackedEntry(_spawnScalingCategory, 
                "MapPlacedEncounterDelayMinSeconds",
                5f,
                "Map-Placed Encounter Delay Min (seconds)",
                "Shortest wait after a map-placed enemy, trap, or loot marker is cleared before the next bonus encounter from scaling can appear there.");

            MapPlacedEncounterDelayMaxSeconds = CreateTrackedEntry(_spawnScalingCategory, 
                "MapPlacedEncounterDelayMaxSeconds",
                30f,
                "Map-Placed Encounter Delay Max (seconds)",
                "Longest wait for that random delay. Actual delay is picked between min and max.");

            MapPlacedEncounterMinPlayerDistanceMeters = CreateTrackedEntry(_spawnScalingCategory, 
                "MapPlacedEncounterMinPlayerDistanceMeters",
                10f,
                "Map-Placed Encounter Min Player Distance (m)",
                "After the delay, hold the spawn until no living players are within this radius of the marker. 0 = spawn as soon as the delay elapses.");

            AutoScaleOtherSpawnsByPlayerCount = CreateTrackedEntry(_spawnScalingCategory, 
                "AutoScaleOtherSpawnsByPlayerCount",
                true,
                "Auto Scale Other Spawns By Player Count",
                "When enabled, multiply other spawn counts by player count / 4 for sessions with more than 4 players (stacks with OtherSpawnMultiplier).");

            OtherSpawnMultiplier = CreateTrackedEntry(_spawnScalingCategory, 
                "OtherSpawnMultiplier",
                1f,
                "Other Spawn Multiplier",
                "Spawn multiplier for entities that are not mimics, bosses, jakos, specials, or traps.");

            EnableLootMultiplicator = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "EnableLootMultiplicator",
                false,
                "Enable Loot Multiplicator",
                "Scale how much loot appears in a run. Host only. See each Map/Drop/Trigger entry below for what it affects.");

            AutoScaleMapConsumableLootByPlayerCount = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "AutoScaleMapConsumableLootByPlayerCount",
                true,
                "Auto Scale Map Consumable Loot By Player Count",
                "Map loot = items placed on the dungeon map (spawn markers, shelves, floors). Consumables = ammo, healing, and other used-up items. When enabled, multiply by player count / 4 above 4 players (stacks with MapConsumableLootMultiplier).");

            MapConsumableLootMultiplier = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "MapConsumableLootMultiplier",
                1f,
                "Map Consumable Loot Multiplier",
                "Multiplier for consumables on map spawn points: stack size on fixed markers and respawn count at room load. Random pools scale via dungeon misc budget. 1 = vanilla, 2 = double.");

            AutoScaleMapEquipmentLootByPlayerCount = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "AutoScaleMapEquipmentLootByPlayerCount",
                true,
                "Auto Scale Map Equipment Loot By Player Count",
                "Map loot = items placed on the dungeon map. Equipment = tools, weapons, and gear you equip. When enabled, multiply by player count / 4 above 4 players (stacks with MapEquipmentLootMultiplier).");

            MapEquipmentLootMultiplier = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "MapEquipmentLootMultiplier",
                1f,
                "Map Equipment Loot Multiplier",
                "Multiplier for equipment on map spawn points: respawn count at room load and fixed-loot marker activation. Random pools scale via dungeon misc budget. 1 = vanilla, 2 = double.");

            AutoScaleMapMiscellanyLootByPlayerCount = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "AutoScaleMapMiscellanyLootByPlayerCount",
                true,
                "Auto Scale Map Miscellany Loot By Player Count",
                "Map loot = items placed on the dungeon map. Miscellany = other pickup items (keys, misc objects). When enabled, multiply by player count / 4 above 4 players (stacks with MapMiscellanyLootMultiplier).");

            MapMiscellanyLootMultiplier = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "MapMiscellanyLootMultiplier",
                1f,
                "Map Miscellany Loot Multiplier",
                "Multiplier for miscellany on map spawn points: respawn count at room load and fixed-loot marker activation. Random pools scale via dungeon misc budget. 1 = vanilla, 2 = double.");

            AutoScaleDropConsumableLootByPlayerCount = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "AutoScaleDropConsumableLootByPlayerCount",
                true,
                "Auto Scale Drop Consumable Loot By Player Count",
                "Drop loot = items from enemy death tables when killed. Consumables = ammo, healing, and other used-up items. When enabled, multiply by player count / 4 above 4 players (stacks with DropConsumableLootMultiplier).");

            DropConsumableLootMultiplier = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "DropConsumableLootMultiplier",
                1f,
                "Drop Consumable Loot Multiplier",
                "Multiplier for consumables in enemy death drops: extra weighted re-rolls from the drop table and consumable stack count on spawn. 1 = vanilla, 2 = double.");

            AutoScaleDropEquipmentLootByPlayerCount = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "AutoScaleDropEquipmentLootByPlayerCount",
                true,
                "Auto Scale Drop Equipment Loot By Player Count",
                "Drop loot = items from enemy death tables when killed. Equipment = tools, weapons, and gear you equip. When enabled, multiply by player count / 4 above 4 players (stacks with DropEquipmentLootMultiplier).");

            DropEquipmentLootMultiplier = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "DropEquipmentLootMultiplier",
                1f,
                "Drop Equipment Loot Multiplier",
                "Multiplier for equipment in enemy death drops: extra weighted re-rolls from the drop table. 1 = vanilla, 2 = double.");

            AutoScaleDropMiscellanyLootByPlayerCount = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "AutoScaleDropMiscellanyLootByPlayerCount",
                true,
                "Auto Scale Drop Miscellany Loot By Player Count",
                "Drop loot = items from enemy death tables when killed. Miscellany = other pickup items. When enabled, multiply by player count / 4 above 4 players (stacks with DropMiscellanyLootMultiplier).");

            DropMiscellanyLootMultiplier = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "DropMiscellanyLootMultiplier",
                1f,
                "Drop Miscellany Loot Multiplier",
                "Multiplier for miscellany in enemy death drops: extra weighted re-rolls from the drop table. 1 = vanilla, 2 = double.");

            AutoScaleTriggerConsumableLootByPlayerCount = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "AutoScaleTriggerConsumableLootByPlayerCount",
                true,
                "Auto Scale Trigger Consumable Loot By Player Count",
                "Trigger loot = items spawned by map events/trigger volumes (EventAction spawns only). Consumables = ammo, healing, and other used-up items. When enabled, multiply by player count / 4 above 4 players (stacks with TriggerConsumableLootMultiplier).");

            TriggerConsumableLootMultiplier = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "TriggerConsumableLootMultiplier",
                1f,
                "Trigger Consumable Loot Multiplier",
                "Multiplier for consumables from map events/triggers: extra weighted picks and consumable stack count on spawn. 1 = vanilla, 2 = double.");

            AutoScaleTriggerEquipmentLootByPlayerCount = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "AutoScaleTriggerEquipmentLootByPlayerCount",
                true,
                "Auto Scale Trigger Equipment Loot By Player Count",
                "Trigger loot = items spawned by map events/trigger volumes (EventAction spawns only). Equipment = tools, weapons, and gear you equip. When enabled, multiply by player count / 4 above 4 players (stacks with TriggerEquipmentLootMultiplier).");

            TriggerEquipmentLootMultiplier = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "TriggerEquipmentLootMultiplier",
                1f,
                "Trigger Equipment Loot Multiplier",
                "Multiplier for equipment from map events/triggers: extra weighted picks from the event item table. 1 = vanilla, 2 = double.");

            AutoScaleTriggerMiscellanyLootByPlayerCount = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "AutoScaleTriggerMiscellanyLootByPlayerCount",
                true,
                "Auto Scale Trigger Miscellany Loot By Player Count",
                "Trigger loot = items spawned by map events/trigger volumes (EventAction spawns only). Miscellany = other pickup items. When enabled, multiply by player count / 4 above 4 players (stacks with TriggerMiscellanyLootMultiplier).");

            TriggerMiscellanyLootMultiplier = CreateTrackedEntry(_lootMultiplicatorCategory, 
                "TriggerMiscellanyLootMultiplier",
                1f,
                "Trigger Miscellany Loot Multiplier",
                "Multiplier for miscellany from map events/triggers: extra weighted picks from the event item table. 1 = vanilla, 2 = double.");

            LootItemFilterMode = CreateTrackedEntry(_lootMultiplicatorCategory,
                "LootItemFilterMode",
                "All",
                "Loot Item Filter Mode",
                "All = every item can be scaled; AllowlistOnly = only comma-separated master IDs in LootAllowlist; BlocklistOnly = all items except LootBlocklist.");

            LootAllowlist = CreateTrackedEntry(_lootMultiplicatorCategory,
                "LootAllowlist",
                "",
                "Loot Allowlist",
                "Comma-separated item master IDs (e.g. 12345,67890). Used when LootItemFilterMode is AllowlistOnly. See docs/LOOT_ITEM_IDS.md in the repo for the full list.");

            LootBlocklist = CreateTrackedEntry(_lootMultiplicatorCategory,
                "LootBlocklist",
                "",
                "Loot Blocklist",
                "Comma-separated item master IDs to exclude from scaling. Used when LootItemFilterMode is BlocklistOnly. See docs/LOOT_ITEM_IDS.md in the repo for the full list.");

            ConvertFakeActorDyingDropChancePercent = CreateTrackedEntry(_lootMultiplicatorCategory,
                "ConvertFakeActorDyingDropChancePercent",
                30,
                "Convert Fake Death Drops To Real Chance",
                "Chance (0-100) that fake items dropped on enemy death (ActorDying, e.g. mimic inventory decoys) become real pickup loot. 0 = vanilla (fake items vanish on grab), 100 = always real. Monster drop-table loot is already real.");

            EnableMoneyMultiplier = CreateTrackedEntry(_moneyMultiplierCategory, 
                "EnableMoneyMultiplier",
                false,
                "Enable Money Multiplier",
                "Scale startup money, round goal quota, scrap/sell values, shop buy prices, shop item count, and reinforce costs. Host only.");

            AutoScaleStartupMoneyByPlayerCount = CreateTrackedEntry(_moneyMultiplierCategory, 
                "AutoScaleStartupMoneyByPlayerCount",
                true,
                "Auto Scale Startup Money By Player Count",
                "When enabled, multiply startup money by player count / 4 for sessions with more than 4 players (stacks with StartupMoneyMultiplier).");

            StartupMoneyMultiplier = CreateTrackedEntry(_moneyMultiplierCategory, 
                "StartupMoneyMultiplier",
                1f,
                "Startup Money Multiplier",
                "Starting maintenance-room currency on a new game or session reset (1 = vanilla, 2 = double).");

            AutoScaleRoundGoalMoneyByPlayerCount = CreateTrackedEntry(_moneyMultiplierCategory, 
                "AutoScaleRoundGoalMoneyByPlayerCount",
                true,
                "Auto Scale Round Goal Money By Player Count",
                "When enabled, multiply the stage target currency (quota) by player count / 4 for sessions with more than 4 players (stacks with RoundGoalMoneyMultiplier).");

            RoundGoalMoneyMultiplier = CreateTrackedEntry(_moneyMultiplierCategory, 
                "RoundGoalMoneyMultiplier",
                1f,
                "Round Goal Money Multiplier",
                "Target currency required to finish a stage (1 = vanilla, 2 = double).");

            AutoScaleScrapSellValueByPlayerCount = CreateTrackedEntry(_moneyMultiplierCategory, 
                "AutoScaleScrapSellValueByPlayerCount",
                true,
                "Auto Scale Scrap Sell Value By Player Count",
                "When enabled, multiply item scrap/sell values by player count / 4 for sessions with more than 4 players (stacks with ScrapSellValueMultiplier).");

            ScrapSellValueMultiplier = CreateTrackedEntry(_moneyMultiplierCategory, 
                "ScrapSellValueMultiplier",
                1f,
                "Scrap Sell Value Multiplier",
                "Currency earned when scrapping items and item value counted toward the tram quota (1 = vanilla, 2 = double).");

            AutoScaleShopBuyPriceByPlayerCount = CreateTrackedEntry(_moneyMultiplierCategory, 
                "AutoScaleShopBuyPriceByPlayerCount",
                true,
                "Auto Scale Shop Buy Price By Player Count",
                "When enabled, multiply maintenance shop buy prices by player count / 4 for sessions with more than 4 players (stacks with ShopBuyPriceMultiplier).");

            ShopBuyPriceMultiplier = CreateTrackedEntry(_moneyMultiplierCategory, 
                "ShopBuyPriceMultiplier",
                1f,
                "Shop Buy Price Multiplier",
                "Maintenance shop and vending-machine kiosk purchase cost multiplier (1 = vanilla, 2 = double). Applied when shop items are initialized each maintenance round.");

            AutoScaleShopItemsByPlayerCount = CreateTrackedEntry(_moneyMultiplierCategory, 
                "AutoScaleShopItemsByPlayerCount",
                true,
                "Auto Scale Shop Items By Player Count",
                "When enabled, multiply maintenance shop item count by player count / 4 for sessions with more than 4 players (stacks with ShopItemsMultiplier).");

            ShopItemsMultiplier = CreateTrackedEntry(_moneyMultiplierCategory, 
                "ShopItemsMultiplier",
                1f,
                "Shop Items Multiplier",
                "Number of unique items offered in the maintenance shop (1 = vanilla, 2 = double). Extra items are rolled from vending-machine shop groups on the map.");

            ShopDiscountMinPercent = CreateTrackedEntry(_moneyMultiplierCategory, 
                "ShopDiscountMinPercent",
                0,
                "Shop Discount Min Percent",
                "Minimum shop discount percentage when a discount is rolled (0-100). Only used when ShopDiscountChancePercent is above 0.");

            ShopDiscountMaxPercent = CreateTrackedEntry(_moneyMultiplierCategory, 
                "ShopDiscountMaxPercent",
                100,
                "Shop Discount Max Percent",
                "Maximum shop discount percentage when a discount is rolled (0-100). Must be >= ShopDiscountMinPercent.");

            ShopDiscountChancePercent = CreateTrackedEntry(_moneyMultiplierCategory, 
                "ShopDiscountChancePercent",
                0,
                "Shop Discount Chance Percent",
                "Chance per shop item to receive a discount between min and max percent (0 = vanilla shop discounts, 100 = every item discounted).");

            AutoScaleReinforcePriceByPlayerCount = CreateTrackedEntry(_moneyMultiplierCategory, 
                "AutoScaleReinforcePriceByPlayerCount",
                true,
                "Auto Scale Reinforce Price By Player Count",
                "When enabled, multiply item reinforcement costs by player count / 4 for sessions with more than 4 players (stacks with ReinforcePriceMultiplier).");

            ReinforcePriceMultiplier = CreateTrackedEntry(_moneyMultiplierCategory, 
                "ReinforcePriceMultiplier",
                1f,
                "Reinforce Price Multiplier",
                "Maintenance item reinforcement cost multiplier (1 = vanilla, 2 = double).");

            EnableDungeonTime = CreateTrackedEntry(_dungeonTimeCategory, 
                "EnableDungeonTime",
                false,
                "Enable Dungeon Time",
                "Extend dungeon shift length on the host when player count exceeds the baseline.");

            DungeonTimeBaselinePlayerCount = CreateTrackedEntry(_dungeonTimeCategory, 
                "DungeonTimeBaselinePlayerCount",
                4,
                "Dungeon Time Baseline Player Count",
                "No extra shift time at or below this player count (vanilla is 4). Minimum is 1.");

            ExtraShiftSecondsPerPlayerAboveBaseline = CreateTrackedEntry(_dungeonTimeCategory,
                "ExtraShiftSecondsPerPlayerAboveBaseline",
                10f,
                "Extra Shift Seconds Per Player Above Baseline",
                "Real seconds added to the shift deadline for each player above the baseline. Minimum is 0.");

            EnableRoomEntryDelay = CreateTrackedEntry(_roomEntryDelayCategory,
                "EnableRoomEntryDelay",
                false,
                "Enable Room Entry Delay",
                "Multiply hold/teleport timing for E-interaction room entry teleporters on the host.");

            RoomEntryDelayMultiplier = CreateTrackedEntry(_roomEntryDelayCategory,
                "RoomEntryDelayMultiplier",
                1f,
                "Room Entry Delay Multiplier",
                "Multiplier for room entry timing at teleporters and dungeon doors (1 = vanilla, 2 = double). Host only.");

            EnableMimicTuning = CreateTrackedEntry(_mimicTuningCategory,
                "EnableMimicTuning",
                false,
                "Enable Mimic Tuning",
                "Tune dead-player mimic possession speak duration and cooldown on the host.");

            RandomizeMimicPossessionDuration = CreateTrackedEntry(_mimicTuningCategory,
                "RandomizeMimicPossessionDuration",
                false,
                "Randomize Mimic Possession Duration",
                "Roll a random speak window per E-possession between min and max seconds below. Host only.");

            MimicPossessionMinTimeSeconds = CreateTrackedEntry(_mimicTuningCategory,
                "MimicPossessionMinTimeSeconds",
                Features.MimicTuning.MimicTuningResolver.VanillaPossessionDurationSeconds,
                "Mimic Possession Min Time (seconds)",
                "Minimum rolled speak duration in seconds (vanilla is 12). Host only.");

            MimicPossessionMaxTimeSeconds = CreateTrackedEntry(_mimicTuningCategory,
                "MimicPossessionMaxTimeSeconds",
                Features.MimicTuning.MimicTuningResolver.VanillaPossessionDurationSeconds,
                "Mimic Possession Max Time (seconds)",
                "Maximum rolled speak duration in seconds (vanilla is 12). Host only.");

            MimicPossessionCooltimeMultiplier = CreateTrackedEntry(_mimicTuningCategory,
                "MimicPossessionCooltimeMultiplier",
                1f,
                "Mimic Possession Cooltime Multiplier",
                "Multiplier for wait time after mimic possession before the next E-possession (1 = vanilla). Host only.");

            EnablePlayerTuning = CreateTrackedEntry(_playerTuningCategory,
                "EnablePlayerTuning",
                false,
                "Enable Player Tuning",
                "Scale player move speed, stamina, and carry weight on the host. Joining clients do not need the mod.");

            MoveSpeedMultiplier = CreateTrackedEntry(_playerTuningCategory,
                "MoveSpeedMultiplier",
                1f,
                "Move Speed Multiplier",
                "Scales walk and run base speed (1 = vanilla, 2 = double). Host only.");

            MaxStaminaMultiplier = CreateTrackedEntry(_playerTuningCategory,
                "MaxStaminaMultiplier",
                1f,
                "Max Stamina Multiplier",
                "Scales maximum stamina (1 = vanilla, 2 = double). Host only.");

            StaminaDrainMultiplier = CreateTrackedEntry(_playerTuningCategory,
                "StaminaDrainMultiplier",
                1f,
                "Stamina Drain Multiplier",
                "Scales sprint stamina cost per tick (1 = vanilla, 0.5 = half drain). Host only.");

            StaminaRegenMultiplier = CreateTrackedEntry(_playerTuningCategory,
                "StaminaRegenMultiplier",
                1f,
                "Stamina Regen Multiplier",
                "Scales stamina recovered per regen tick (1 = vanilla, 2 = double). Host only.");

            StaminaRegenDelayMultiplier = CreateTrackedEntry(_playerTuningCategory,
                "StaminaRegenDelayMultiplier",
                1f,
                "Stamina Regen Delay Multiplier",
                "Scales wait time before stamina regen starts after sprinting (1 = vanilla, 0.5 = regen starts sooner). Host only.");

            MaxCarryWeightMultiplier = CreateTrackedEntry(_playerTuningCategory,
                "MaxCarryWeightMultiplier",
                1f,
                "Max Carry Weight Multiplier",
                "Scales carry capacity before encumbrance slows movement (1 = vanilla, 2 = double capacity). Host only.");

            EnableDungeonRandomizer = CreateTrackedEntry(_dungeonRandomizerCategory,
                "EnableDungeonRandomizer",
                false,
                "Enable Dungeon Randomizer",
                "Randomize dungeon selection on the host: tram dungeon pick, layout flow, map variant, and procedural seed. Host only.");

            RandomizeDungeonPick = CreateTrackedEntry(_dungeonRandomizerCategory, 
                "RandomizeDungeonPick",
                true,
                "Randomize Dungeon Pick",
                "Override which dungeon master ID is rolled on the tram.");

            DungeonPickPoolMode = CreateTrackedEntry(_dungeonRandomizerCategory, 
                "DungeonPickPoolMode",
                "WidenVanilla",
                "Dungeon Pick Pool Mode",
                "WidenVanilla = keep cycle weights but allow repeats sooner; AllActiveUniform = pick uniformly from all active dungeons (ignores cycle table).");

            DungeonAllowlist = CreateTrackedEntry(_dungeonRandomizerCategory, 
                "DungeonAllowlist",
                "",
                "Dungeon Allowlist",
                "Comma-separated dungeon master IDs. When non-empty, only these IDs are eligible.");

            DungeonBlocklist = CreateTrackedEntry(_dungeonRandomizerCategory, 
                "DungeonBlocklist",
                "",
                "Dungeon Blocklist",
                "Comma-separated dungeon master IDs to exclude from the pool.");

            IgnoreDungeonExcludeList = CreateTrackedEntry(_dungeonRandomizerCategory, 
                "IgnoreDungeonExcludeList",
                true,
                "Ignore Dungeon Exclude List",
                "When using WidenVanilla, do not exclude recently played dungeons from the tram roll.");

            RandomizeLayoutFlow = CreateTrackedEntry(_dungeonRandomizerCategory, 
                "RandomizeLayoutFlow",
                true,
                "Randomize Layout Flow",
                "Pick DunGen layout flows uniformly from each dungeon's candidates instead of using weighted vanilla rolls.");

            RandomizeMapVariant = CreateTrackedEntry(_dungeonRandomizerCategory, 
                "RandomizeMapVariant",
                true,
                "Randomize Map Variant",
                "Pick map variants uniformly from each dungeon's MapIDs instead of vanilla selection.");

            RandomizeDungeonSeed = CreateTrackedEntry(_dungeonRandomizerCategory, 
                "RandomizeDungeonSeed",
                true,
                "Randomize Dungeon Seed",
                "Replace the procedural dungeon seed with a new random value when a dungeon is chosen.");

            EnableWebDashboard = CreateTrackedEntry(_webDashboardCategory, 
                "EnableWebDashboard",
                true,
                "Enable Web Dashboard",
                "Serve a local web UI for connected players and host moderation. Default bind is loopback only.");

            WebDashboardListenAddress = CreateTrackedEntry(_webDashboardCategory, 
                "WebDashboardListenAddress",
                "127.0.0.1",
                "Listen Address",
                "HTTP bind address. Use 127.0.0.1 for local-only access.");

            WebDashboardListenPort = CreateTrackedEntry(_webDashboardCategory, 
                "WebDashboardListenPort",
                8001,
                "Listen Port",
                "TCP port for the local web dashboard.");

            if (MaxPlayers.Value < 1)
            {
                logger.Warning("MaxPlayers must be at least 1; resetting to 1.");
                MaxPlayers.Value = 1;
            }

            if (JoinConnectionGraceSeconds.Value < 1)
            {
                logger.Warning("JoinConnectionGraceSeconds must be at least 1; resetting to 1.");
                JoinConnectionGraceSeconds.Value = 1;
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
            ShowPlayerAnnouncements.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            ModToastDurationSeconds.OnEntryValueChanged.Subscribe((_, value) =>
            {
                if (value < 1f)
                {
                    logger.Warning("ModToastDurationSeconds must be at least 1; resetting to 1.");
                    ModToastDurationSeconds.Value = 1f;
                    return;
                }

                NotifyChanged();
            });
            EnableJoinAnytime.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());

            EnableExtendedSaveSlots.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());

            JoinConnectionGraceSeconds.OnEntryValueChanged.Subscribe((_, value) =>
            {
                if (value < 1)
                {
                    logger.Warning("JoinConnectionGraceSeconds must be at least 1; resetting to 1.");
                    JoinConnectionGraceSeconds.Value = 1;
                    return;
                }

                NotifyChanged();
            });
            EnableSpawnScaling.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            AutoScaleMimicSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            AutoScaleBossSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            AutoScaleJakoSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            AutoScaleSpecialSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            AutoScaleTrapSpawnsByPlayerCount.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            MapPlacedEncounterDelayMinSeconds.OnEntryValueChanged.Subscribe((_, value) => OnMapPlacedEncounterDelayChanged(logger, value, MapPlacedEncounterDelayMinSeconds));
            MapPlacedEncounterDelayMaxSeconds.OnEntryValueChanged.Subscribe((_, value) => OnMapPlacedEncounterDelayChanged(logger, value, MapPlacedEncounterDelayMaxSeconds));
            MapPlacedEncounterMinPlayerDistanceMeters.OnEntryValueChanged.Subscribe((_, value) => OnMapPlacedEncounterMinPlayerDistanceChanged(logger, value));
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
            LootItemFilterMode.OnEntryValueChanged.Subscribe((_, value) => OnLootItemFilterModeChanged(logger, value));
            LootAllowlist.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            LootBlocklist.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            ConvertFakeActorDyingDropChancePercent.OnEntryValueChanged.Subscribe((_, value) =>
                OnFakeActorDyingDropChancePercentChanged(logger, value));

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

            EnableRoomEntryDelay.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            RoomEntryDelayMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnRoomEntryDelayMultiplierChanged(logger, value));

            EnableMimicTuning.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            RandomizeMimicPossessionDuration.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            MimicPossessionMinTimeSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnMimicPossessionDurationSecondsChanged(logger, value, MimicPossessionMinTimeSeconds));
            MimicPossessionMaxTimeSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnMimicPossessionDurationSecondsChanged(logger, value, MimicPossessionMaxTimeSeconds));
            MimicPossessionCooltimeMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnMimicPossessionCooltimeMultiplierChanged(logger, value));

            EnablePlayerTuning.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            MoveSpeedMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, MoveSpeedMultiplier));
            MaxStaminaMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, MaxStaminaMultiplier));
            StaminaDrainMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, StaminaDrainMultiplier));
            StaminaRegenMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, StaminaRegenMultiplier));
            StaminaRegenDelayMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, StaminaRegenDelayMultiplier));
            MaxCarryWeightMultiplier.OnEntryValueChanged.Subscribe((_, value) =>
                OnPlayerTuningMultiplierChanged(logger, value, MaxCarryWeightMultiplier));

            EnableDungeonRandomizer.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            RandomizeDungeonPick.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            DungeonPickPoolMode.OnEntryValueChanged.Subscribe((_, value) => OnDungeonPickPoolModeChanged(logger, value));
            DungeonAllowlist.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            DungeonBlocklist.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            IgnoreDungeonExcludeList.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            RandomizeLayoutFlow.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            RandomizeMapVariant.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            RandomizeDungeonSeed.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());

            EnableWebDashboard.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            WebDashboardListenAddress.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());
            WebDashboardListenPort.OnEntryValueChanged.Subscribe((_, value) =>
            {
                if (value is < 1 or > 65535)
                {
                    logger.Warning("WebDashboardListenPort must be between 1 and 65535; resetting to 8001.");
                    WebDashboardListenPort.Value = 8001;
                    return;
                }

                NotifyChanged();
            });

            EnableDebugLogging.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged());

            RegisterFloatEntries();
            MigrateLegacyMapPlacedEncounterKeys(logger);
            MigrateLegacyMimicPossessionTimeKeys(logger);
            ModConfigFloatHelper.SanitizeAll(FloatEntries);
            NormalizeSavedFloats();
            ModConfigRegistry.Rebuild();

            IsInitialized = true;
        }

        /// <summary>Persist current preference values to <see cref="FilePath"/>.</summary>
        public static void SaveToFile()
        {
            ModConfigRegistry.SaveToFile();
        }

        /// <summary>Reload in-memory entries from the global config file on disk.</summary>
        internal static void ReloadGlobalFromFile()
        {
            if (!IsInitialized)
            {
                return;
            }

            MainCategory.LoadFromFile(false);
            SanitizeFloatEntries();
        }

        /// <summary>Notify listeners that runtime config changed without persisting global file.</summary>
        internal static void NotifyRuntimeChanged()
        {
            NotifyChanged();
        }

        /// <summary>Update a single preference by section and key. Validation runs through existing entry change handlers.</summary>
        public static bool TrySetEntryValue(string sectionId, string key, string value, out string? error)
        {
            return ModConfigRegistry.TrySetEntryValue(sectionId, key, value, out error);
        }

        /// <summary>Called after MelonLoader reloads the config file from disk.</summary>
        internal static void NotifyFileReloaded()
        {
            NotifyChanged();
        }

        public static void NormalizeSavedFloats()
        {
            ModConfigFloatHelper.NormalizeSavedFloats(FilePath, FloatEntries);
        }

        internal static void SanitizeFloatEntries()
        {
            ModConfigFloatHelper.SanitizeAll(FloatEntries);
        }

        private static void RegisterFloatEntries()
        {
            FloatEntries.AddRange(
            [
                MimicSpawnMultiplier,
                BossSpawnMultiplier,
                JakoSpawnMultiplier,
                SpecialSpawnMultiplier,
                TrapSpawnMultiplier,
                MapPlacedEncounterDelayMinSeconds,
                MapPlacedEncounterDelayMaxSeconds,
                MapPlacedEncounterMinPlayerDistanceMeters,
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
                RoomEntryDelayMultiplier,
                MimicPossessionMinTimeSeconds,
                MimicPossessionMaxTimeSeconds,
                MimicPossessionCooltimeMultiplier,
                MoveSpeedMultiplier,
                MaxStaminaMultiplier,
                StaminaDrainMultiplier,
                StaminaRegenMultiplier,
                StaminaRegenDelayMultiplier,
                MaxCarryWeightMultiplier,
            ]);
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

        private static void OnRoomEntryDelayMultiplierChanged(MelonLogger.Instance logger, float value)
        {
            if (value < Features.RoomEntryDelay.RoomEntryDelayResolver.MinMultiplier)
            {
                logger.Warning(
                    $"RoomEntryDelayMultiplier must be at least {Features.RoomEntryDelay.RoomEntryDelayResolver.MinMultiplier}; resetting.");
                RoomEntryDelayMultiplier.Value = Features.RoomEntryDelay.RoomEntryDelayResolver.MinMultiplier;
                return;
            }

            if (value > Features.RoomEntryDelay.RoomEntryDelayResolver.MaxMultiplier)
            {
                logger.Warning(
                    $"RoomEntryDelayMultiplier must be at most {Features.RoomEntryDelay.RoomEntryDelayResolver.MaxMultiplier}; resetting.");
                RoomEntryDelayMultiplier.Value = Features.RoomEntryDelay.RoomEntryDelayResolver.MaxMultiplier;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(RoomEntryDelayMultiplier);
            NotifyChanged();
        }

        private static void OnMimicPossessionDurationSecondsChanged(
            MelonLogger.Instance logger,
            float value,
            MelonPreferences_Entry<float> entry)
        {
            if (value < Features.MimicTuning.MimicTuningResolver.MinDurationSeconds)
            {
                logger.Warning(
                    $"{entry.Identifier} must be at least {Features.MimicTuning.MimicTuningResolver.MinDurationSeconds}; resetting.");
                entry.Value = Features.MimicTuning.MimicTuningResolver.MinDurationSeconds;
                return;
            }

            if (value > Features.MimicTuning.MimicTuningResolver.MaxDurationSeconds)
            {
                logger.Warning(
                    $"{entry.Identifier} must be at most {Features.MimicTuning.MimicTuningResolver.MaxDurationSeconds}; resetting.");
                entry.Value = Features.MimicTuning.MimicTuningResolver.MaxDurationSeconds;
                return;
            }

            float min = MimicPossessionMinTimeSeconds.Value;
            float max = MimicPossessionMaxTimeSeconds.Value;
            if (max < min)
            {
                logger.Warning(
                    "MimicPossessionMaxTimeSeconds must be >= MimicPossessionMinTimeSeconds; syncing max to min.");
                MimicPossessionMaxTimeSeconds.Value = min;
            }

            ModConfigFloatHelper.SanitizeEntry(entry);
            NotifyChanged();
        }

        private static void OnMimicPossessionCooltimeMultiplierChanged(MelonLogger.Instance logger, float value)
        {
            if (value < Features.MimicTuning.MimicTuningResolver.MinCooltimeMultiplier)
            {
                logger.Warning(
                    $"MimicPossessionCooltimeMultiplier must be at least {Features.MimicTuning.MimicTuningResolver.MinCooltimeMultiplier}; resetting.");
                MimicPossessionCooltimeMultiplier.Value = Features.MimicTuning.MimicTuningResolver.MinCooltimeMultiplier;
                return;
            }

            if (value > Features.MimicTuning.MimicTuningResolver.MaxCooltimeMultiplier)
            {
                logger.Warning(
                    $"MimicPossessionCooltimeMultiplier must be at most {Features.MimicTuning.MimicTuningResolver.MaxCooltimeMultiplier}; resetting.");
                MimicPossessionCooltimeMultiplier.Value = Features.MimicTuning.MimicTuningResolver.MaxCooltimeMultiplier;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(MimicPossessionCooltimeMultiplier);
            NotifyChanged();
        }

        private static void MigrateLegacyMapPlacedEncounterKeys(MelonLogger.Instance logger)
        {
            bool migrated = false;
            migrated |= TryMigrateLegacyFloatKey("FixedSpawnRespawnDelayMinSeconds", MapPlacedEncounterDelayMinSeconds);
            migrated |= TryMigrateLegacyFloatKey("FixedSpawnRespawnDelayMaxSeconds", MapPlacedEncounterDelayMaxSeconds);
            migrated |= TryMigrateLegacyFloatKey("FixedSpawnRespawnMinPlayerDistanceMeters", MapPlacedEncounterMinPlayerDistanceMeters);

            if (migrated)
            {
                logger.Msg(
                    "Spawn Scaling config migrated — FixedSpawnRespawn* keys copied to MapPlacedEncounter* keys.");
            }
        }

        private static void MigrateLegacyMimicPossessionTimeKeys(MelonLogger.Instance logger)
        {
            bool migrated = false;
            migrated |= TryMigrateLegacyMimicPossessionTimeMultiplier(
                "MimicPossessionMinTimeMultiplier",
                MimicPossessionMinTimeSeconds);
            migrated |= TryMigrateLegacyMimicPossessionTimeMultiplier(
                "MimicPossessionMaxTimeMultiplier",
                MimicPossessionMaxTimeSeconds);

            if (migrated)
            {
                logger.Msg(
                    "Mimic Tuning config migrated — possession min/max multiplier keys converted to seconds.");
            }
        }

        private static bool TryMigrateLegacyFloatKey(
            string legacyKey,
            MelonPreferences_Entry<float> targetEntry)
        {
            if (_spawnScalingCategory.GetEntry<float>(legacyKey) is not MelonPreferences_Entry<float> legacyEntry)
            {
                return false;
            }

            targetEntry.Value = legacyEntry.Value;
            return true;
        }

        private static bool TryMigrateLegacyMimicPossessionTimeMultiplier(
            string legacyKey,
            MelonPreferences_Entry<float> targetEntry)
        {
            if (_mimicTuningCategory.GetEntry<float>(legacyKey) is not MelonPreferences_Entry<float> legacyEntry)
            {
                return false;
            }

            targetEntry.Value = legacyEntry.Value
                * Features.MimicTuning.MimicTuningResolver.VanillaPossessionDurationSeconds;
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

            float min = MapPlacedEncounterDelayMinSeconds.Value;
            float max = MapPlacedEncounterDelayMaxSeconds.Value;
            if (max < min)
            {
                logger.Warning("MapPlacedEncounterDelayMaxSeconds must be >= MapPlacedEncounterDelayMinSeconds; syncing max to min.");
                MapPlacedEncounterDelayMaxSeconds.Value = min;
            }

            ModConfigFloatHelper.SanitizeEntry(entry);
            NotifyChanged();
        }

        private static void OnMapPlacedEncounterMinPlayerDistanceChanged(MelonLogger.Instance logger, float value)
        {
            if (value < 0f)
            {
                logger.Warning("MapPlacedEncounterMinPlayerDistanceMeters must be >= 0; resetting to 0.");
                MapPlacedEncounterMinPlayerDistanceMeters.Value = 0f;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(MapPlacedEncounterMinPlayerDistanceMeters);
            NotifyChanged();
        }

        private static void OnPlayerTuningMultiplierChanged(
            MelonLogger.Instance logger,
            float value,
            MelonPreferences_Entry<float> entry)
        {
            if (value < Features.PlayerTuning.PlayerTuningResolver.MinMultiplier)
            {
                logger.Warning(
                    $"{entry.Identifier} must be at least {Features.PlayerTuning.PlayerTuningResolver.MinMultiplier}; resetting.");
                entry.Value = Features.PlayerTuning.PlayerTuningResolver.MinMultiplier;
                return;
            }

            if (value > Features.PlayerTuning.PlayerTuningResolver.MaxMultiplier)
            {
                logger.Warning(
                    $"{entry.Identifier} must be at most {Features.PlayerTuning.PlayerTuningResolver.MaxMultiplier}; resetting.");
                entry.Value = Features.PlayerTuning.PlayerTuningResolver.MaxMultiplier;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(entry);
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

        private static void OnFakeActorDyingDropChancePercentChanged(MelonLogger.Instance logger, int value)
        {
            if (value is < 0 or > 100)
            {
                logger.Warning("ConvertFakeActorDyingDropChancePercent must be 0-100; resetting to 30.");
                ConvertFakeActorDyingDropChancePercent.Value = 30;
                return;
            }

            NotifyChanged();
        }

        private static void OnLootItemFilterModeChanged(MelonLogger.Instance logger, string value)
        {
            if (!string.Equals(value, "All", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "AllowlistOnly", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "BlocklistOnly", StringComparison.OrdinalIgnoreCase))
            {
                logger.Warning("LootItemFilterMode must be All, AllowlistOnly, or BlocklistOnly; resetting to All.");
                LootItemFilterMode.Value = "All";
                return;
            }

            NotifyChanged();
        }


        private static MelonPreferences_Category CreateCategory(string id, string displayName)
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory(id, displayName);
            category.SetFilePath(FilePath);
            ModConfigRegistry.TrackCategory(category);
            return category;
        }

        private static MelonPreferences_Entry<T> CreateTrackedEntry<T>(
            MelonPreferences_Category category,
            string identifier,
            T defaultValue,
            string displayName,
            string description)
        {
            MelonPreferences_Entry<T> entry = category.CreateEntry(
                identifier, defaultValue, displayName, description);
            ModConfigRegistry.TrackEntry(entry);
            return entry;
        }

        private static void NotifyChanged()
        {
            ModConfigRegistry.NotifyRuntimeChange();
            Changed?.Invoke();
        }
    }
}
