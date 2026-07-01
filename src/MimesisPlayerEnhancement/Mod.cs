using System;
using MelonLoader;
using MimesisPlayerEnhancement.Features.Persistence;
using MimesisPlayerEnhancement.Features.PlayerTuning;
using MimesisPlayerEnhancement.Features.WebDashboard;
using MimesisPlayerEnhancement.Util;
using UnityEngine;

[assembly: MelonInfo(typeof(MimesisPlayerEnhancement.Mod), "MimesisPlayerEnhancement", MimesisPlayerEnhancement.VersionInfo.ModuleVersion, "kalle")]
[assembly: MelonGame("ReLUGames", "MIMESIS")]
[assembly: HarmonyDontPatchAll]

namespace MimesisPlayerEnhancement
{
    public sealed class Mod : MelonMod
    {
        private HarmonyLib.Harmony? _harmony;
        private bool _statisticsWasEnabled;
        private float _nextEncounterSpawnProcessTime;

        public override void OnInitializeMelon()
        {
            ModConfig.Initialize(LoggerInstance);
            ModConfig.Changed += SyncFromConfig;

            _harmony = new HarmonyLib.Harmony("com.mimesis.playerenhancement");
            foreach (IFeatureModule module in FeatureModules.All)
            {
                module.ApplyPatches(_harmony);
            }

            _statisticsWasEnabled = ModConfig.EnableStatistics.Value;
            SyncFromConfig();
            LogStartupSummary();
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
            ModConfig.NotifyFileReloaded();
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
            StatisticsWriteQueue.FlushAllSync();
            PersistenceWriteQueue.FlushAllSync();
            PlayerTuningApplier.RestoreOnShutdown();
            WebDashboardServer.StopOnDeinit();
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

        private void SyncFromConfig()
        {
            if (!ModConfig.IsInitialized)
            {
                return;
            }

            foreach (IFeatureModule module in FeatureModules.All)
            {
                module.SyncFromConfig();
            }

            if (_statisticsWasEnabled && !ModConfig.EnableStatistics.Value)
            {
                StatisticsTracker.ClearRuntimeState();
            }

            _statisticsWasEnabled = ModConfig.EnableStatistics.Value;

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
                $"MimicTuning={ModConfig.EnableMimicTuning.Value}" +
                (ModConfig.EnableMimicTuning.Value
                    ? $" (randomize={ModConfig.RandomizeMimicPossessionDuration.Value}" +
                      (ModConfig.RandomizeMimicPossessionDuration.Value
                          ? $", duration={ModConfig.MimicPossessionMinTimeSeconds.Value}-{ModConfig.MimicPossessionMaxTimeSeconds.Value}s"
                          : "") +
                      $", cooltime×{ModConfig.MimicPossessionCooltimeMultiplier.Value})"
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
