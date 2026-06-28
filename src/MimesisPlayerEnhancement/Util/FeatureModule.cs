using System;
using System.Collections.Generic;
using MimesisPlayerEnhancement.Features.DungeonRandomizer;
using MimesisPlayerEnhancement.Features.DungeonSizeScaling;
using MimesisPlayerEnhancement.Features.DungeonTime;
using MimesisPlayerEnhancement.Features.JoinAnytime;
using MimesisPlayerEnhancement.Features.LootMultiplicator;
using MimesisPlayerEnhancement.Features.MoneyMultiplier;
using MimesisPlayerEnhancement.Features.MorePlayers;
using MimesisPlayerEnhancement.Features.MoreVoices;
using MimesisPlayerEnhancement.Features.Persistence;
using MimesisPlayerEnhancement.Features.PlayerAnnouncements;
using MimesisPlayerEnhancement.Features.SpawnScaling;
using MimesisPlayerEnhancement.Features.SpectatorTransition;
using MimesisPlayerEnhancement.Features.Statistics;

namespace MimesisPlayerEnhancement.Util;

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
}

internal sealed class FeatureModule : IFeatureModule
{
    private readonly Action<HarmonyLib.Harmony> _applyPatches;
    private readonly Action? _syncFromConfig;
    private readonly Action? _onUpdate;

    public FeatureModule(
        string name,
        Action<HarmonyLib.Harmony> applyPatches,
        Action? syncFromConfig = null,
        Action? onUpdate = null,
        bool throttledUpdate = false)
    {
        Name = name;
        _applyPatches = applyPatches;
        _syncFromConfig = syncFromConfig;
        _onUpdate = onUpdate;
        if (throttledUpdate)
            ThrottledUpdate = true;
    }

    public string Name { get; }

    internal bool ThrottledUpdate { get; }

    public void ApplyPatches(HarmonyLib.Harmony harmony) => _applyPatches(harmony);

    public void SyncFromConfig() => _syncFromConfig?.Invoke();

    public void OnUpdate() => _onUpdate?.Invoke();
}

internal static class FeatureModules
{
    internal static IReadOnlyList<IFeatureModule> All { get; } = new IFeatureModule[]
    {
        new FeatureModule("MoreVoices", MoreVoicesPatches.Apply, MoreVoicesPatches.RefreshFromConfig),
        new FeatureModule("Persistence", PersistencePatches.Apply, onUpdate: () =>
        {
            if (ModConfig.EnablePersistence.Value)
                SpeechEventPoolManager.ProcessDeferredUpdates();
        }),
        new FeatureModule("Statistics", StatisticsPatches.Apply, onUpdate: () =>
        {
            if (ModConfig.EnableStatistics.Value)
                StatisticsTracker.OnUpdate();
        }),
        new FeatureModule("PlayerAnnouncements", PlayerAnnouncementPatches.Apply),
        new FeatureModule("MorePlayers", MorePlayersPatches.Apply, MorePlayersPatches.RefreshFromConfig),
        new FeatureModule("JoinAnytime", JoinAnytimePatches.Apply),
        new FeatureModule("SpawnScaling", SpawnScalingPatches.Apply, onUpdate: () =>
        {
            if (ModConfig.EnableSpawnScaling.Value)
                FixedSpawnCoordinator.ProcessPendingRespawns();
        }, throttledUpdate: true),
        new FeatureModule("LootMultiplicator", LootMultiplicatorPatches.Apply, onUpdate: () =>
        {
            if (ModConfig.EnableLootMultiplicator.Value)
                FixedLootSpawnCoordinator.ProcessPendingRespawns();
        }, throttledUpdate: true),
        new FeatureModule("MoneyMultiplier", MoneyMultiplierPatches.Apply),
        new FeatureModule("DungeonTime", DungeonTimePatches.Apply),
        new FeatureModule("SpectatorTransition", SpectatorTransitionPatches.Apply),
        new FeatureModule("DungeonRandomizer", DungeonRandomizerPatches.Apply),
        new FeatureModule("DungeonSizeScaling", DungeonSizeScalingPatches.Apply),
    };
}
