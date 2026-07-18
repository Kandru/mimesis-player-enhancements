using MimesisPlayerEnhancement.Config;
using MimesisPlayerEnhancement.Features.MimicTuning;
using MimesisPlayerEnhancement.Features.UserInterface;

namespace MimesisPlayerEnhancement.Util
{
    internal sealed class FeatureModule
    {
        private readonly Action<HarmonyLib.Harmony> _applyPatches;
        private readonly Action? _syncFromConfig;
        private readonly Action? _onUpdate;
        private readonly Action? _onDeinitialize;
        private readonly Action<SessionRole, int>? _onSessionStarted;
        private readonly Action? _onSessionEnded;

        internal FeatureModule(
            string name,
            Action<HarmonyLib.Harmony> applyPatches,
            Action? syncFromConfig = null,
            Action? onUpdate = null,
            bool throttledUpdate = false,
            Action? onDeinitialize = null,
            SessionScope sessionScope = SessionScope.HostOnly,
            Action<SessionRole, int>? onSessionStarted = null,
            Action? onSessionEnded = null)
        {
            Name = name;
            SessionScope = sessionScope;
            _applyPatches = applyPatches;
            _syncFromConfig = syncFromConfig;
            _onUpdate = onUpdate;
            _onDeinitialize = onDeinitialize;
            _onSessionStarted = onSessionStarted;
            _onSessionEnded = onSessionEnded;
            if (throttledUpdate)
            {
                ThrottledUpdate = true;
            }
        }

        internal string Name { get; }

        internal SessionScope SessionScope { get; }

        internal bool ThrottledUpdate { get; }

        internal void ApplyPatches(HarmonyLib.Harmony harmony) => _applyPatches(harmony);

        internal void SyncFromConfig() => _syncFromConfig?.Invoke();

        internal void OnUpdate() => _onUpdate?.Invoke();

        internal void OnDeinitialize() => _onDeinitialize?.Invoke();

        internal void InvokeSessionStarted(SessionRole role, int slotId) =>
            _onSessionStarted?.Invoke(role, slotId);

        internal void InvokeSessionEnded() => _onSessionEnded?.Invoke();
    }

    internal static class FeatureModules
    {
        internal static void ResetSharedSessionState() => PlayerLifecycleCoordinator.ClearAll();

        internal static void SyncModuleByName(string moduleName)
        {
            foreach (FeatureModule module in All)
            {
                if (string.Equals(module.Name, moduleName, StringComparison.Ordinal))
                {
                    module.SyncFromConfig();
                    return;
                }
            }
        }

        internal static IReadOnlyList<FeatureModule> All { get; } =
        [
            new FeatureModule("Persistence", PersistencePatches.Apply,
                syncFromConfig: PersistenceRuntime.RefreshFromConfig,
                onUpdate: SpeechEventPoolManager.ProcessDeferredUpdates,
                onDeinitialize: PersistenceWriteQueue.FlushAllSync,
                onSessionEnded: SpeechEventPoolManager.OnSessionEnded),
            new FeatureModule("Statistics", StatisticsPatches.Apply,
                syncFromConfig: StatisticsTracker.RefreshFromConfig,
                onUpdate: () =>
                {
                    if (ModConfig.EnableStatistics.Value) { StatisticsTracker.OnUpdate(); }
                },
                onDeinitialize: SaveSlotSidecarPersistence.FlushAllSync,
                onSessionEnded: StatisticsTracker.OnSessionEnded),
            new FeatureModule("SaveSlotSidecar", SaveSlotSidecarPersistencePatches.Apply,
                onSessionEnded: SaveSlotSidecarPersistence.OnSessionEnded),

            new FeatureModule("MoreVoices", MoreVoicesPatches.Apply, MoreVoicesPatches.RefreshFromConfig,
                onDeinitialize: VoicePerformanceRuntime.ClearAll,
                onSessionEnded: VoicePerformanceRuntime.ClearAll),
            new FeatureModule("PlayerAnnouncements", PlayerAnnouncementsPatches.Apply,
                onSessionEnded: () =>
                {
                    PlayerAnnouncements.ResetForSessionEnd();
                    BossSpawnAnnouncer.ResetForSessionEnd();
                    MapRunStatsTracker.ResetForSessionEnd();
                }),
            new FeatureModule("MorePlayers", MorePlayersPatches.Apply, MorePlayersPatches.RefreshFromConfig,
                onSessionEnded: MorePlayersPatches.OnSessionEnded),
            new FeatureModule("JoinAnytime", JoinAnytimePatches.Apply,
                syncFromConfig: JoinAnytimeRuntime.RefreshFromConfig,
                onUpdate: JoinAnytimeRuntime.OnUpdate,
                onSessionEnded: JoinAnytimeRuntime.ResetSessionState),
            new FeatureModule("SpawnScaling", SpawnScalingPatches.Apply,
                syncFromConfig: SpawnScalingPatches.RefreshFromConfig,
                onUpdate: () =>
                {
                    if (ModConfig.EnableSpawnScaling.Value) { MapPlacedEncounterScheduler.ProcessPendingEncounters(); }
                },
                throttledUpdate: true),
            new FeatureModule("LootMultiplicator", LootMultiplicatorPatches.Apply,
                syncFromConfig: LootMultiplicatorPatches.RefreshFromConfig,
                onUpdate: () =>
                {
                    if (SceneScopedConfigGate.Loot.EnableLootMultiplicator)
                    {
                        FixedLootSpawnCoordinator.ProcessPendingRespawns();
                    }
                },
                onSessionEnded: FixedLootSpawnCoordinator.ClearPendingRespawns,
                throttledUpdate: true),
            new FeatureModule("Economy", EconomyPatches.Apply,
                syncFromConfig: EconomyPatches.RefreshFromConfig,
                onSessionEnded: EconomyPatches.OnSessionEnded),
            new FeatureModule("DungeonTime", DungeonTimePatches.Apply),
            new FeatureModule("MimicTuning", MimicTuningPatches.Apply,
                syncFromConfig: MimicTuningRuntime.RefreshFromConfig,
                onSessionEnded: MimicTuningRuntime.OnSessionEnded),
            new FeatureModule("Ui", UiPatches.Apply,
                syncFromConfig: UiRuntime.RefreshFromConfig,
                onUpdate: UiRuntime.OnUpdate,
                sessionScope: SessionScope.ClientAllowed,
                onSessionEnded: UiRuntime.OnSessionEnded),
            new FeatureModule("Privacy", PrivacyPatches.Apply,
                syncFromConfig: PrivacyRuntime.RefreshFromConfig,
                onDeinitialize: PrivacyRuntime.RestoreOnShutdown,
                sessionScope: SessionScope.Global),
            new FeatureModule("PlayerTuning", PlayerTuningPatches.Apply,
                syncFromConfig: PlayerTuningApplier.RefreshFromConfig,
                onDeinitialize: PlayerTuningApplier.RestoreOnShutdown,
                onSessionEnded: PlayerTuningApplier.OnSessionEnded),
            new FeatureModule("DungeonRandomizer", DungeonRandomizerPatches.Apply,
                onSessionEnded: DungeonRandomizerRuntime.OnSessionEnded),
            new FeatureModule("Weather", WeatherPatches.Apply,
                syncFromConfig: WeatherPatches.RefreshFromConfig,
                onUpdate: WeatherCycleScheduler.ProcessPendingTransitions,
                throttledUpdate: true),
            new FeatureModule("WebDashboard", WebDashboardServer.Apply,
                syncFromConfig: WebDashboardServer.SyncFromConfig,
                onUpdate: WebDashboardServer.OnUpdate,
                onDeinitialize: WebDashboardServer.StopOnDeinit,
                sessionScope: SessionScope.Global,
                onSessionEnded: WebDashboardSnapshotCache.OnSessionEnded),
        ];
    }
}
