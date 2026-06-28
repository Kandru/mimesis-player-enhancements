using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.SpawnScaling;

internal static class FixedSpawnCoordinator
{
    private const string Feature = "SpawnScaling";

    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly FieldInfo SpawnedActorDatasField =
        typeof(DungeonRoom).GetField("_spawnedActorDatas", InstanceFlags)
        ?? throw new InvalidOperationException("DungeonRoom._spawnedActorDatas not found");

    private static readonly MethodInfo ExecuteCreatureSpawnMethod =
        AccessTools.Method(typeof(IVroom), "ExecuteCreatureSpawn", new[] { typeof(SpawnedActorData) })
        ?? throw new InvalidOperationException("IVroom.ExecuteCreatureSpawn not found");

    private static readonly MethodInfo MemberwiseCloneMethod =
        AccessTools.Method(typeof(object), "MemberwiseClone")
        ?? throw new InvalidOperationException("object.MemberwiseClone not found");

    private static readonly FieldInfo CurrentSpawnCountBackingField =
        AccessToolsField(typeof(SpawnedActorData), "<CurrentSpawnCount>k__BackingField");

    private static readonly FieldInfo SpawnDataIndexField =
        AccessToolsField(typeof(SpawnedActorData), "Index");

    private static readonly HashSet<DungeonRoom> AppliedRooms = new();
    private static readonly Dictionary<DungeonRoom, RoomState> RoomStates = new();
    private static readonly List<PendingRespawn> PendingRespawns = new();

    internal static void ApplyAfterInit(DungeonRoom room)
    {
        if (!ModConfig.EnableSpawnScaling.Value || AppliedRooms.Contains(room))
            return;

        if (SpawnScalingHost.IsParticipantClient() || !SpawnScalingHost.ShouldApplyScaling())
            return;

        AppliedRooms.Add(room);

        if (SpawnedActorDatasField.GetValue(room) is not IDictionary spawnDatas || spawnDatas.Count == 0)
            return;

        int playerCount = room.GetMemberCount();
        MapMarker_CreatureSpawnPoint[] markers = CollectCreatureSpawnMarkers();
        var state = new RoomState(room);

        foreach (DictionaryEntry entry in spawnDatas)
        {
            if (entry.Value is not FixedSpawnedActorData spawnData)
                continue;

            state.RegisterSlot(entry.Key as string ?? spawnData.Name, spawnData);
        }

        foreach (KeyValuePair<int, List<SpawnSlot>> group in state.GroupSlotsByMasterId())
        {
            int masterId = group.Key;
            SpawnCategory category = SpawnCategoryLookup.GetCategory(masterId);
            float multiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(category, playerCount);
            int vanillaCount = group.Value.Count;
            int targetTotal = SpawnMultiplierResolver.ScaleCount(vanillaCount, multiplier);
            int need = targetTotal - vanillaCount;

            if (need <= 0)
                continue;

            string entityName = MonsterTypeLookup.GetDisplayName(masterId);
            var usedMarkerIds = new HashSet<int>();
            FixedSpawnedActorData? template = null;

            foreach (SpawnSlot slot in group.Value)
            {
                usedMarkerIds.Add(slot.Data.Index);
                template ??= slot.Data;
            }

            if (template == null)
                continue;

            var unusedMarkers = new List<MapMarker_CreatureSpawnPoint>();
            foreach (MapMarker_CreatureSpawnPoint marker in markers)
            {
                if (marker.masterID != masterId || usedMarkerIds.Contains(marker.ID))
                    continue;

                unusedMarkers.Add(marker);
            }

            int activated = ActivateUnusedMarkers(room, state, spawnDatas, template, unusedMarkers, need);
            int remainingQuota = need - activated;
            state.SetRemainingQuota(masterId, remainingQuota);

            ModLog.Info(
                Feature,
                $"Fixed spawn scaling — category={SpawnCategoryLookup.Format(category)}, name={entityName}, master={masterId}, " +
                $"{multiplier:0.##}× (vanilla={vanillaCount}, target={targetTotal}, markers+={activated}, respawnQuota={remainingQuota})");
        }

        if (state.HasQuotas || state.SlotCount > 0)
            RoomStates[room] = state;
    }

    internal static void OnActorDead(SpawnedActorData spawnData)
    {
        if (!ModConfig.EnableSpawnScaling.Value || spawnData == null)
            return;

        if (SpawnScalingHost.IsParticipantClient() || !SpawnScalingHost.ShouldApplyScaling())
            return;

        if (!TryFindRoomState(spawnData, out RoomState? state, out DungeonRoom? room))
            return;

        if (!state.TryConsumeQuota(spawnData.MasterID))
            return;

        ScheduleRespawn(room, spawnData, spawnData.MasterID);
    }

    internal static void ProcessPendingRespawns()
    {
        if (PendingRespawns.Count == 0)
            return;

        float now = Time.time;

        for (int i = PendingRespawns.Count - 1; i >= 0; i--)
        {
            PendingRespawn pending = PendingRespawns[i];
            if (now < pending.ExecuteAt)
                continue;

            PendingRespawns.RemoveAt(i);

            if (pending.Room == null)
                continue;

            if (!RoomStates.TryGetValue(pending.Room, out RoomState? state))
                continue;

            if (pending.Data == null || !pending.Data.CanSpawn())
            {
                state.RestoreQuota(pending.MasterId);
                continue;
            }

            try
            {
                ExecuteCreatureSpawn(pending.Room, pending.Data);
            }
            catch (Exception ex)
            {
                state.RestoreQuota(pending.MasterId);
                ModLog.Warn(Feature, $"Fixed spawn respawn failed — master={pending.MasterId}: {ex.Message}");
            }
        }
    }

    private static int ActivateUnusedMarkers(
        DungeonRoom room,
        RoomState state,
        IDictionary spawnDatas,
        FixedSpawnedActorData template,
        List<MapMarker_CreatureSpawnPoint> unusedMarkers,
        int need)
    {
        int activated = 0;

        for (int i = 0; i < unusedMarkers.Count && activated < need; i++)
        {
            MapMarker_CreatureSpawnPoint marker = unusedMarkers[i];
            string key = marker.Name;

            if (string.IsNullOrWhiteSpace(key) || spawnDatas.Contains(key))
                key = $"{marker.masterID}_{marker.ID}";

            if (spawnDatas.Contains(key))
                continue;

            var spawnData = CreateSpawnDataFromMarker(template, marker);
            spawnDatas.Add(key, spawnData);
            state.RegisterSlot(key, spawnData);

            try
            {
                ExecuteCreatureSpawn(room, spawnData);
                activated++;
            }
            catch (Exception ex)
            {
                spawnDatas.Remove(key);
                ModLog.Warn(Feature, $"Fixed spawn marker activation failed — master={marker.masterID}, marker={marker.ID}: {ex.Message}");
            }
        }

        return activated;
    }

    private static FixedSpawnedActorData CreateSpawnDataFromMarker(
        FixedSpawnedActorData template,
        MapMarker_CreatureSpawnPoint marker)
    {
        var data = (FixedSpawnedActorData)MemberwiseCloneMethod.Invoke(template, null)!;
        data.SetActorID(0);
        SetField(data, CurrentSpawnCountBackingField, 0);
        SetField(data, SpawnDataIndexField, marker.ID);
        SetField(data, AccessToolsField(typeof(SpawnedActorData), "IsIndoor"), marker.IsIndoor);
        SetField(data, AccessToolsField(typeof(SpawnedActorData), "MasterID"), marker.masterID);
        SetField(data, AccessToolsField(typeof(SpawnedActorData), "MaxRespawnCount"), marker.MaxRespawnCount);
        SetField(data, AccessToolsField(typeof(SpawnedActorData), "Name"), marker.Name);
        SetField(data, AccessToolsField(typeof(SpawnedActorData), "Pos"), marker.pos);
        SetField(data, AccessToolsField(typeof(SpawnedActorData), "PosVector"), marker.pos.pos);
        SetField(data, AccessToolsField(typeof(SpawnedActorData), "AIName"), marker.aiName);
        SetField(data, AccessToolsField(typeof(SpawnedActorData), "BTName"), marker.btName);
        SetField(data, AccessToolsField(typeof(SpawnedActorData), "SpawnWaitTime"), marker.spawnWaitTime);
        SetField(data, AccessToolsField(typeof(SpawnedActorData), "StackCount"), 1);

        return data;
    }

    private static void ExecuteCreatureSpawn(DungeonRoom room, SpawnedActorData spawnData)
    {
        if (room is not IVroom vroom)
            throw new InvalidOperationException("DungeonRoom is not IVroom");

        ExecuteCreatureSpawnMethod.Invoke(vroom, new object[] { spawnData });
    }

    private static void SetField(object target, FieldInfo field, object? value) =>
        field.SetValue(target, value);

    private static FieldInfo AccessToolsField(Type type, string name)
    {
        FieldInfo? field = AccessTools.Field(type, name);
        if (field == null)
            throw new InvalidOperationException($"{type.Name}.{name} not found");

        return field;
    }

    private static void ScheduleRespawn(DungeonRoom room, SpawnedActorData spawnData, int masterId)
    {
        foreach (PendingRespawn pending in PendingRespawns)
        {
            if (pending.Room == room && ReferenceEquals(pending.Data, spawnData))
                return;
        }

        float minDelay = ModConfig.FixedSpawnRespawnDelayMinSeconds.Value;
        float maxDelay = ModConfig.FixedSpawnRespawnDelayMaxSeconds.Value;
        float delay = minDelay >= maxDelay ? minDelay : UnityEngine.Random.Range(minDelay, maxDelay);

        PendingRespawns.Add(new PendingRespawn(room, spawnData, masterId, Time.time + delay));

        if (ModConfig.EnableDebugLogging.Value)
        {
            Vector3 pos = spawnData.PosVector;
            ModLog.Debug(
                Feature,
                $"Fixed spawn respawn scheduled — master={masterId}, marker={spawnData.Index}, " +
                $"pos={SpawnScalingLog.FormatLocation(room, pos)}, delay={delay:0.0}s");
        }
    }

    private static bool TryFindRoomState(SpawnedActorData spawnData, out RoomState state, out DungeonRoom room)
    {
        foreach (KeyValuePair<DungeonRoom, RoomState> entry in RoomStates)
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

    private static MapMarker_CreatureSpawnPoint[] CollectCreatureSpawnMarkers() =>
        UnityEngine.Object.FindObjectsByType<MapMarker_CreatureSpawnPoint>(FindObjectsSortMode.None);

    private sealed class RoomState
    {
        private readonly Dictionary<int, int> _remainingQuotaByMasterId = new();

        internal RoomState(DungeonRoom room) => Room = room;

        internal DungeonRoom Room { get; }

        internal int SlotCount => _slots.Count;

        internal bool HasQuotas
        {
            get
            {
                foreach (int quota in _remainingQuotaByMasterId.Values)
                {
                    if (quota > 0)
                        return true;
                }

                return false;
            }
        }

        private readonly List<SpawnSlot> _slots = new();
        private readonly Dictionary<SpawnedActorData, DungeonRoom> _spawnDataToRoom = new();

        internal void RegisterSlot(string key, FixedSpawnedActorData data)
        {
            _slots.Add(new SpawnSlot(key, data));
            _spawnDataToRoom[data] = Room;
        }

        internal bool TryGetRoomForSpawnData(SpawnedActorData data, out DungeonRoom room) =>
            _spawnDataToRoom.TryGetValue(data, out room!);

        internal IEnumerable<KeyValuePair<int, List<SpawnSlot>>> GroupSlotsByMasterId()
        {
            var groups = new Dictionary<int, List<SpawnSlot>>();

            foreach (SpawnSlot slot in _slots)
            {
                if (!groups.TryGetValue(slot.Data.MasterID, out List<SpawnSlot>? list))
                {
                    list = new List<SpawnSlot>();
                    groups[slot.Data.MasterID] = list;
                }

                list.Add(slot);
            }

            return groups;
        }

        internal void SetRemainingQuota(int masterId, int quota)
        {
            if (quota <= 0)
                _remainingQuotaByMasterId.Remove(masterId);
            else
                _remainingQuotaByMasterId[masterId] = quota;
        }

        internal bool TryConsumeQuota(int masterId)
        {
            if (!_remainingQuotaByMasterId.TryGetValue(masterId, out int quota) || quota <= 0)
                return false;

            _remainingQuotaByMasterId[masterId] = quota - 1;
            return true;
        }

        internal void RestoreQuota(int masterId)
        {
            if (!_remainingQuotaByMasterId.TryGetValue(masterId, out int quota))
                return;

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
        }

        internal DungeonRoom Room { get; }

        internal SpawnedActorData Data { get; }

        internal int MasterId { get; }

        internal float ExecuteAt { get; }
    }
}
