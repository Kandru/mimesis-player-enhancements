using System;
using MelonLoader;
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
        private float _nextFixedRespawnProcessTime;

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
                && Time.time >= _nextFixedRespawnProcessTime)
            {
                _nextFixedRespawnProcessTime = Time.time + FixedRespawnTiming.RetryIntervalSeconds;

                foreach (IFeatureModule module in FeatureModules.All)
                {
                    if (module is FeatureModule { ThrottledUpdate: true })
                    {
                        module.OnUpdate();
                    }
                }
            }
        }

        public override void OnDeinitializeMelon()
        {
            StatisticsWriteQueue.FlushAllSync();
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

            int sessionCap = ModConfig.EnableMorePlayers.Value ? ModConfig.MaxPlayers.Value : 4;

            if (_statisticsWasEnabled && !ModConfig.EnableStatistics.Value)
            {
                StatisticsTracker.ClearRuntimeState();
            }

            _statisticsWasEnabled = ModConfig.EnableStatistics.Value;

            ModLog.Debug(
                "Config",
                $"Synced — MorePlayers={ModConfig.EnableMorePlayers.Value} (session cap {sessionCap}), " +
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
                $"SpectatorTransition={ModConfig.EnableSpectatorTransition.Value}, " +
                $"DungeonRandomizer={ModConfig.EnableDungeonRandomizer.Value}, " +
                $"DungeonSizeScaling={ModConfig.EnableDungeonSizeScaling.Value}, " +
                $"WebDashboard={ModConfig.EnableWebDashboard.Value}" +
                (ModConfig.EnableWebDashboard.Value
                    ? $" ({ModConfig.WebDashboardListenAddress.Value}:{ModConfig.WebDashboardListenPort.Value})"
                    : "") +
                $", DebugLogging={ModConfig.EnableDebugLogging.Value}");
        }

        private void LogStartupSummary()
        {
            ModLog.Info(
                "Startup",
                $"v{VersionInfo.ModuleVersion} loaded — " +
                $"MorePlayers={ModConfig.EnableMorePlayers.Value}" +
                (ModConfig.EnableMorePlayers.Value ? $" (session cap {ModConfig.MaxPlayers.Value})" : "") +
                $", MoreVoices={ModConfig.EnableMoreVoices.Value}" +
                (ModConfig.EnableMoreVoices.Value
                    ? $" (indoor {ModConfig.MaxIndoorVoiceEvents.Value}, deathmatch {ModConfig.MaxDeathMatchVoiceEvents.Value}, outdoor {ModConfig.MaxOutdoorVoiceEvents.Value})"
                    : "") +
                $", Persistence={ModConfig.EnablePersistence.Value}" +
                $", Statistics={ModConfig.EnableStatistics.Value}, " +
                $"JoinAnytime={ModConfig.EnableJoinAnytime.Value}, " +
                $"SpawnScaling={ModConfig.EnableSpawnScaling.Value}, " +
                $"LootMultiplicator={ModConfig.EnableLootMultiplicator.Value}, " +
                $"MoneyMultiplier={ModConfig.EnableMoneyMultiplier.Value}, " +
                $"DungeonTime={ModConfig.EnableDungeonTime.Value}, " +
                $"SpectatorTransition={ModConfig.EnableSpectatorTransition.Value}, " +
                $"DungeonRandomizer={ModConfig.EnableDungeonRandomizer.Value}, " +
                $"DungeonSizeScaling={ModConfig.EnableDungeonSizeScaling.Value}, " +
                $"WebDashboard={ModConfig.EnableWebDashboard.Value}" +
                (ModConfig.EnableWebDashboard.Value
                    ? $" ({ModConfig.WebDashboardListenAddress.Value}:{ModConfig.WebDashboardListenPort.Value})"
                    : "") +
                $", DebugLogging={ModConfig.EnableDebugLogging.Value}");
        }
    }
}
