using System.Collections;
using System.Reflection;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class FixedLootSpawnCoordinator
    {
        private const string Feature = "LootMultiplicator";

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Type[] SpawnLootingObjectParameterTypes =
        [
            typeof(ItemElement),
            typeof(PosWithRot),
            typeof(bool),
            typeof(ReasonOfSpawn),
            typeof(int),
            typeof(int),
            typeof(long),
            typeof(bool),
            typeof(bool),
        ];

        private static readonly FieldInfo SpawnedActorDatasField =
            typeof(DungeonRoom).GetField("_spawnedActorDatas", InstanceFlags)
            ?? throw new InvalidOperationException("DungeonRoom._spawnedActorDatas not found");

        private static readonly MethodInfo GetNewItemElementMethod =
            AccessTools.Method(typeof(IVroom), "GetNewItemElement",
            [
                typeof(int),
                typeof(bool),
                typeof(int),
                typeof(int),
                typeof(int),
                typeof(int),
            ])
            ?? throw new InvalidOperationException("IVroom.GetNewItemElement not found");

        private static readonly MethodInfo ExecuteLootingObjectSpawnMethod =
            AccessTools.Method(typeof(IVroom), "ExecuteLootingObjectSpawn", [typeof(SpawnedActorData)])
            ?? throw new InvalidOperationException("IVroom.ExecuteLootingObjectSpawn not found");

        private static readonly MethodInfo MemberwiseCloneMethod =
            AccessTools.Method(typeof(object), "MemberwiseClone")
            ?? throw new InvalidOperationException("object.MemberwiseClone not found");

        private static readonly MethodInfo SpawnLootingObjectMethod =
            AccessTools.Method(typeof(IVroom), "SpawnLootingObject", SpawnLootingObjectParameterTypes)
            ?? throw new InvalidOperationException("IVroom.SpawnLootingObject not found");

        private static readonly FieldInfo CurrentSpawnCountBackingField =
            AccessToolsField(typeof(SpawnedActorData), "<CurrentSpawnCount>k__BackingField");

        private static readonly FieldInfo SpawnDataIndexField =
            AccessToolsField(typeof(SpawnedActorData), "Index");

        private static readonly DungeonRoomStateRegistry<RoomState> RoomStates = new();
        private static readonly List<PendingRespawn> PendingRespawns = [];

        /// <summary>Drops queued loot respawns so a disabled feature cannot spawn them later.</summary>
        internal static void ClearPendingRespawns()
        {
            if (PendingRespawns.Count == 0)
            {
                return;
            }

            ModLog.Debug(Feature, $"Pending loot respawns cleared — {PendingRespawns.Count} dropped");
            PendingRespawns.Clear();
        }

        internal static void ApplyAfterInit(DungeonRoom room)
        {
            if (!LootScalingGate.ShouldScale(room)
                || DungeonRoomAppliedSet.IsApplied(room, DungeonRoomApplyKind.FixedLootCoordination))
            {
                return;
            }

            DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.FixedLootCoordination);

            if (SpawnedActorDatasField.GetValue(room) is not IDictionary spawnDatas || spawnDatas.Count == 0)
            {
                return;
            }

            int playerCount = room.GetMemberCount();
            LootMultiplicatorSceneConfig lootConfig = SceneScopedConfigGate.Loot;
            RoomState state = new(room, lootConfig);

            foreach (DictionaryEntry entry in spawnDatas)
            {
                if (entry.Value is not FixedSpawnedActorData spawnData)
                {
                    continue;
                }

                if (!spawnData.MarkerType.Equals(MapMarkerType.LootingObject))
                {
                    continue;
                }

                state.RegisterSlot(entry.Key as string ?? spawnData.Name, spawnData);
            }

            foreach (KeyValuePair<int, List<SpawnSlot>> group in state.GroupSlotsByMasterId())
            {
                int masterId = group.Key;
                if (masterId <= 0)
                {
                    continue;
                }

                ItemType itemType = ItemTypeLookup.GetItemType(masterId);
                float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(
                    LootSource.Map,
                    itemType,
                    playerCount,
                    masterId,
                    lootConfig);
                int vanillaCount = group.Value.Count;
                int targetTotal = LootMultiplierResolver.ScaleCount(vanillaCount, multiplier);
                int need = targetTotal - vanillaCount;

                if (need <= 0)
                {
                    continue;
                }

                HashSet<int> usedMarkerIds = [];
                FixedSpawnedActorData? template = null;

                foreach (SpawnSlot slot in group.Value)
                {
                    _ = usedMarkerIds.Add(slot.Data.Index);
                    template ??= slot.Data;
                }

                if (template == null)
                {
                    continue;
                }

                List<MapMarker_LootingObjectSpawnPoint> unusedMarkers =
                    MapLootMarkerIndex.CollectUnusedMarkers(masterId, usedMarkerIds);
                MapLootMarkerIndex.ShuffleMarkers(unusedMarkers);

                int activated = ActivateUnusedMarkers(room, state, spawnDatas, template, unusedMarkers, need);
                int remainingQuota = need - activated;
                state.SetRemainingQuota(masterId, remainingQuota);

                ModLog.Info(Feature, $"Fixed loot scaling — type={itemType}, master={masterId}, " +
                    $"{multiplier:0.##}× (vanilla={vanillaCount}, target={targetTotal}, markers+={activated}, respawnQuota={remainingQuota})");
            }

            if (state.HasQuotas || state.SlotCount > 0)
            {
                RoomStates.Register(room, state);
            }

            LootSpawnDataLookup.RebuildIndex(room);
        }

        internal static void OnActorDead(SpawnedActorData spawnData)
        {
            if (!LootScalingGate.ShouldScale() || spawnData == null)
            {
                return;
            }

            if (spawnData is not FixedSpawnedActorData fixedSpawn)
            {
                return;
            }

            if (!fixedSpawn.MarkerType.Equals(MapMarkerType.LootingObject))
            {
                return;
            }

            if (!TryFindRoomState(spawnData, out RoomState? state, out DungeonRoom? room))
            {
                return;
            }

            if (!state.TryConsumeQuota(spawnData.MasterID))
            {
                return;
            }

            ScheduleRespawn(room, spawnData, spawnData.MasterID);
        }

        internal static void ProcessPendingRespawns()
        {
            if (PendingRespawns.Count == 0
                || !SceneScopedConfigGate.Loot.EnableLootMultiplicator
                || !HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            float now = Time.time;

            for (int i = PendingRespawns.Count - 1; i >= 0; i--)
            {
                PendingRespawn pending = PendingRespawns[i];
                if (now < pending.ExecuteAt)
                {
                    continue;
                }

                if (now < pending.NextAttemptAt)
                {
                    continue;
                }

                if (pending.Room == null || pending.Data == null)
                {
                    PendingRespawns.RemoveAt(i);
                    continue;
                }

                if (MapPlacedEncounterProximity.ShouldBlockBonusLootRespawn(pending.Room, pending.Data, throttle: false))
                {
                    if (ModConfig.EnableDebugLogging.Value)
                    {
                        ModLog.Debug(Feature, $"Fixed loot respawn waiting — master={pending.MasterId}, marker={pending.Data.Index}, " +
                            $"players within {ResolveMinPlayerDistanceMeters(pending.Room):0.#}m");
                    }

                    DeferNextAttempt(i, pending, now);
                    continue;
                }

                if (!RoomStates.TryGet(pending.Room, out _))
                {
                    PendingRespawns.RemoveAt(i);
                    continue;
                }

                try
                {
                    if (TrySpawnFixedLoot(pending.Room, pending.Data, isRespawn: true))
                    {
                        PendingRespawns.RemoveAt(i);
                        continue;
                    }

                    if (pending.Data.ActorID != 0)
                    {
                        PendingRespawns.RemoveAt(i);
                        continue;
                    }

                    DeferNextAttempt(i, pending, now);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Fixed loot respawn failed — master={pending.MasterId}: {ex.Message}");
                    DeferNextAttempt(i, pending, now);
                }
            }
        }

        private static int ActivateUnusedMarkers(
            DungeonRoom room,
            RoomState state,
            IDictionary spawnDatas,
            FixedSpawnedActorData template,
            List<MapMarker_LootingObjectSpawnPoint> unusedMarkers,
            int need)
        {
            int activated = 0;

            for (int i = 0; i < unusedMarkers.Count && activated < need; i++)
            {
                MapMarker_LootingObjectSpawnPoint marker = unusedMarkers[i];
                string key = marker.Name;

                if (string.IsNullOrWhiteSpace(key) || spawnDatas.Contains(key))
                {
                    key = $"{marker.masterID}_{marker.ID}";
                }

                if (spawnDatas.Contains(key))
                {
                    continue;
                }

                FixedSpawnedActorData spawnData = CreateSpawnDataFromMarker(template, marker);
                spawnDatas.Add(key, spawnData);
                state.RegisterSlot(key, spawnData);

                try
                {
                    ExecuteLootingObjectSpawn(room, spawnData);
                    activated++;
                }
                catch (Exception ex)
                {
                    spawnDatas.Remove(key);
                    ModLog.Warn(Feature, $"Fixed loot marker activation failed — master={marker.masterID}, marker={marker.ID}: {ex.Message}");
                }
            }

            return activated;
        }

        private static FixedSpawnedActorData CreateSpawnDataFromMarker(
            FixedSpawnedActorData template,
            MapMarker_LootingObjectSpawnPoint marker)
        {
            FixedSpawnedActorData data = (FixedSpawnedActorData)MemberwiseCloneMethod.Invoke(template, null);
            data.SetActorID(0);
            SetField(data, CurrentSpawnCountBackingField, 0);
            SetField(data, SpawnDataIndexField, marker.ID);
            SetField(data, AccessToolsField(typeof(SpawnedActorData), "IsIndoor"), marker.IsIndoor);
            SetField(data, AccessToolsField(typeof(SpawnedActorData), "MasterID"), marker.masterID);
            SetField(data, AccessToolsField(typeof(SpawnedActorData), "MaxRespawnCount"), marker.MaxRespawnCount);
            SetField(data, AccessToolsField(typeof(SpawnedActorData), "Name"), marker.Name);
            SetField(data, AccessToolsField(typeof(SpawnedActorData), "Pos"), marker.pos);
            SetField(data, AccessToolsField(typeof(SpawnedActorData), "PosVector"), marker.pos.pos);
            SetField(data, AccessToolsField(typeof(SpawnedActorData), "SpawnWaitTime"), marker.spawnWaitTime);
            SetField(data, AccessToolsField(typeof(SpawnedActorData), "StackCount"), marker.stackCount > 0 ? marker.stackCount : 1);
            SetField(data, AccessToolsField(typeof(SpawnedActorData), "Durability"), marker.durability);
            SetField(data, AccessToolsField(typeof(SpawnedActorData), "DefaultGauge"), marker.defaultGauge);
            SetField(data, AccessToolsField(typeof(SpawnedActorData), "IgnoreNav"), marker.ignoreNav);
            SetField(data, AccessToolsField(typeof(SpawnedActorData), "EnableReset"), marker.enableReset);
            SetField(data, AccessToolsField(typeof(SpawnedActorData), "SpawnType"), marker.spawnType);

            return data;
        }

        private static void ExecuteLootingObjectSpawn(DungeonRoom room, SpawnedActorData spawnData)
        {
            if (room is not IVroom vroom)
            {
                throw new InvalidOperationException("DungeonRoom is not IVroom");
            }

            _ = ExecuteLootingObjectSpawnMethod.Invoke(vroom, [spawnData]);
        }

        private static bool TrySpawnFixedLoot(DungeonRoom room, SpawnedActorData spawnData, bool isRespawn)
        {
            if (room is not IVroom vroom)
            {
                return false;
            }

            if (spawnData.ActorID != 0 || spawnData.MasterID <= 0)
            {
                return false;
            }

            if (isRespawn && MapPlacedEncounterProximity.ShouldBlockBonusLootRespawn(room, spawnData, throttle: false))
            {
                return false;
            }

            if (isRespawn)
            {
                PrepareSpawnCountForRespawn(spawnData);
            }

            if (!ItemTypeLookup.TryGetItem(spawnData.MasterID, out _))
            {
                return false;
            }

            int stackCount = spawnData.StackCount > 0 ? spawnData.StackCount : 1;

            if (GetNewItemElementMethod.Invoke(
                vroom,
                [
                    spawnData.MasterID,
                    false,
                    stackCount,
                    spawnData.Durability,
                    spawnData.DefaultGauge,
                    0,
                ]) is not ItemElement element)
            {
                return false;
            }

            int actorId = (int)SpawnLootingObjectMethod.Invoke(
                vroom,
                [
                    element,
                    spawnData.Pos,
                    spawnData.IsIndoor,
                    ReasonOfSpawn.Spawn,
                    spawnData.Index,
                    0,
                    0L,
                    spawnData.IgnoreNav,
                    false,
                ]);

            if (actorId == 0)
            {
                return false;
            }

            spawnData.SetActorID(actorId);
            return true;
        }

        private static void PrepareSpawnCountForRespawn(SpawnedActorData spawnData)
        {
            if (!spawnData.EnableReset || spawnData.MaxRespawnCount <= 0)
            {
                return;
            }

            if (spawnData.CurrentSpawnCount > spawnData.MaxRespawnCount)
            {
                SetField(spawnData, CurrentSpawnCountBackingField, 0);
            }
        }

        private static void SetField(object target, FieldInfo field, object? value)
        {
            field.SetValue(target, value);
        }

        private static FieldInfo AccessToolsField(Type type, string name)
        {
            FieldInfo? field = AccessTools.Field(type, name);
            return field ?? throw new InvalidOperationException($"{type.Name}.{name} not found");
        }

        private static void ScheduleRespawn(DungeonRoom room, SpawnedActorData spawnData, int masterId)
        {
            foreach (PendingRespawn pending in PendingRespawns)
            {
                if (pending.Room == room && ReferenceEquals(pending.Data, spawnData))
                {
                    return;
                }
            }

            (float minDelay, float maxDelay) = ResolveEncounterDelays(room);
            float delay = minDelay >= maxDelay ? minDelay : UnityEngine.Random.Range(minDelay, maxDelay);
            long spawnWaitMs = spawnData.SpawnWaitTime;
            if (spawnWaitMs > 0)
            {
                float spawnWaitSeconds = spawnWaitMs / 1000f;
                if (spawnWaitSeconds > delay)
                {
                    delay = spawnWaitSeconds;
                }
            }

            PendingRespawns.Add(new PendingRespawn(room, spawnData, masterId, Time.time + delay));

            if (ModConfig.EnableDebugLogging.Value)
            {
                ModLog.Debug(Feature, $"Fixed loot respawn scheduled — master={masterId}, marker={spawnData.Index}, delay={delay:0.0}s");
            }
        }

        private static void DeferNextAttempt(int index, PendingRespawn pending, float now)
        {
            PendingRespawns[index] = pending.WithNextAttemptAt(now + EncounterSpawnTiming.RetryIntervalSeconds);
        }

        private static bool TryFindRoomState(SpawnedActorData spawnData, out RoomState state, out DungeonRoom room)
        {
            foreach (KeyValuePair<DungeonRoom, RoomState> entry in RoomStates.EnumerateAll())
            {
                if (entry.Value.TryGetRoomForSpawnData(spawnData, out room))
                {
                    state = entry.Value;
                    return true;
                }
            }

            state = null!;
            room = null!;
            return false;
        }

        private static (float MinDelay, float MaxDelay) ResolveEncounterDelays(DungeonRoom room)
        {
            if (RoomSpawnScalingRegistry.TryGet(room, out RoomSpawnScalingState? spawnState) && spawnState.HasSnapshot)
            {
                return (spawnState.Snapshot.MapPlacedEncounterDelayMinSeconds, spawnState.Snapshot.MapPlacedEncounterDelayMaxSeconds);
            }

            SpawnScalingSceneConfig spawn = SceneScopedConfigGate.Spawn;
            return (spawn.MapPlacedEncounterDelayMinSeconds, spawn.MapPlacedEncounterDelayMaxSeconds);
        }

        private static float ResolveMinPlayerDistanceMeters(DungeonRoom? room)
        {
            if (room != null
                && RoomSpawnScalingRegistry.TryGet(room, out RoomSpawnScalingState? spawnState)
                && spawnState.HasSnapshot)
            {
                return spawnState.Snapshot.MapPlacedEncounterMinPlayerDistanceMeters;
            }

            return SceneScopedConfigGate.Spawn.MapPlacedEncounterMinPlayerDistanceMeters;
        }

        private sealed class RoomState
        {
            private readonly Dictionary<int, int> _remainingQuotaByMasterId = [];

            internal RoomState(DungeonRoom room, LootMultiplicatorSceneConfig snapshot)
            {
                Room = room;
                Snapshot = snapshot;
            }

            internal DungeonRoom Room { get; }

            internal LootMultiplicatorSceneConfig Snapshot { get; }

            internal int SlotCount => _slots.Count;

            internal bool HasQuotas
            {
                get
                {
                    foreach (int quota in _remainingQuotaByMasterId.Values)
                    {
                        if (quota > 0)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            private readonly List<SpawnSlot> _slots = [];
            private readonly Dictionary<SpawnedActorData, DungeonRoom> _spawnDataToRoom = [];

            internal void RegisterSlot(string key, FixedSpawnedActorData data)
            {
                _slots.Add(new SpawnSlot(key, data));
                _spawnDataToRoom[data] = Room;
            }

            internal bool TryGetRoomForSpawnData(SpawnedActorData data, out DungeonRoom room)
            {
                return _spawnDataToRoom.TryGetValue(data, out room);
            }

            internal IEnumerable<KeyValuePair<int, List<SpawnSlot>>> GroupSlotsByMasterId()
            {
                Dictionary<int, List<SpawnSlot>> groups = [];

                foreach (SpawnSlot slot in _slots)
                {
                    if (!groups.TryGetValue(slot.Data.MasterID, out List<SpawnSlot>? list))
                    {
                        list = [];
                        groups[slot.Data.MasterID] = list;
                    }

                    list.Add(slot);
                }

                return groups;
            }

            internal void SetRemainingQuota(int masterId, int quota)
            {
                if (quota <= 0)
                {
                    _ = _remainingQuotaByMasterId.Remove(masterId);
                }
                else
                {
                    _remainingQuotaByMasterId[masterId] = quota;
                }
            }

            internal bool TryConsumeQuota(int masterId)
            {
                if (!_remainingQuotaByMasterId.TryGetValue(masterId, out int quota) || quota <= 0)
                {
                    return false;
                }

                _remainingQuotaByMasterId[masterId] = quota - 1;
                return true;
            }

            internal void RestoreQuota(int masterId)
            {
                if (!_remainingQuotaByMasterId.TryGetValue(masterId, out int quota))
                {
                    return;
                }

                _remainingQuotaByMasterId[masterId] = quota + 1;
            }
        }

        private readonly struct SpawnSlot
        {
            internal SpawnSlot(string key, FixedSpawnedActorData data)
            {
                Key = key;
                Data = data;
            }

            internal string Key { get; }

            internal FixedSpawnedActorData Data { get; }
        }

        private readonly struct PendingRespawn
        {
            internal PendingRespawn(DungeonRoom room, SpawnedActorData data, int masterId, float executeAt)
            {
                Room = room;
                Data = data;
                MasterId = masterId;
                ExecuteAt = executeAt;
                NextAttemptAt = executeAt;
            }

            internal DungeonRoom Room { get; }

            internal SpawnedActorData Data { get; }

            internal int MasterId { get; }

            internal float ExecuteAt { get; }

            internal float NextAttemptAt { get; }

            internal PendingRespawn WithNextAttemptAt(float nextAttemptAt)
            {
                return new PendingRespawn(Room, Data, MasterId, ExecuteAt, nextAttemptAt);
            }

            private PendingRespawn(
                DungeonRoom room,
                SpawnedActorData data,
                int masterId,
                float executeAt,
                float nextAttemptAt)
            {
                Room = room;
                Data = data;
                MasterId = masterId;
                ExecuteAt = executeAt;
                NextAttemptAt = nextAttemptAt;
            }
        }
    }
}
