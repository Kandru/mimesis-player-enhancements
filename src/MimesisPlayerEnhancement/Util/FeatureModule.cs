using System;
using System.Collections.Generic;
using MimesisPlayerEnhancement.Features.DungeonRandomizer;
using MimesisPlayerEnhancement.Features.DungeonTime;
using MimesisPlayerEnhancement.Features.ExtendedSaveSlots;
using MimesisPlayerEnhancement.Features.JoinAnytime;
using MimesisPlayerEnhancement.Features.LootMultiplicator;
using MimesisPlayerEnhancement.Features.MimicTuning;
using MimesisPlayerEnhancement.Features.MoneyMultiplier;
using MimesisPlayerEnhancement.Features.MorePlayers;
using MimesisPlayerEnhancement.Features.MoreVoices;
using MimesisPlayerEnhancement.Features.PlayerAnnouncements;
using MimesisPlayerEnhancement.Features.PlayerTuning;
using MimesisPlayerEnhancement.Features.SpawnScaling;
using MimesisPlayerEnhancement.Features.WebDashboard;
using MimesisPlayerEnhancement.ModVersionDisplay;

namespace MimesisPlayerEnhancement.Util
{
    internal interface IFeatureModule
    {
        string Name { get; }

        void ApplyPatches(HarmonyLib.Harmony harmony);

        void SyncFromConfig()
        {
        }

        void OnUpdate()
        {
        }

        void OnDeinitialize()
        {
        }
    }

    internal sealed class FeatureModule : IFeatureModule
    {
        private readonly Action<HarmonyLib.Harmony> _applyPatches;
        private readonly Action? _syncFromConfig;
        private readonly Action? _onUpdate;
        private readonly Action? _onDeinitialize;

        public FeatureModule(
            string name,
            Action<HarmonyLib.Harmony> applyPatches,
            Action? syncFromConfig = null,
            Action? onUpdate = null,
            bool throttledUpdate = false,
            Action? onDeinitialize = null)
        {
            Name = name;
            _applyPatches = applyPatches;
            _syncFromConfig = syncFromConfig;
            _onUpdate = onUpdate;
            _onDeinitialize = onDeinitialize;
            if (throttledUpdate)
            {
                ThrottledUpdate = true;
            }
        }

        public string Name { get; }

        internal bool ThrottledUpdate { get; }

        public void ApplyPatches(HarmonyLib.Harmony harmony)
        {
            _applyPatches(harmony);
        }

        public void SyncFromConfig()
        {
            _syncFromConfig?.Invoke();
        }

        public void OnUpdate()
        {
            _onUpdate?.Invoke();
        }

        public void OnDeinitialize()
        {
            _onDeinitialize?.Invoke();
        }
    }

    internal static class FeatureModules
    {
        internal static IReadOnlyList<IFeatureModule> All { get; } =
        [
            new FeatureModule("MoreVoices", MoreVoicesPatches.Apply, MoreVoicesPatches.RefreshFromConfig),
            new FeatureModule("Persistence", PersistencePatches.Apply, onUpdate: () =>
            {
                if (ModConfig.EnablePersistence.Value) { SpeechEventPoolManager.ProcessDeferredUpdates(); } },
                onDeinitialize: PersistenceWriteQueue.FlushAllSync),
            new FeatureModule("Statistics", StatisticsPatches.Apply,
                syncFromConfig: StatisticsTracker.RefreshFromConfig,
                onUpdate: () =>
            {
                if (ModConfig.EnableStatistics.Value) { StatisticsTracker.OnUpdate(); } },
                onDeinitialize: StatisticsWriteQueue.FlushAllSync),
            new FeatureModule("PlayerAnnouncements", PlayerAnnouncementPatches.Apply),
            new FeatureModule("MorePlayers", MorePlayersPatches.Apply, MorePlayersPatches.RefreshFromConfig),
            new FeatureModule("JoinAnytime", JoinAnytimePatches.Apply,
                syncFromConfig: JoinAnytimeRuntime.RefreshFromConfig,
                onUpdate: JoinAnytimeRuntime.OnUpdate),
            new FeatureModule("SpawnScaling", SpawnScalingPatches.Apply,
                syncFromConfig: SpawnScalingPatches.RefreshFromConfig,
                onUpdate: () =>
            {
                if (ModConfig.EnableSpawnScaling.Value) { MapPlacedEncounterScheduler.ProcessPendingEncounters(); } }, throttledUpdate: true),
            new FeatureModule("LootMultiplicator", LootMultiplicatorPatches.Apply,
                syncFromConfig: LootMultiplicatorPatches.RefreshFromConfig,
                onUpdate: () =>
            {
                if (ModConfig.EnableLootMultiplicator.Value) { FixedLootSpawnCoordinator.ProcessPendingRespawns(); } }, throttledUpdate: true),
            new FeatureModule("MoneyMultiplier", MoneyMultiplierPatches.Apply,
                syncFromConfig: MoneyMultiplierPatches.RefreshFromConfig),
            new FeatureModule("DungeonTime", DungeonTimePatches.Apply),
            new FeatureModule("MimicTuning", MimicTuningPatches.Apply),
            new FeatureModule("PlayerTuning", PlayerTuningPatches.Apply,
                syncFromConfig: PlayerTuningApplier.RefreshFromConfig,
                onDeinitialize: PlayerTuningApplier.RestoreOnShutdown),
            new FeatureModule("DungeonRandomizer", DungeonRandomizerPatches.Apply),
            new FeatureModule("WebDashboard", WebDashboardServer.Apply,
                syncFromConfig: WebDashboardServer.SyncFromConfig,
                onUpdate: WebDashboardServer.OnUpdate,
                onDeinitialize: WebDashboardServer.StopOnDeinit),
            new FeatureModule("ModVersionDisplay", ModVersionDisplayPatches.Apply),
            new FeatureModule(
                "ExtendedSaveSlots",
                ExtendedSaveSlotsPatches.Apply,
                syncFromConfig: ExtendedSaveSlotsRuntime.RefreshFromConfig),
        ];
    }
}
