using UnityEngine;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class MapPlacedEncounterProximity
    {
        private static readonly Dictionary<SpawnedActorData, CachedBlockResult> CreatureBlockCache = [];
        private static readonly Dictionary<SpawnedActorData, CachedBlockResult> LootBlockCache = [];

        internal static bool ShouldBlockBonusEncounterSpawn(
            DungeonRoom? room,
            SpawnedActorData? spawnData,
            bool throttle = true)
        {
            float minDistance = ResolveMinPlayerDistanceMeters(room);
            return minDistance > 0f
                && room != null
                && spawnData != null
                && IsBonusCreatureEncounter(spawnData)
                && IsPlayerBlockingSpawnCached(room, spawnData, CreatureBlockCache, throttle, minDistance);
        }

        internal static bool ShouldBlockBonusLootRespawn(
            DungeonRoom? room,
            SpawnedActorData? spawnData,
            bool throttle = true)
        {
            float minDistance = ResolveMinPlayerDistanceMeters(room);
            return minDistance > 0f
                && room != null
                && spawnData != null
                && IsBonusLootRespawn(spawnData)
                && IsPlayerBlockingSpawnCached(room, spawnData, LootBlockCache, throttle, minDistance);
        }

        internal static bool IsPlayerBlockingSpawn(DungeonRoom room, Vector3 spawnPos)
        {
            return IsPlayerBlockingSpawn(room, spawnPos, ResolveMinPlayerDistanceMeters(room));
        }

        internal static bool IsPlayerBlockingSpawn(DungeonRoom room, Vector3 spawnPos, float minDistance)
        {
            if (minDistance <= 0f)
            {
                return false;
            }

            List<(VActor actor, double distance)> playersInRange =
                room.GetPlayerActorsInRange(spawnPos, 0f, minDistance, ignoreHeight: true);

            if (playersInRange == null || playersInRange.Count == 0)
            {
                return false;
            }

            foreach ((VActor actor, double distance) in playersInRange)
            {
                if (actor is VPlayer player && player.IsAliveStatus() && distance <= minDistance)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPlayerBlockingSpawnCached(
            DungeonRoom room,
            SpawnedActorData spawnData,
            Dictionary<SpawnedActorData, CachedBlockResult> cache,
            bool throttle,
            float minDistance)
        {
            float now = Time.time;

            if (throttle
                && cache.TryGetValue(spawnData, out CachedBlockResult cached)
                && now < cached.NextCheckAt)
            {
                return cached.Blocked;
            }

            bool blocked = IsPlayerBlockingSpawn(room, spawnData.PosVector, minDistance);
            if (throttle)
            {
                cache[spawnData] = new CachedBlockResult(blocked, now + EncounterSpawnTiming.RetryIntervalSeconds);
            }

            return blocked;
        }

        private static float ResolveMinPlayerDistanceMeters(DungeonRoom? room)
        {
            if (room != null
                && RoomSpawnScalingRegistry.TryGet(room, out RoomSpawnScalingState? state)
                && state.HasSnapshot)
            {
                return state.Snapshot.MapPlacedEncounterMinPlayerDistanceMeters;
            }

            return SceneScopedConfigGate.Spawn.MapPlacedEncounterMinPlayerDistanceMeters;
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

        private static bool IsBonusCreatureEncounter(SpawnedActorData spawnData)
        {
            return spawnData is FixedSpawnedActorData
                && (spawnData.MarkerType.Equals(MapMarkerType.Creature)
                    || spawnData.MarkerType.Equals(MapMarkerType.SpecialMonster))
                && spawnData.ActorID == 0
                && spawnData.CurrentSpawnCount > 0;
        }

        private static bool IsBonusLootRespawn(SpawnedActorData spawnData)
        {
            return spawnData is FixedSpawnedActorData
                && spawnData.MarkerType.Equals(MapMarkerType.LootingObject)
                && spawnData.MasterID > 0
                && spawnData.ActorID == 0
                && spawnData.CurrentSpawnCount > 0;
        }
    }
}
