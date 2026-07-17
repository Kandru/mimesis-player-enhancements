using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using MimesisPlayerEnhancement.Features.MimicTuning;
using MimesisPlayerEnhancement.Features.UserInterface;

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
        public static MelonPreferences_Entry<bool> EnableScalingRoundGoals { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> RoundGoalBasePerZone { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> RoundGoalMoneyMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> RoundGoalRandomSpreadPercent { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> RoundGoalCurveExponent { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableMoreVoices { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> UnifyIndoorOutdoorVoices { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> MaxIndoorVoiceEvents { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> MaxDeathMatchVoiceEvents { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> MaxOutdoorVoiceEvents { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> RecordVoiceInMaintenance { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> RecordVoiceInTram { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> RecordVoiceDuringMimicPossession { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> EnableVoicePerformanceCache { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> VoiceClipCacheMaxEntries { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnablePersistence { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableStatistics { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> SessionReconnectGraceMinutes { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> ShowStatisticsToasts { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> ShowPlayerAnnouncements { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableJoinAnytime { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> JoinConnectionGraceSeconds { get; internal set; } = null!;

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
        public static MelonPreferences_Entry<string> PeriodicSpawnWaitMode { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> InitialPeriodicSpawnWaitSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> InitialPeriodicSpawnWaitMinSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> InitialPeriodicSpawnWaitMaxSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> PeriodicSpawnIntervalSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> PeriodicSpawnIntervalMinSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> PeriodicSpawnIntervalMaxSeconds { get; internal set; } = null!;
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
        public static MelonPreferences_Entry<bool> AutoScaleMapLootBudgetForFilter { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> ConvertFakeActorDyingDropChancePercent { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableEconomy { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> EconomyPlayerCountScaleRate { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> AutoScaleStartupMoneyByPlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> StartupMoneyMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> AutoScaleScrapSellValueByPlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> ScrapSellValueMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> AutoScaleShopBuyPriceByPlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> ShopBuyPriceMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> ShopDiscountMinPercent { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> ShopDiscountMaxPercent { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> ShopDiscountChancePercent { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> AutoScaleReinforcePriceByPlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> ReinforcePriceMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> RetainUnspentCurrencyBetweenCycles { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableDungeonTime { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> DungeonTimeBaselinePlayerCount { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> ExtraShiftSecondsPerPlayerAboveBaseline { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableMimicPossessionTuning { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> RandomizeMimicPossessionDuration { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MimicPossessionMinTimeSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MimicPossessionMaxTimeSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MimicPossessionCooltimeMultiplier { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableMimicTuning { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> MimicVoiceTuningMode { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> PeriodicVoiceIntervalMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> PlayerVoiceResponseChancePercent { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> PlayerVoiceResponseCooldownSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> PlayerVoiceResponseDelayMinSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> PlayerVoiceResponseDelayMaxSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> PlayerVoiceResponseMaxDistance { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> MimicInventoryCopyMode { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> MimicInventoryCopyPickRule { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnablePlayerTuning { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MoveSpeedMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> NoClipSpeedMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MaxStaminaMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> StaminaDrainMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> StaminaRegenMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> StaminaRegenDelayMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> MaxCarryWeightMultiplier { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> DisablePlayerCollision { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableDungeonRandomizer { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> RandomizeDungeonPick { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> DungeonPickPoolMode { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> DungeonAllowlist { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> DungeonBlocklist { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> IgnoreDungeonExcludeList { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> RandomizeMapVariant { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> DungeonSeedFlavor { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableWeather { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> WeatherMode { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> FixedWeatherPreset { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> DisableRandomWeather { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> WeatherCyclePresets { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> WeatherCycleMinDelaySeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> WeatherCycleMaxDelaySeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> StartTimePreset { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> EnableRealtimeTramClock { get; internal set; } = null!;

        public static MelonPreferences_Entry<string> WebDashboardListenAddress { get; internal set; } = null!;
        public static MelonPreferences_Entry<int> WebDashboardListenPort { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnableDebugLogging { get; private set; } = null!;

        public static MelonPreferences_Entry<string> LastSeenModVersion { get; private set; } = null!;

        public static MelonPreferences_Entry<float> ModToastDurationSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> EnableExtendedSaveSlots { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> EnableExtendedSpectatorPlayerList { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> EnableLoadingWaitPlayerList { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> EnableExtendedInGameMenuPlayerList { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> EnableDamageHealthGlow { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> EnableFloatingDamageNumbers { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> FloatingDamageDurationSeconds { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> EnableFpsUi { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> EnableFpsUiInventoryNetWorth { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> RoundStartSoundMode { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> RoundStartSoundVariant { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> RoundStartSoundRandomPool { get; internal set; } = null!;
        public static MelonPreferences_Entry<float> RoundStartSoundVolume { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> CustomLoadingScreenMode { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> CustomLoadingScreenVariant { get; internal set; } = null!;
        public static MelonPreferences_Entry<string> CustomLoadingScreenRandomPool { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> CustomLoadingScreenMotion { get; internal set; } = null!;

        public static MelonPreferences_Entry<bool> EnablePrivacy { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> BlockReluTelemetry { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> BlockReplayUpload { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> BlockReplayRecording { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> BlockCrashReports { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> StripCrashReportMetadata { get; internal set; } = null!;
        public static MelonPreferences_Entry<bool> BlockKraftonGppSdk { get; internal set; } = null!;

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

            MainCategory = CreateCategory(MainCategoryId);
            UiConfig.CreateCategory();
            PrivacyConfig.CreateCategory();
            MorePlayersConfig.CreateCategory();
            MoreVoicesConfig.CreateCategory();
            PersistenceConfig.CreateCategory();
            StatisticsConfig.CreateCategory();
            PlayerAnnouncementsConfig.CreateCategory();
            JoinAnytimeConfig.CreateCategory();
            SpawnScalingConfig.CreateCategory();
            LootMultiplicatorConfig.CreateCategory();
            EconomyConfig.CreateCategory();
            DungeonTimeConfig.CreateCategory();
            MimicTuningConfig.CreateCategory();
            PlayerTuningConfig.CreateCategory();
            DungeonRandomizerConfig.CreateCategory();
            WeatherConfig.CreateCategory();
            WebDashboardConfig.CreateCategory();

            EnableDebugLogging = CreateTrackedEntry(MainCategory,
                "EnableDebugLogging",
                false);

            LastSeenModVersion = CreateHiddenTrackedEntry(MainCategory,
                "LastSeenModVersion",
                string.Empty);

            MorePlayersConfig.CreateEntries();
            MoreVoicesConfig.CreateEntries();
            PersistenceConfig.CreateEntries();
            StatisticsConfig.CreateEntries();
            PlayerAnnouncementsConfig.CreateEntries();
            JoinAnytimeConfig.CreateEntries();
            SpawnScalingConfig.CreateEntries();
            LootMultiplicatorConfig.CreateEntries();
            EconomyConfig.CreateEntries();
            DungeonTimeConfig.CreateEntries();
            MimicTuningConfig.CreateEntries();
            PlayerTuningConfig.CreateEntries();
            DungeonRandomizerConfig.CreateEntries();
            WeatherConfig.CreateEntries();
            WebDashboardConfig.CreateEntries();
            UiConfig.CreateEntries();
            PrivacyConfig.CreateEntries();

            MorePlayersConfig.SanitizeInitialValues(logger);
            JoinAnytimeConfig.SanitizeInitialValues(logger);
            EconomyConfig.SanitizeInitialValues(logger);
            DungeonRandomizerConfig.SanitizeInitialValues(logger);
            WeatherConfig.SanitizeInitialValues(logger);

            MorePlayersConfig.WireValidation(logger);
            MoreVoicesConfig.WireValidation(logger);
            PersistenceConfig.WireValidation();
            StatisticsConfig.WireValidation(logger);
            PlayerAnnouncementsConfig.WireValidation();
            JoinAnytimeConfig.WireValidation(logger);
            SpawnScalingConfig.WireValidation(logger);
            LootMultiplicatorConfig.WireValidation(logger);
            EconomyConfig.WireValidation(logger);
            DungeonTimeConfig.WireValidation(logger);
            MimicTuningConfig.WireValidation(logger);
            PlayerTuningConfig.WireValidation(logger);
            DungeonRandomizerConfig.WireValidation(logger);
            WeatherConfig.WireValidation(logger);
            WebDashboardConfig.WireValidation(logger);
            UiConfig.WireValidation(logger);
            PrivacyConfig.WireValidation();
            EnableDebugLogging.OnEntryValueChanged.Subscribe((_, _) => NotifyChanged(EnableDebugLogging));

            RegisterFloatEntries();
            ModConfigFloatHelper.SanitizeAll(FloatEntries);
            NormalizeSavedFloats();
            ModConfigRegistry.Rebuild();
            ModConfigRegistry.PurgeObsoleteFromFile(FilePath);
            MainCategory.LoadFromFile(false);
            SanitizeFloatEntries();

            IsInitialized = true;
            ModConfigLocalization.Apply();
        }

        /// <summary>Persist current preference values to <see cref="FilePath"/>.</summary>
        public static void SaveToFile()
        {
            ModConfigRegistry.SaveToFile();
        }

        /// <summary>Flush deferred global config changes to disk. Called on game quit.</summary>
        internal static void FlushGlobalToDisk()
        {
            GlobalConfigStore.FlushToDisk();
        }

        /// <summary>Reload in-memory entries from the global config file on disk.</summary>
        internal static void ReloadGlobalFromFile()
        {
            if (!IsInitialized)
            {
                return;
            }

            GlobalConfigStore.FlushIfDirty();
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
            MorePlayersConfig.RegisterFloatEntries();
            SpawnScalingConfig.RegisterFloatEntries();
            LootMultiplicatorConfig.RegisterFloatEntries();
            EconomyConfig.RegisterFloatEntries();
            DungeonTimeConfig.RegisterFloatEntries();
            MimicTuningConfig.RegisterFloatEntries();
            PlayerTuningConfig.RegisterFloatEntries();
            WeatherConfig.RegisterFloatEntries();
            UiConfig.RegisterFloatEntries();
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

        internal static MelonPreferences_Category CreateCategory(string id)
        {
            string displayName = ModL10n.GetConfigSectionTitle(id) ?? id;
            MelonPreferences_Category category = MelonPreferences.CreateCategory(id, displayName);
            category.SetFilePath(FilePath);
            ModConfigRegistry.TrackCategory(category);
            return category;
        }

        internal static MelonPreferences_Entry<T> CreateTrackedEntry<T>(
            MelonPreferences_Category category,
            string identifier,
            T defaultValue)
        {
            string sectionId = category.Identifier;
            string displayName = ModL10n.GetConfigEntryTitle(sectionId, identifier) ?? identifier;
            string description = ModL10n.GetConfigEntryDescription(sectionId, identifier) ?? string.Empty;
            MelonPreferences_Entry<T> entry = category.CreateEntry(
                identifier, defaultValue, displayName, description);
            ModConfigRegistry.TrackEntry(entry);
            return entry;
        }

        internal static MelonPreferences_Entry<T> CreateHiddenTrackedEntry<T>(
            MelonPreferences_Category category,
            string identifier,
            T defaultValue)
        {
            MelonPreferences_Entry<T> entry = CreateTrackedEntry(
                category, identifier, defaultValue);
            entry.IsHidden = true;
            return entry;
        }

        internal static void NotifyChanged(MelonPreferences_Entry entry)
        {
            ModConfigChangeTracker.NotifyEntryChanged(entry);
        }
    }
}
