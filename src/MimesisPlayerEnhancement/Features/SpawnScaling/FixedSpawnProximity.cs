using System.Collections.Generic;
using Bifrost.ConstEnum;
using MimesisPlayerEnhancement.Util;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.SpawnScaling;

internal static class FixedSpawnProximity
{
    private static readonly Dictionary<SpawnedActorData, CachedBlockResult> CreatureBlockCache = new();
    private static readonly Dictionary<SpawnedActorData, CachedBlockResult> LootBlockCache = new();

    internal static bool ShouldBlockFixedCreatureRespawn(
        DungeonRoom? room,
        SpawnedActorData? spawnData,
        bool throttle = true)
    {
        if (ModConfig.FixedSpawnRespawnMinPlayerDistanceMeters.Value <= 0f || room == null || spawnData == null)
            return false;

        if (!IsFixedCreatureRespawn(spawnData))
            return false;

        return IsPlayerBlockingRespawnCached(room, spawnData, CreatureBlockCache, throttle);
    }

    internal static bool ShouldBlockFixedLootRespawn(
        DungeonRoom? room,
        SpawnedActorData? spawnData,
        bool throttle = true)
    {
        if (ModConfig.FixedSpawnRespawnMinPlayerDistanceMeters.Value <= 0f || room == null || spawnData == null)
            return false;

        if (!IsFixedLootRespawn(spawnData))
            return false;

        return IsPlayerBlockingRespawnCached(room, spawnData, LootBlockCache, throttle);
    }

    internal static bool IsPlayerBlockingRespawn(DungeonRoom room, Vector3 spawnPos)
    {
        float minDistance = ModConfig.FixedSpawnRespawnMinPlayerDistanceMeters.Value;
        if (minDistance <= 0f)
            return false;

        List<(VActor actor, double distance)> playersInRange =
            room.GetPlayerActorsInRange(spawnPos, 0f, minDistance, ignoreHeight: true);

        if (playersInRange == null || playersInRange.Count == 0)
            return false;

        foreach ((VActor actor, double distance) in playersInRange)
        {
            if (actor is VPlayer player && player.IsAliveStatus() && distance <= minDistance)
                return true;
        }

        return false;
    }

    private static bool IsPlayerBlockingRespawnCached(
        DungeonRoom room,
        SpawnedActorData spawnData,
        Dictionary<SpawnedActorData, CachedBlockResult> cache,
        bool throttle)
    {
        float now = Time.time;

        if (throttle
            && cache.TryGetValue(spawnData, out CachedBlockResult cached)
            && now < cached.NextCheckAt)
        {
            return cached.Blocked;
        }

        bool blocked = IsPlayerBlockingRespawn(room, spawnData.PosVector);
        if (throttle)
            cache[spawnData] = new CachedBlockResult(blocked, now + FixedRespawnTiming.RetryIntervalSeconds);

        return blocked;
    }

    private readonly struct CachedBlockResult
    {
        internal CachedBlockResult(bool blocked, float nextCheckAt)
        {
            Blocked = blocked;
            NextCheckAt = nextCheckAt;
        }

        internal bool Blocked { get; }

        internal float NextCheckAt { get; }
    }

    private static bool IsFixedCreatureRespawn(SpawnedActorData spawnData) =>
        spawnData is FixedSpawnedActorData
        && (spawnData.MarkerType.Equals(MapMarkerType.Creature)
            || spawnData.MarkerType.Equals(MapMarkerType.SpecialMonster))
        && spawnData.ActorID == 0
        && spawnData.CurrentSpawnCount > 0;

    private static bool IsFixedLootRespawn(SpawnedActorData spawnData) =>
        spawnData is FixedSpawnedActorData
        && spawnData.MarkerType.Equals(MapMarkerType.LootingObject)
        && spawnData.MasterID > 0
        && spawnData.ActorID == 0
        && spawnData.CurrentSpawnCount > 0;
}
