using MelonLoader;
using MimesisPlayerEnhancement.Util.Patches;
using UnityEngine;

[assembly: MelonInfo(typeof(MimesisPlayerEnhancement.Mod), "MimesisPlayerEnhancement", MimesisPlayerEnhancement.VersionInfo.ModuleVersion, "kalle")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]
[assembly: HarmonyDontPatchAll]

namespace MimesisPlayerEnhancement
{
    public sealed class Mod : MelonMod
    {
        private HarmonyLib.Harmony? _harmony;
        private float _nextEncounterSpawnProcessTime;
        private float _nextLocaleRefreshTime;
        private bool _isInitializing;
        private static bool _globalConfigFlushed;
        private readonly object _pendingSyncLock = new();
        private ModConfigChangeInfo? _pendingSyncFromConfig;

        public override void OnInitializeMelon()
        {
            _isInitializing = true;
            try
            {
                GameLocaleAccess.CaptureMainThread();
                ModL10n.Initialize();
                ModConfig.Initialize(LoggerInstance);
                SceneScopedConfigGate.Initialize();
                SceneScopedConfigGate.SetDeferredModuleSyncAction(SyncFeatureModuleByName);
                if (!GameVersionChecker.TryAllowLoad())
                {
                    throw new InvalidOperationException(
                        $"Mimesis Player Enhancement refused to load — game version mismatch (expected {VersionInfo.GameVersion}).");
                }

                _harmony = new HarmonyLib.Harmony("com.mimesis.playerenhancement");
                foreach (FeatureModule module in FeatureModules.All)
                {
                    module.ApplyPatches(_harmony);
                }

                SceneScopedConfigPatches.Apply(_harmony);
                SyncFromConfig(ModConfigChangeInfo.FullReload);
                LogStartupSummary();
                ModConfig.Changed += SyncFromConfig;
                Application.quitting += OnApplicationQuitting;
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private static void OnApplicationQuitting()
        {
            WebDashboardServer.PrepareApplicationQuit();
            FlushGlobalConfigOnShutdown();
        }

        public override void OnPreferencesSaved(string filepath)
        {
            if (!IsOurConfigFile(filepath))
            {
                return;
            }

            ModConfig.NormalizeSavedFloats();
            GlobalConfigStore.RestorePendingToDisk();
        }

        public override void OnPreferencesLoaded(string filepath)
        {
            if (!IsOurConfigFile(filepath))
            {
                return;
            }

            ModConfig.SanitizeFloatEntries();
            ModConfig.NormalizeSavedFloats();
            if (!_isInitializing)
            {
                ModConfig.NotifyFileReloaded();
            }
        }

        public override void OnUpdate()
        {
            FlushDeferredSyncFromConfig();

            foreach (FeatureModule module in FeatureModules.All)
            {
                if (module.ThrottledUpdate)
                {
                    continue;
                }

                if (module.SessionScope == SessionScope.HostOnly
                    && !FeatureModuleSessionHooks.IsModuleActive(module.Name))
                {
                    continue;
                }

                module.OnUpdate();
            }

            if ((ModConfig.EnableSpawnScaling.Value || ModConfig.EnableLootMultiplicator.Value
                    || WeatherCycleScheduler.NeedsThrottledProcessing())
                && Time.time >= _nextEncounterSpawnProcessTime)
            {
                _nextEncounterSpawnProcessTime = Time.time + EncounterSpawnTiming.RetryIntervalSeconds;

                foreach (FeatureModule module in FeatureModules.All)
                {
                    if (!module.ThrottledUpdate)
                    {
                        continue;
                    }

                    if (module.SessionScope == SessionScope.HostOnly
                        && !FeatureModuleSessionHooks.IsModuleActive(module.Name))
                    {
                        continue;
                    }

                    module.OnUpdate();
                }
            }

            SessionLifecycle.Tick();

            if (Time.time >= _nextLocaleRefreshTime)
            {
                _nextLocaleRefreshTime = Time.time + 2f;
                ModConfigLocalization.RefreshIfLanguageChanged();
            }
        }

        public override void OnDeinitializeMelon()
        {
            Application.quitting -= OnApplicationQuitting;
            FlushGlobalConfigOnShutdown();
            SessionLifecycle.NotifySessionEndedIfActive();

            foreach (FeatureModule module in FeatureModules.All)
            {
                module.OnDeinitialize();
            }

            HostStatusCache.Invalidate();
            ModConfig.Changed -= SyncFromConfig;
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                ModLog.Debug("Startup", "Harmony patches removed.");
            }
        }

        private static bool IsOurConfigFile(string filepath)
        {
            return string.Equals(filepath, ModConfig.FilePath, StringComparison.OrdinalIgnoreCase);
        }

        private static void FlushGlobalConfigOnShutdown()
        {
            if (_globalConfigFlushed)
            {
                return;
            }

            _globalConfigFlushed = true;
            ModConfig.FlushGlobalToDisk();
        }

        private void SyncFromConfig(ModConfigChangeInfo change)
        {
            if (!ModConfig.IsInitialized)
            {
                return;
            }

            if (!GameLocaleAccess.IsMainThread)
            {
                QueueDeferredSyncFromConfig(change);
                return;
            }

            ApplySyncFromConfig(change);
        }

        private void QueueDeferredSyncFromConfig(ModConfigChangeInfo change)
        {
            lock (_pendingSyncLock)
            {
                _pendingSyncFromConfig = CoalescePendingSync(_pendingSyncFromConfig, change);
            }
        }

        private void FlushDeferredSyncFromConfig()
        {
            ModConfigChangeInfo? pending;
            lock (_pendingSyncLock)
            {
                pending = _pendingSyncFromConfig;
                _pendingSyncFromConfig = null;
            }

            if (pending != null)
            {
                ApplySyncFromConfig(pending);
            }
        }

        private static ModConfigChangeInfo CoalescePendingSync(
            ModConfigChangeInfo? pending,
            ModConfigChangeInfo incoming)
        {
            if (pending == null)
            {
                return incoming;
            }

            if (pending.IsFullReload || incoming.IsFullReload)
            {
                return ModConfigChangeInfo.FullReload;
            }

            List<ModConfigKeyChange> merged = [];
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

            void AddKey(ModConfigKeyChange keyChange)
            {
                string id = $"{keyChange.SectionId}\0{keyChange.Key}";
                if (!seen.Add(id))
                {
                    return;
                }

                merged.Add(keyChange);
            }

            foreach (ModConfigKeyChange keyChange in pending.ChangedKeys)
            {
                AddKey(keyChange);
            }

            foreach (ModConfigKeyChange keyChange in incoming.ChangedKeys)
            {
                AddKey(keyChange);
            }

            return new ModConfigChangeInfo { ChangedKeys = merged };
        }

        private void ApplySyncFromConfig(ModConfigChangeInfo change)
        {
            SceneScopedConfigGate.OnConfigChangePrepared(change);

            HashSet<string>? affectedModules = change.IsFullReload
                ? null
                : ModConfigRegistry.GetAffectedModuleNames(change);

            foreach (FeatureModule module in FeatureModules.All)
            {
                if (affectedModules != null && !affectedModules.Contains(module.Name))
                {
                    continue;
                }

                if (SceneScopedConfigGate.ShouldDeferModuleSync(module.Name, change))
                {
                    continue;
                }

                module.SyncFromConfig();
            }

            ModLog.Debug("Config", $"Synced — {BuildConfigSummary()}");
        }

        private static void SyncFeatureModuleByName(string moduleName)
        {
            foreach (FeatureModule module in FeatureModules.All)
            {
                if (string.Equals(module.Name, moduleName, StringComparison.Ordinal))
                {
                    module.SyncFromConfig();
                    break;
                }
            }
        }

        private void LogStartupSummary()
        {
            ModLog.Info("Startup", $"v{VersionInfo.ModuleVersion} loaded — {BuildConfigSummary()}");
        }

        private static string BuildConfigSummary()
        {
            int sessionCap = ModConfig.EnableMorePlayers.Value ? ModConfig.MaxPlayers.Value : 4;
            return
                $"MorePlayers={ModConfig.EnableMorePlayers.Value} (session cap {sessionCap}), " +
                $"MoreVoices={ModConfig.EnableMoreVoices.Value}" +
                (ModConfig.EnableMoreVoices.Value
                    ? $" (indoor {ModConfig.MaxIndoorVoiceEvents.Value}, deathmatch {ModConfig.MaxDeathMatchVoiceEvents.Value}, outdoor {ModConfig.MaxOutdoorVoiceEvents.Value}; " +
                      $"unifiedIndoorOutdoor={ModConfig.UnifyIndoorOutdoorVoices.Value}; " +
                      $"maintenance={ModConfig.RecordVoiceInMaintenance.Value}, tram={ModConfig.RecordVoiceInTram.Value}, possession={ModConfig.RecordVoiceDuringMimicPossession.Value})"
                    : "") +
                $", Persistence={ModConfig.EnablePersistence.Value}, " +
                $"Statistics={ModConfig.EnableStatistics.Value}, " +
                $"JoinAnytime={ModConfig.EnableJoinAnytime.Value}, " +
                $"SpawnScaling={ModConfig.EnableSpawnScaling.Value}, " +
                $"LootMultiplicator={ModConfig.EnableLootMultiplicator.Value}, " +
                $"Economy={ModConfig.EnableEconomy.Value}, " +
                $"DungeonTime={ModConfig.EnableDungeonTime.Value}, " +
                $"MimicTuning={ModConfig.EnableMimicTuning.Value}" +
                (ModConfig.EnableMimicTuning.Value
                    ? $" (voice={ModConfig.MimicVoiceTuningMode.Value}, inventory={ModConfig.MimicInventoryCopyMode.Value}" +
                      (ModConfig.EnableMimicPossessionTuning.Value
                          ? $", possession" +
                            (ModConfig.RandomizeMimicPossessionDuration.Value
                                ? $", duration={ModConfig.MimicPossessionMinTimeSeconds.Value}-{ModConfig.MimicPossessionMaxTimeSeconds.Value}s"
                                : "") +
                            $", cooltime×{ModConfig.MimicPossessionCooltimeMultiplier.Value}"
                          : "") +
                      ")"
                    : "") +
                $", PlayerTuning={ModConfig.EnablePlayerTuning.Value}, " +
                $"DungeonRandomizer={ModConfig.EnableDungeonRandomizer.Value}, " +
                $"Ui=(saveSlots={ModConfig.EnableExtendedSaveSlots.Value}, spectatorList={ModConfig.EnableExtendedSpectatorPlayerList.Value}, inGameMenuPlayerList={ModConfig.EnableExtendedInGameMenuPlayerList.Value})" +
                $", WebDashboard=({ModConfig.WebDashboardListenAddress.Value}:{ModConfig.WebDashboardListenPort.Value})" +
                $", DebugLogging={ModConfig.EnableDebugLogging.Value}";
        }
    }
}
