using System;
using MelonLoader;
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
                ModL10n.Initialize();

                _harmony = new HarmonyLib.Harmony("com.mimesis.playerenhancement");
                foreach (IFeatureModule module in FeatureModules.All)
                {
                    module.ApplyPatches(_harmony);
                }

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

            HashSet<string>? affectedModules = change.IsFullReload
                ? null
                : ModConfigRegistry.GetAffectedModuleNames(change);

            foreach (IFeatureModule module in FeatureModules.All)
            {
                if (affectedModules != null && !affectedModules.Contains(module.Name))
                {
                    continue;
                }

                module.SyncFromConfig();
            }

            ModLog.Debug("Config", $"Synced — {BuildConfigSummary()}");
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
                    ? $" (indoor {ModConfig.MaxIndoorVoiceEvents.Value}, deathmatch {ModConfig.MaxDeathMatchVoiceEvents.Value}, outdoor {ModConfig.MaxOutdoorVoiceEvents.Value})"
                    : "") +
                $", Persistence={ModConfig.EnablePersistence.Value}, " +
                $"Statistics={ModConfig.EnableStatistics.Value}, " +
                $"JoinAnytime={ModConfig.EnableJoinAnytime.Value}, " +
                $"SpawnScaling={ModConfig.EnableSpawnScaling.Value}, " +
                $"LootMultiplicator={ModConfig.EnableLootMultiplicator.Value}, " +
                $"MoneyMultiplier={ModConfig.EnableMoneyMultiplier.Value}, " +
                $"DungeonTime={ModConfig.EnableDungeonTime.Value}, " +
                $"DeadPlayerFeatures={ModConfig.EnableDeadPlayerFeatures.Value}" +
                (ModConfig.EnableDeadPlayerFeatures.Value
                    ? $" (mimicTuning={ModConfig.EnableMimicPossessionTuning.Value}" +
                      (ModConfig.EnableMimicPossessionTuning.Value && ModConfig.RandomizeMimicPossessionDuration.Value
                          ? $", duration={ModConfig.MimicPossessionMinTimeSeconds.Value}-{ModConfig.MimicPossessionMaxTimeSeconds.Value}s"
                          : "") +
                      (ModConfig.EnableMimicPossessionTuning.Value
                          ? $", cooltime×{ModConfig.MimicPossessionCooltimeMultiplier.Value}"
                          : "") +
                      $", monsterSpectate={ModConfig.EnableMonsterSpectate.Value})"
                    : "") +
                $", PlayerTuning={ModConfig.EnablePlayerTuning.Value}, " +
                $"DungeonRandomizer={ModConfig.EnableDungeonRandomizer.Value}, " +
                $"ExtendedSaveSlots={ModConfig.EnableExtendedSaveSlots.Value}" +
                $", WebDashboard={ModConfig.EnableWebDashboard.Value}" +
                (ModConfig.EnableWebDashboard.Value
                    ? $" ({ModConfig.WebDashboardListenAddress.Value}:{ModConfig.WebDashboardListenPort.Value})"
                    : "") +
                $", DebugLogging={ModConfig.EnableDebugLogging.Value}";
        }
    }
}
