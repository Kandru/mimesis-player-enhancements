using System;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using MimesisPlayerEnhancement.Features.DungeonRandomizer;
using MimesisPlayerEnhancement.Features.DungeonTime;
using MimesisPlayerEnhancement.Features.ExtendedSaveSlots;
using MimesisPlayerEnhancement.Features.LootMultiplicator;
using MimesisPlayerEnhancement.Features.MoneyMultiplier;
using MimesisPlayerEnhancement.Features.MorePlayers;
using MimesisPlayerEnhancement.Features.MoreVoices;
using MimesisPlayerEnhancement.Features.PlayerAnnouncements;
using MimesisPlayerEnhancement.Features.PlayerTuning;
using MimesisPlayerEnhancement.Features.SpawnScaling;
using MimesisPlayerEnhancement.Features.WebDashboard;

namespace MimesisPlayerEnhancement
{
    /// <summary>
    /// MelonPreferences-backed configuration. Values are stored in
    /// UserData/MimesisPlayerEnhancement.cfg (separate from the global MelonPreferences.cfg).
    /// Global settings use the [MimesisPlayerEnhancement] section; each feature has
    /// its own [MimesisPlayerEnhancement_FeatureName] section registered by its
    /// per-feature config class (e.g. <c>SpawnScalingConfig</c>). Registration
    /// order is driven from <see cref="Initialize"/> to keep the TOML layout stable.
    /// </summary>
    public static class ModConfig
    {
        private const string MainCategoryId = "MimesisPlayerEnhancement";

        /// <summary>Fired when preference values change (UI save, file reload, or programmatic update).</summary>
        public static event Action<ModConfigChangeInfo>? Changed
        {
            add => ModConfigChangeTracker.Changed += value;
            remove => ModConfigChangeTracker.Changed -= value;
        }

        /// <summary>Increments whenever configuration values change at runtime.</summary>
        public static int Version => ModConfigRegistry.Version;

        public static bool IsInitialized { get; private set; }

        public static string FilePath { get; private set; } = "";

        public static MelonPreferences_Category MainCategory { get; private set; } = null!;

        public static MelonPreferences_Entry<bool> EnableMorePlayers { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> MaxPlayers { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> EnableExtendedSpectatorPlayerList { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableMoreVoices { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> MaxIndoorVoiceEvents { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> MaxDeathMatchVoiceEvents { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> MaxOutdoorVoiceEvents { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnablePersistence { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableStatistics { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> SessionReconnectGraceMinutes { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> ShowStatisticsToasts { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> ShowPlayerAnnouncements { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> ModToastDurationSeconds { get; private set; } = null!;

        public static MelonPreferences_Entry<bool> EnableJoinAnytime { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> JoinConnectionGraceSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> JoinTramRouteRetrySeconds { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableSpawnScaling { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> SpawnScalingPlayerCountScaleRate { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> AutoScaleMimicSpawnsByPlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MimicSpawnMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> AutoScaleBossSpawnsByPlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> BossSpawnMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> AutoScaleJakoSpawnsByPlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> JakoSpawnMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> AutoScaleSpecialSpawnsByPlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> SpecialSpawnMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> AutoScaleTrapSpawnsByPlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> TrapSpawnMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MapPlacedEncounterDelayMinSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MapPlacedEncounterDelayMaxSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MapPlacedEncounterMinPlayerDistanceMeters { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> AutoScaleOtherSpawnsByPlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> OtherSpawnMultiplier { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableLootMultiplicator { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> LootMultiplicatorPlayerCountScaleRate { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> AutoScaleMapLootByPlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MapLootMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> AutoScaleDropLootByPlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> DropLootMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> LootItemFilterMode { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> LootAllowlist { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> LootBlocklist { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> ConvertFakeActorDyingDropChancePercent { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableMoneyMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MoneyMultiplierPlayerCountScaleRate { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> AutoScaleStartupMoneyByPlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> StartupMoneyMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> AutoScaleRoundGoalMoneyByPlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> RoundGoalMoneyMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> AutoScaleScrapSellValueByPlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> ScrapSellValueMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> AutoScaleShopBuyPriceByPlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> ShopBuyPriceMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> ShopDiscountMinPercent { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> ShopDiscountMaxPercent { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> ShopDiscountChancePercent { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> AutoScaleReinforcePriceByPlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> ReinforcePriceMultiplier { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableDungeonTime { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> DungeonTimeBaselinePlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> ExtraShiftSecondsPerPlayerAboveBaseline { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableDeadPlayerFeatures { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> EnableMimicPossessionTuning { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> RandomizeMimicPossessionDuration { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MimicPossessionMinTimeSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MimicPossessionMaxTimeSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MimicPossessionCooltimeMultiplier { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableDeadPlayerPhoneRing { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> DeadPlayerPhoneMaxDistanceMeters { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> DeadPlayerPhoneMaxLookAngleDegrees { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> DeadPlayerPhoneMaxRingTimeSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> RandomizeDeadPlayerPhoneTalkTime { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> DeadPlayerPhoneTalkMinTimeSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> DeadPlayerPhoneTalkMaxTimeSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> DeadPlayerPhoneCooldownSeconds { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableMonsterSpectate { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> SpectateMonstersAfterPlayers { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> IncludeMimicsInMonsterSpectate { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnablePlayerTuning { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MoveSpeedMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MaxStaminaMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> StaminaDrainMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> StaminaRegenMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> StaminaRegenDelayMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MaxCarryWeightMultiplier { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableDungeonRandomizer { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> RandomizeDungeonPick { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> DungeonPickPoolMode { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> DungeonAllowlist { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> DungeonBlocklist { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> IgnoreDungeonExcludeList { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> RandomizeLayoutFlow { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> RandomizeMapVariant { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> RandomizeDungeonSeed { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableWebDashboard { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> WebDashboardListenAddress { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> WebDashboardListenPort { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableDebugLogging { get; private set; } = null!;

        public static MelonPreferences_Entry<bool> EnableExtendedSaveSlots { get; internal set; } = null!;

        private static readonly List<MelonPreferences_Entry<float>> FloatEntries = [];

        /// <summary>Registers a float entry for NaN/precision sanitizing and TOML normalization.</summary>
        internal static void TrackFloatEntry(MelonPreferences_Entry<float> entry)
        {
            FloatEntries.Add(entry);
        }

        public static void Initialize(MelonLogger.Instance logger)
        {
            ModConfigRegistry.ClearRegistrationOrder();
            FilePath = Path.Combine(MelonEnvironment.UserDataDirectory, "MimesisPlayerEnhancement.cfg");
            SparseTomlConfig.RepairTomletCompatibility(FilePath);

            MainCategory = CreateCategory(MainCategoryId, "Mimesis Player Enhancement");
            MorePlayersConfig.CreateCategory();
            MoreVoicesConfig.CreateCategory();
            PersistenceConfig.CreateCategory();
            StatisticsConfig.CreateCategory();
            PlayerAnnouncementsConfig.CreateCategory();
            JoinAnytimeConfig.CreateCategory();
            SpawnScalingConfig.CreateCategory();
            LootMultiplicatorConfig.CreateCategory();
            MoneyMultiplierConfig.CreateCategory();
            DungeonTimeConfig.CreateCategory();
            DeadPlayerFeaturesConfig.CreateCategory();
            PlayerTuningConfig.CreateCategory();
            DungeonRandomizerConfig.CreateCategory();
            WebDashboardConfig.CreateCategory();
            ExtendedSaveSlotsConfig.CreateCategory();

            ModToastDurationSeconds = CreateTrackedEntry(MainCategory,
                "ModToastDurationSeconds",
                5f,
                "Mod Toast Duration (seconds)",
                "How long [PlayerEnhancements] toasts stay visible before fading. Vanilla join/leave toasts are unchanged (~2 seconds). Each player controls this locally.");

            EnableDebugLogging = CreateTrackedEntry(MainCategory,
                "EnableDebugLogging",
                false,
                "Enable Debug Logging",
                "Emit verbose diagnostic lines to the MelonLoader console.");

            MorePlayersConfig.CreateEntries();
            MoreVoicesConfig.CreateEntries();
            PersistenceConfig.CreateEntries();
            StatisticsConfig.CreateEntries();
            PlayerAnnouncementsConfig.CreateEntries();
            JoinAnytimeConfig.CreateEntries();
            ExtendedSaveSlotsConfig.CreateEntries();
            SpawnScalingConfig.CreateEntries();
            LootMultiplicatorConfig.CreateEntries();
            MoneyMultiplierConfig.CreateEntries();
            DungeonTimeConfig.CreateEntries();
            DeadPlayerFeaturesConfig.CreateEntries();
            PlayerTuningConfig.CreateEntries();
            DungeonRandomizerConfig.CreateEntries();
            WebDashboardConfig.CreateEntries();

            MorePlayersConfig.SanitizeInitialValues(logger);
            JoinAnytimeConfig.SanitizeInitialValues(logger);
            MoneyMultiplierConfig.SanitizeInitialValues(logger);
            DungeonRandomizerConfig.SanitizeInitialValues(logger);

            MorePlayersConfig.WireValidation(logger);
            MoreVoicesConfig.WireValidation(logger);
            PersistenceConfig.WireValidation();
            StatisticsConfig.WireValidation(logger);
            PlayerAnnouncementsConfig.WireValidation();
            ModToastDurationSeconds.OnEntryValueChanged.Subscribe((_, value) =>
            {
                if (value < 1f)
                {
                    logger.Warning("ModToastDurationSeconds must be at least 1; resetting to 1.");
                    ModToastDurationSeconds.Value = 1f;
                    return;
                }

                NotifyChanged(ModToastDurationSeconds);
            });
            JoinAnytimeConfig.WireValidation(logger);
            ExtendedSaveSlotsConfig.WireValidation();
            SpawnScalingConfig.WireValidation(logger);
            LootMultiplicatorConfig.WireValidation(logger);
            MoneyMultiplierConfig.WireValidation(logger);
            DungeonTimeConfig.WireValidation(logger);
            DeadPlayerFeaturesConfig.WireValidation(logger);
            PlayerTuningConfig.WireValidation(logger);
            DungeonRandomizerConfig.WireValidation(logger);
            WebDashboardConfig.WireValidation(logger);
            EnableDebugLogging.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged(EnableDebugLogging));

            RegisterFloatEntries();
            SpawnScalingConfig.MigrateLegacyKeys(logger);
            DeadPlayerFeaturesConfig.MigrateLegacyKeys(logger);
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

        /// <summary>Update a single preference by section and key. Validation runs through existing entry change handlers.</summary>
        public static bool TrySetEntryValue(string sectionId, string key, string value, out string? error)
        {
            return ModConfigRegistry.TrySetEntryValue(sectionId, key, value, out error);
        }

        /// <summary>Called after MelonLoader reloads the config file from disk.</summary>
        internal static void NotifyFileReloaded()
        {
            ModConfigChangeTracker.NotifyFullReload();
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
            SpawnScalingConfig.RegisterFloatEntries();
            LootMultiplicatorConfig.RegisterFloatEntries();
            MoneyMultiplierConfig.RegisterFloatEntries();
            DungeonTimeConfig.RegisterFloatEntries();
            DeadPlayerFeaturesConfig.RegisterFloatEntries();
            PlayerTuningConfig.RegisterFloatEntries();
        }

        internal static void OnSpawnMultiplierChanged(MelonLogger.Instance logger, float value, MelonPreferences_Entry<float> entry)
        {
            if (value < 0f)
            {
                logger.Warning($"{entry.Identifier} must be >= 0; resetting to 0.");
                entry.Value = 0f;
                return;
            }

            ModConfigFloatHelper.SanitizeEntry(entry);
            NotifyChanged(entry);
        }

        internal static MelonPreferences_Category CreateCategory(string id, string displayName)
        {
            MelonPreferences_Category category = MelonPreferences.CreateCategory(id, displayName);
            category.SetFilePath(FilePath);
            ModConfigRegistry.TrackCategory(category);
            return category;
        }

        internal static MelonPreferences_Entry<T> CreateTrackedEntry<T>(
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

        internal static void NotifyChanged(MelonPreferences_Entry entry)
        {
            ModConfigChangeTracker.NotifyEntryChanged(entry);
        }
    }
}
