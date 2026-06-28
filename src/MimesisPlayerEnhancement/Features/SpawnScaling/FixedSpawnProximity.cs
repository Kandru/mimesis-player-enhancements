using System.Collections.Generic;
using Bifrost.ConstEnum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.SpawnScaling;

internal static class FixedSpawnProximity
{
    internal static bool ShouldBlockFixedCreatureRespawn(DungeonRoom? room, SpawnedActorData? spawnData)
    {
        if (ModConfig.FixedSpawnRespawnMinPlayerDistanceMeters.Value <= 0f || room == null || spawnData == null)
            return false;

        if (!IsFixedCreatureRespawn(spawnData))
            return false;

        return IsPlayerBlockingRespawn(room, spawnData.PosVector);
    }

    internal static bool ShouldBlockFixedLootRespawn(DungeonRoom? room, SpawnedActorData? spawnData)
    {
        if (ModConfig.FixedSpawnRespawnMinPlayerDistanceMeters.Value <= 0f || room == null || spawnData == null)
            return false;

        if (!IsFixedLootRespawn(spawnData))
            return false;

        return IsPlayerBlockingRespawn(room, spawnData.PosVector);
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
