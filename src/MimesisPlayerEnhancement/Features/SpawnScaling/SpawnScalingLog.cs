using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.SpawnScaling;

internal static class SpawnScalingLog
{
    private const string Feature = "SpawnScaling";

    internal static string FormatPosition(Vector3 pos) =>
        $"({pos.x:0.0}, {pos.y:0.0}, {pos.z:0.0})";

    internal static void InfoScalingApplied(int playerCount)
    {
        float sharedPlayerScale = playerCount > 4 ? playerCount / 4f : 1f;
        ModLog.Info(
            Feature,
            $"Spawn scaling applied — players={playerCount} (shared playerScale={sharedPlayerScale:0.##}× when auto enabled), " +
            $"mimic={ModConfig.MimicSpawnMultiplier.Value:0.##}× auto={ModConfig.AutoScaleMimicSpawnsByPlayerCount.Value}, " +
            $"boss={ModConfig.BossSpawnMultiplier.Value:0.##}× auto={ModConfig.AutoScaleBossSpawnsByPlayerCount.Value}, " +
            $"jako={ModConfig.JakoSpawnMultiplier.Value:0.##}× auto={ModConfig.AutoScaleJakoSpawnsByPlayerCount.Value}, " +
            $"special={ModConfig.SpecialSpawnMultiplier.Value:0.##}× auto={ModConfig.AutoScaleSpecialSpawnsByPlayerCount.Value}, " +
            $"trap={ModConfig.TrapSpawnMultiplier.Value:0.##}× auto={ModConfig.AutoScaleTrapSpawnsByPlayerCount.Value}, " +
            $"other={ModConfig.OtherSpawnMultiplier.Value:0.##}× auto={ModConfig.AutoScaleOtherSpawnsByPlayerCount.Value}");
    }

    internal static void DebugFieldScaled(string label, int before, int after, float multiplier)
    {
        if (before == after)
        {
            ModLog.Debug(Feature, $"{label} unchanged at {before} ({multiplier:0.##}×)");
            return;
        }

        ModLog.Debug(Feature, $"{label} scaled {before} -> {after} ({multiplier:0.##}×)");
    }

    internal static void DebugEntitySpawned(
        int masterId,
        string entityName,
        SpawnCategory category,
        float effectiveMultiplier,
        bool scalingApplied,
        Vector3 position,
        bool isIndoor,
        ReasonOfSpawn reason,
        string spawnSource)
    {
        ModLog.Debug(
            Feature,
            $"Entity spawned — category={SpawnCategoryLookup.Format(category)}, name={entityName}, master={masterId}, " +
            $"multiplier={effectiveMultiplier:0.##}×, budgetsScaled={scalingApplied}, pos={FormatPosition(position)}, " +
            $"indoor={isIndoor}, reason={reason}, source={spawnSource}");
    }

    internal static void DebugSpawnFailed(
        int masterId,
        string entityName,
        SpawnCategory category,
        bool scalingApplied,
        string spawnSource) =>
        ModLog.Debug(
            Feature,
            $"Entity spawn failed — category={SpawnCategoryLookup.Format(category)}, name={entityName}, " +
            $"master={masterId}, budgetsScaled={scalingApplied}, source={spawnSource}");
}
