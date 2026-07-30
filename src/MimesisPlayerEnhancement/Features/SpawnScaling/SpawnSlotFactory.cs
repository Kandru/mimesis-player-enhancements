using System.Collections;
using System.Reflection;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class SpawnSlotFactory
    {
        private const string Feature = "SpawnScaling";

        internal const int SyntheticIndexBase = 900_000;
        internal const int MaxSyntheticSlotsPerRoom = 120;

        private const float JitterMinMeters = 2f;
        private const float JitterMaxMeters = 8f;
        private const float MinSeparationMeters = 2f;
        private const float NavSnapRadiusMeters = 2f;
        private const float MaxFloorDropMeters = 0.5f;
        private const float FloorProbeMarginMeters = 1f;
        private const float BlockCheckRadiusMeters = 0.4f;
        private const int PlacementAttempts = 8;

        private static readonly MethodInfo SpawnMonsterMethod =
            AccessTools.Method(typeof(IVroom), "SpawnMonster",
            [
                typeof(int),
                typeof(SpawnedActorData),
                typeof(bool),
                typeof(string),
                typeof(string),
                typeof(ReasonOfSpawn),
            ])
            ?? throw new InvalidOperationException("IVroom.SpawnMonster not found");

        internal readonly struct MapPlacedScaleResult
        {
            internal MapPlacedScaleResult(int recovered, int synthesized, int shortfall)
            {
                Recovered = recovered;
                Synthesized = synthesized;
                Shortfall = shortfall;
            }

            internal int Recovered { get; }

            internal int Synthesized { get; }

            internal int Shortfall { get; }
        }

        internal static bool MaySynthesize(SpawnCategory category)
        {
            return category != SpawnCategory.Trap;
        }

        internal static int ComputeAmbientExpandCount(int poolSize, float jakoMultiplier, float mimicMultiplier, int alreadySynthetic)
        {
            if (poolSize <= 0)
            {
                return 0;
            }

            float multiplier = Math.Max(jakoMultiplier, mimicMultiplier);
            if (multiplier <= FeatureToggleGate.NeutralMultiplier)
            {
                return 0;
            }

            int target = ScalingMath.ScaleCount(poolSize, multiplier);
            int need = target - poolSize;
            int remainingCap = Math.Max(0, MaxSyntheticSlotsPerRoom - alreadySynthetic);
            return Math.Max(0, Math.Min(need, remainingCap));
        }

        internal static MapPlacedScaleResult ScaleMapPlacedGroup(
            DungeonRoom room,
            RoomSpawnScalingState state,
            IDictionary spawnDatas,
            int masterId,
            SpawnCategory category,
            IReadOnlyList<RoomSpawnScalingState.EncounterSlot> slots,
            int need,
            MapMarker_CreatureSpawnPoint[]? allMarkers = null)
        {
            if (need <= 0)
            {
                return new MapPlacedScaleResult(0, 0, 0);
            }

            HashSet<int> usedMarkerIds = [];
            FixedSpawnedActorData? template = null;
            List<Vector3> occupied = [];

            foreach (RoomSpawnScalingState.EncounterSlot slot in slots)
            {
                _ = usedMarkerIds.Add(slot.MarkerId);
                template ??= slot.Data;
                occupied.Add(slot.Data.PosVector);
            }

            if (template == null)
            {
                return new MapPlacedScaleResult(0, 0, need);
            }

            allMarkers ??= CreatureSpawnMarkerAccess.CollectSceneMarkers();
            List<MapMarker_CreatureSpawnPoint> unusedMarkers =
                CreatureSpawnMarkerAccess.CollectUnusedMarkers(masterId, usedMarkerIds, allMarkers);
            CreatureSpawnMarkerAccess.ShuffleMarkers(unusedMarkers);

            int recovered = RecoverMarkers(room, state, spawnDatas, unusedMarkers, need, occupied);
            int remaining = need - recovered;
            int synthesized = 0;

            if (remaining > 0 && MaySynthesize(category))
            {
                synthesized = SynthesizeFixedSlots(
                    room,
                    state,
                    spawnDatas,
                    template,
                    slots,
                    remaining,
                    occupied,
                    category);
                remaining -= synthesized;
            }

            if (remaining > 0)
            {
                ModLog.Warn(Feature, $"Map-placed density shortfall — category={SpawnCategoryLookup.Format(category)}, master={masterId}, " +
                    $"missing={remaining} slots (recovered={recovered}, synthesized={synthesized})");
            }

            return new MapPlacedScaleResult(recovered, synthesized, remaining);
        }

        internal static int ExpandAmbientPool(
            DungeonRoom room,
            RoomSpawnScalingState state,
            int playerCount,
            SpawnScalingSceneConfig config)
        {
            if (SpawnScalingFields.SpawnedActorDatasField.GetValue(room) is not IDictionary spawnDatas)
            {
                return 0;
            }

            float jakoMultiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Jako, playerCount, config);
            float mimicMultiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(SpawnCategory.Mimic, playerCount, config);
            List<RandomSpawnedMonsterActorData> pool = [];

            foreach (DictionaryEntry entry in spawnDatas)
            {
                if (entry.Value is not RandomSpawnedMonsterActorData ambient)
                {
                    continue;
                }

                if (!ambient.MarkerType.Equals(MapMarkerType.Creature)
                    || !ambient.SpawnType.Equals(SpawnType.Periodic)
                    || ambient.MasterID != 0)
                {
                    continue;
                }

                pool.Add(ambient);
            }

            int need = ComputeAmbientExpandCount(pool.Count, jakoMultiplier, mimicMultiplier, state.SyntheticSlotCount);
            if (need <= 0)
            {
                return 0;
            }

            List<Vector3> occupied = pool.ConvertAll(slot => slot.PosVector);
            CreatureSpawnMarkerAccess.ShuffleMarkers(pool);
            int added = 0;

            for (int i = 0; i < need; i++)
            {
                if (!state.CanAddSyntheticSlot())
                {
                    break;
                }

                RandomSpawnedMonsterActorData anchor = pool[i % pool.Count];
                if (!TryResolveJitteredPosition(anchor.PosVector, occupied, out Vector3 pos))
                {
                    continue;
                }

                int index = AllocateSyntheticIndex(spawnDatas, state);
                SpawnedActorData clone = CloneSlot(anchor, index, pos, UnityEngine.Random.Range(0f, 360f));
                spawnDatas.Add(index, clone);
                state.TrackSyntheticSlot();
                added++;
            }

            if (added > 0)
            {
                float multiplier = Math.Max(jakoMultiplier, mimicMultiplier);
                ModLog.Info(Feature, $"Ambient spawn pool expanded — vanilla={pool.Count}, target={pool.Count + need}, " +
                    $"synthetic={added}, multiplier={multiplier:0.##}×");
            }
            else if (need > 0)
            {
                ModLog.Warn(Feature, $"Ambient spawn pool expansion failed — need={need}, pool={pool.Count}");
            }

            return added;
        }

        private static int RecoverMarkers(
            DungeonRoom room,
            RoomSpawnScalingState state,
            IDictionary spawnDatas,
            List<MapMarker_CreatureSpawnPoint> unusedMarkers,
            int need,
            List<Vector3> occupied)
        {
            int recovered = 0;

            for (int i = 0; i < unusedMarkers.Count && recovered < need; i++)
            {
                MapMarker_CreatureSpawnPoint marker = unusedMarkers[i];
                if (spawnDatas.Contains(marker.ID))
                {
                    continue;
                }

                FixedSpawnedActorData spawnData = new(marker);
                spawnDatas.Add(marker.ID, spawnData);
                state.RegisterSlot(marker.ID, spawnData);
                occupied.Add(spawnData.PosVector);
                recovered++;

                if (spawnData.MarkerType.Equals(MapMarkerType.SpecialMonster))
                {
                    if (!TryBindSpecialSlot(room, spawnData))
                    {
                        spawnDatas.Remove(marker.ID);
                        state.UnregisterSlot(marker.ID, spawnData);
                        occupied.RemoveAt(occupied.Count - 1);
                        recovered--;
                        continue;
                    }
                }
                else if (!TryActivateCreatureSlot(room, spawnData))
                {
                    spawnDatas.Remove(marker.ID);
                    state.UnregisterSlot(marker.ID, spawnData);
                    occupied.RemoveAt(occupied.Count - 1);
                    recovered--;
                    continue;
                }

                if (ModConfig.EnableDebugLogging.Value)
                {
                    ModLog.Debug(Feature, $"Recovered inactive marker — master={marker.masterID}, marker={marker.ID}, " +
                        $"pos={SpawnScalingLog.FormatLocation(room, marker.pos.pos)}");
                }
            }

            return recovered;
        }

        private static int SynthesizeFixedSlots(
            DungeonRoom room,
            RoomSpawnScalingState state,
            IDictionary spawnDatas,
            FixedSpawnedActorData template,
            IReadOnlyList<RoomSpawnScalingState.EncounterSlot> slots,
            int need,
            List<Vector3> occupied,
            SpawnCategory category)
        {
            int synthesized = 0;

            for (int i = 0; i < need && state.CanAddSyntheticSlot(); i++)
            {
                RoomSpawnScalingState.EncounterSlot anchorSlot = slots[i % slots.Count];
                if (!TryResolveJitteredPosition(anchorSlot.Data.PosVector, occupied, out Vector3 pos))
                {
                    break;
                }

                int index = AllocateSyntheticIndex(spawnDatas, state);
                FixedSpawnedActorData spawnData = (FixedSpawnedActorData)CloneSlot(
                    template,
                    index,
                    pos,
                    UnityEngine.Random.Range(0f, 360f));

                spawnDatas.Add(index, spawnData);
                state.RegisterSlot(index, spawnData);
                state.TrackSyntheticSlot();
                occupied.Add(pos);
                synthesized++;

                if (spawnData.MarkerType.Equals(MapMarkerType.SpecialMonster))
                {
                    if (!TryBindSpecialSlot(room, spawnData))
                    {
                        spawnDatas.Remove(index);
                        state.UnregisterSlot(index, spawnData);
                        occupied.RemoveAt(occupied.Count - 1);
                        synthesized--;
                        state.TrackSyntheticSlotRollback();
                        continue;
                    }
                }
                else if (!TryActivateCreatureSlot(room, spawnData))
                {
                    spawnDatas.Remove(index);
                    state.UnregisterSlot(index, spawnData);
                    occupied.RemoveAt(occupied.Count - 1);
                    synthesized--;
                    state.TrackSyntheticSlotRollback();
                    continue;
                }

                if (ModConfig.EnableDebugLogging.Value)
                {
                    ModLog.Debug(Feature, $"Synthetic slot placed — category={SpawnCategoryLookup.Format(category)}, master={template.MasterID}, " +
                        $"marker={index}, pos={SpawnScalingLog.FormatLocation(room, pos)}");
                }
            }

            return synthesized;
        }

        private static bool TryBindSpecialSlot(DungeonRoom room, SpawnedActorData slot)
        {
            if (SpawnScalingFields.SpecialMonsterSpawnGroupsField.GetValue(room) is not IList groups)
            {
                return false;
            }

            foreach (object group in groups)
            {
                if (group == null)
                {
                    continue;
                }

                if (SpawnScalingFields.SpecialGroupInfoField.GetValue(group) is not SpecialMonsterSpawnInfo spawnInfo)
                {
                    continue;
                }

                if (spawnInfo.MasterID != slot.MasterID)
                {
                    continue;
                }

                if (SpawnScalingFields.SpecialGroupDedicatedSpawnedActorDatasField.GetValue(group) is IList dedicated)
                {
                    dedicated.Add(slot);
                    return true;
                }
            }

            return false;
        }

        private static bool TryActivateCreatureSlot(DungeonRoom room, SpawnedActorData spawnData)
        {
            if (room is not IVroom vroom || spawnData.ActorID != 0)
            {
                return false;
            }

            return (bool)SpawnMonsterMethod.Invoke(
                vroom,
                [
                    spawnData.MasterID,
                    spawnData,
                    spawnData.IsIndoor,
                    spawnData.AIName,
                    spawnData.BTName,
                    ReasonOfSpawn.EventAction,
                ]);
        }

        private static int AllocateSyntheticIndex(IDictionary spawnDatas, RoomSpawnScalingState state)
        {
            int next = Math.Max(SyntheticIndexBase, state.NextSyntheticIndex);
            foreach (object key in spawnDatas.Keys)
            {
                if (key is int markerId && markerId >= next)
                {
                    next = markerId + 1;
                }
            }

            while (spawnDatas.Contains(next))
            {
                next++;
            }

            state.NextSyntheticIndex = next + 1;
            return next;
        }

        private static SpawnedActorData CloneSlot(SpawnedActorData template, int index, Vector3 pos, float yaw)
        {
            SpawnedActorData clone = (SpawnedActorData)SpawnScalingFields.MemberwiseCloneMethod.Invoke(template, null)!;
            PosWithRot posWithRot = pos.toPosWithRot(yaw);
            long now = GameSessionAccess.TryGetTimeUtil()?.GetCurrentTickMilliSec() ?? 0L;

            SpawnScalingFields.SpawnDataIndexField.SetValue(clone, index);
            SpawnScalingFields.SpawnDataPosField.SetValue(clone, posWithRot);
            SpawnScalingFields.SpawnDataPosVectorField.SetValue(clone, pos);
            SpawnScalingFields.SpawnDataActorIdBackingField.SetValue(clone, 0);
            SpawnScalingFields.CurrentSpawnCountBackingField.SetValue(clone, 0);
            SpawnScalingFields.SpawnDataSpawnWaitStartTimeBackingField.SetValue(clone, now);
            SpawnScalingFields.SpawnDataLastSpawnTimeBackingField.SetValue(clone, now);

            return clone;
        }

        private static bool TryResolveJitteredPosition(
            Vector3 anchor,
            List<Vector3> occupied,
            out Vector3 result)
        {
            result = default;

            VWorld? vworld = GameSessionAccess.TryGetVWorld();
            if (vworld == null)
            {
                return false;
            }

            for (int attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = UnityEngine.Random.Range(JitterMinMeters, JitterMaxMeters);
                Vector3 candidate = anchor + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * distance;

                Vector3 snapped = vworld.FindNearestPoly(candidate, NavSnapRadiusMeters);
                if (!NavMeshConstants.IsValid(snapped))
                {
                    continue;
                }

                Vector3 probeOrigin = new(snapped.x, anchor.y, snapped.z);
                Vector3 floorHit = PhysicsUtility.DropToFloor(
                    probeOrigin,
                    margin: FloorProbeMarginMeters,
                    maxDistance: FloorProbeMarginMeters + MaxFloorDropMeters);
                if (floorHit == probeOrigin)
                {
                    continue;
                }

                if (anchor.y - floorHit.y > MaxFloorDropMeters)
                {
                    continue;
                }

                Vector3 placed = new(snapped.x, floorHit.y, snapped.z);
                if (IsTooClose(placed, occupied))
                {
                    continue;
                }

                if (vworld.IsFullyBlockedByWall(anchor, placed, BlockCheckRadiusMeters, BlockCheckRadiusMeters))
                {
                    continue;
                }

                occupied.Add(placed);
                result = placed;
                return true;
            }

            return false;
        }

        internal static bool IsTooClose(Vector3 candidate, IReadOnlyList<Vector3> occupied)
        {
            float minSeparationSq = MinSeparationMeters * MinSeparationMeters;
            foreach (Vector3 existing in occupied)
            {
                float dx = candidate.x - existing.x;
                float dz = candidate.z - existing.z;
                if (dx * dx + dz * dz < minSeparationSq)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
