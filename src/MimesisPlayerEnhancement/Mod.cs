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
        private bool _isInitializing;

        public override void OnInitializeMelon()
        {
            _isInitializing = true;
            try
            {
                ModConfig.Initialize(LoggerInstance);
                SceneScopedConfigGate.Initialize();
                SceneScopedConfigGate.SetDeferredModuleSyncAction(SyncFeatureModuleByName);
                if (!GameVersionChecker.TryAllowLoad())
                {
                    throw new InvalidOperationException(
                        $"Mimesis Player Enhancement refused to load — game version mismatch (expected {VersionInfo.GameVersion}).");
                }

                ModL10n.Initialize();

                _harmony = new HarmonyLib.Harmony("com.mimesis.playerenhancement");
                foreach (IFeatureModule module in FeatureModules.All)
                {
                    module.ApplyPatches(_harmony);
                }

                SceneScopedConfigPatches.Apply(_harmony);
                SyncFromConfig(ModConfigChangeInfo.FullReload);
                LogStartupSummary();
                ModConfig.Changed += SyncFromConfig;
            }
            finally
            {
                _isInitializing = false;
            }
        }

        public override void OnPreferencesSaved(string filepath)
        {
            if (!IsOurConfigFile(filepath))
            {
                return;
            }

            ModConfig.NormalizeSavedFloats();
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
            foreach (IFeatureModule module in FeatureModules.All)
            {
                if (module is FeatureModule { ThrottledUpdate: true })
                {
                    continue;
                }

                module.OnUpdate();
            }

            if ((ModConfig.EnableSpawnScaling.Value || ModConfig.EnableLootMultiplicator.Value)
                && Time.time >= _nextEncounterSpawnProcessTime)
            {
                _nextEncounterSpawnProcessTime = Time.time + EncounterSpawnTiming.RetryIntervalSeconds;

                foreach (IFeatureModule module in FeatureModules.All)
                {
                    if (module is FeatureModule { ThrottledUpdate: true })
                    {
                        module.OnUpdate();
                    }
                }
            }

            SaveSlotConfigLifecycle.Tick();
        }

        public override void OnDeinitializeMelon()
        {
            foreach (IFeatureModule module in FeatureModules.All)
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

        private void SyncFromConfig(ModConfigChangeInfo change)
        {
            if (!ModConfig.IsInitialized)
            {
                return;
            }

            SceneScopedConfigGate.OnConfigChangePrepared(change);

            HashSet<string>? affectedModules = change.IsFullReload
                ? null
                : ModConfigRegistry.GetAffectedModuleNames(change);

            foreach (IFeatureModule module in FeatureModules.All)
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
            foreach (IFeatureModule module in FeatureModules.All)
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
                $", WebDashboard={ModConfig.EnableWebDashboard.Value}" +
                (ModConfig.EnableWebDashboard.Value
                    ? $" ({ModConfig.WebDashboardListenAddress.Value}:{ModConfig.WebDashboardListenPort.Value})"
                    : "") +
                $", DebugLogging={ModConfig.EnableDebugLogging.Value}";
        }
    }
}
