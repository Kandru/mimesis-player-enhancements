using System.Collections;
using System.Reflection;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class MapPlacedEncounterScheduler
    {
        private const string Feature = "SpawnScaling";

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

        private static readonly List<PendingEncounterSpawn> PendingEncounters = [];

        /// <summary>Drops queued bonus encounters so a disabled feature cannot spawn them later.</summary>
        internal static void ClearPendingEncounters()
        {
            if (PendingEncounters.Count == 0)
            {
                return;
            }

            ModLog.Debug(Feature, $"Pending bonus encounters cleared — {PendingEncounters.Count} dropped");
            PendingEncounters.Clear();
        }

        internal static void ApplyAfterInit(DungeonRoom room)
        {
            if (!ModConfig.EnableSpawnScaling.Value
                || DungeonRoomAppliedSet.IsApplied(room, DungeonRoomApplyKind.MapPlacedEncounters))
            {
                return;
            }

            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            DungeonRoomAppliedSet.MarkApplied(room, DungeonRoomApplyKind.MapPlacedEncounters);

            if (SpawnScalingFields.SpawnedActorDatasField.GetValue(room) is not IDictionary spawnDatas || spawnDatas.Count == 0)
            {
                return;
            }

            int playerCount = room.GetMemberCount();
            RoomSpawnScalingState state = RoomSpawnScalingRegistry.GetOrCreate(room);

            foreach (DictionaryEntry entry in spawnDatas)
            {
                if (entry.Value is not FixedSpawnedActorData spawnData || !IsMapPlacedCreature(spawnData))
                {
                    continue;
                }

                if (entry.Key is int markerId)
                {
                    state.RegisterSlot(markerId, spawnData);
                }
            }

            foreach (KeyValuePair<int, List<RoomSpawnScalingState.EncounterSlot>> group in state.GroupSlotsByMasterId())
            {
                int masterId = group.Key;
                SpawnCategory category = SpawnCategoryLookup.GetCategory(masterId);
                float multiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(category, playerCount);
                int vanillaCount = group.Value.Count;
                int targetTotal = SpawnMultiplierResolver.ScaleCount(vanillaCount, multiplier);
                int need = targetTotal - vanillaCount;

                if (need <= 0)
                {
                    continue;
                }

                string entityName = MonsterTypeLookup.GetDisplayName(masterId);
                HashSet<int> usedMarkerIds = [];

                foreach (RoomSpawnScalingState.EncounterSlot slot in group.Value)
                {
                    _ = usedMarkerIds.Add(slot.MarkerId);
                }

                List<MapMarker_CreatureSpawnPoint> unusedMarkers = [];
                foreach (MapMarker_CreatureSpawnPoint marker in CreatureSpawnMarkerAccess.GetAllCreatureSpawnMarkers())
                {
                    if (marker.masterID != masterId || usedMarkerIds.Contains(marker.ID))
                    {
                        continue;
                    }

                    unusedMarkers.Add(marker);
                }

                int registered = RegisterUnusedMarkers(room, state, spawnDatas, unusedMarkers, need);
                int remainingCredits = need - registered;
                state.SetRemainingCredits(masterId, remainingCredits);

                ModLog.Info(Feature, $"Map-placed encounter scaling — category={SpawnCategoryLookup.Format(category)}, name={entityName}, master={masterId}, " +
                    $"{multiplier:0.##}× (vanilla={vanillaCount}, target={targetTotal}, markers+={registered}, credits={remainingCredits})");
            }

            if (state.HasTrackedSlots)
            {
                RoomSpawnScalingRegistry.Register(room, state);
            }
        }

        internal static void OnActorDead(SpawnedActorData spawnData)
        {
            if (!ModConfig.EnableSpawnScaling.Value || spawnData == null)
            {
                return;
            }

            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            if (!IsMapPlacedCreature(spawnData))
            {
                return;
            }

            if (!TryFindRoomState(spawnData, out RoomSpawnScalingState? state, out DungeonRoom? room))
            {
                return;
            }

            bool creditConsumed = state.TryConsumeCredit(spawnData.MasterID);
            if (!ShouldScheduleEncounter(spawnData, creditConsumed))
            {
                if (creditConsumed)
                {
                    state.RestoreCredit(spawnData.MasterID);
                }

                return;
            }

            ScheduleEncounter(room, spawnData, spawnData.MasterID, creditConsumed);
        }

        internal static void ProcessPendingEncounters()
        {
            if (PendingEncounters.Count == 0
                || !ModConfig.EnableSpawnScaling.Value
                || !HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            float now = Time.time;

            for (int i = PendingEncounters.Count - 1; i >= 0; i--)
            {
                PendingEncounterSpawn pending = PendingEncounters[i];
                if (now < pending.ExecuteAt || now < pending.NextAttemptAt)
                {
                    continue;
                }

                if (pending.Room == null || pending.Data == null)
                {
                    PendingEncounters.RemoveAt(i);
                    continue;
                }

                if (MapPlacedEncounterProximity.IsPlayerBlockingSpawn(pending.Room, pending.Data.PosVector))
                {
                    if (ModConfig.EnableDebugLogging.Value)
                    {
                        ModLog.Debug(Feature, $"Bonus encounter waiting — master={pending.MasterId}, marker={pending.Data.Index}, " +
                            $"players within {ModConfig.MapPlacedEncounterMinPlayerDistanceMeters.Value:0.#}m");
                    }

                    DeferNextAttempt(i, pending, now);
                    continue;
                }

                if (!RoomSpawnScalingRegistry.TryGet(pending.Room, out _))
                {
                    PendingEncounters.RemoveAt(i);
                    continue;
                }

                try
                {
                    if (TrySpawnEncounter(pending.Room, pending.Data))
                    {
                        PendingEncounters.RemoveAt(i);
                        continue;
                    }

                    if (pending.Data.ActorID != 0)
                    {
                        PendingEncounters.RemoveAt(i);
                        continue;
                    }

                    DeferNextAttempt(i, pending, now);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Bonus encounter spawn failed — master={pending.MasterId}: {ex.Message}");
                    DeferNextAttempt(i, pending, now);
                }
            }
        }

        private static int RegisterUnusedMarkers(
            DungeonRoom room,
            RoomSpawnScalingState state,
            IDictionary spawnDatas,
            List<MapMarker_CreatureSpawnPoint> unusedMarkers,
            int need)
        {
            int registered = 0;

            for (int i = 0; i < unusedMarkers.Count && registered < need; i++)
            {
                MapMarker_CreatureSpawnPoint marker = unusedMarkers[i];
                if (spawnDatas.Contains(marker.ID))
                {
                    continue;
                }

                FixedSpawnedActorData spawnData = new(marker);
                spawnDatas.Add(marker.ID, spawnData);
                state.RegisterSlot(marker.ID, spawnData);
                registered++;

                if (ModConfig.EnableDebugLogging.Value)
                {
                    ModLog.Debug(Feature, $"Registered unused map marker — master={marker.masterID}, marker={marker.ID}, " +
                        $"pos={SpawnScalingLog.FormatLocation(room, marker.pos.pos)}");
                }
            }

            return registered;
        }

        private static bool IsMapPlacedCreature(SpawnedActorData spawnData)
        {
            return spawnData is FixedSpawnedActorData
                && (spawnData.MarkerType.Equals(MapMarkerType.Creature)
                    || spawnData.MarkerType.Equals(MapMarkerType.SpecialMonster));
        }

        private static bool ShouldScheduleEncounter(SpawnedActorData spawnData, bool creditConsumed)
        {
            return creditConsumed
                || SpawnCategoryLookup.GetCategory(spawnData.MasterID) == SpawnCategory.Trap
                || HasRespawnBudget(spawnData);
        }

        private static bool HasRespawnBudget(SpawnedActorData spawnData)
        {
            return !spawnData.SpawnType.Equals(SpawnType.OnStartMap)
                && (spawnData.MaxRespawnCount == 0
                    || spawnData.CurrentSpawnCount < spawnData.MaxRespawnCount
                    || spawnData.EnableReset);
        }

        private static bool TrySpawnEncounter(DungeonRoom room, SpawnedActorData spawnData)
        {
            if (room is not IVroom vroom || spawnData.ActorID != 0)
            {
                return false;
            }

            PrepareSpawnCountForRespawn(spawnData);

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

        private static void PrepareSpawnCountForRespawn(SpawnedActorData spawnData)
        {
            if (!spawnData.EnableReset || spawnData.MaxRespawnCount <= 0)
            {
                return;
            }

            if (spawnData.CurrentSpawnCount > spawnData.MaxRespawnCount)
            {
                SpawnScalingFields.CurrentSpawnCountBackingField.SetValue(spawnData, 0);
            }
        }

        private static void ScheduleEncounter(
            DungeonRoom room,
            SpawnedActorData spawnData,
            int masterId,
            bool creditConsumed)
        {
            foreach (PendingEncounterSpawn pending in PendingEncounters)
            {
                if (pending.Room == room && ReferenceEquals(pending.Data, spawnData))
                {
                    return;
                }
            }

            float minDelay = ModConfig.MapPlacedEncounterDelayMinSeconds.Value;
            float maxDelay = ModConfig.MapPlacedEncounterDelayMaxSeconds.Value;
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

            PendingEncounters.Add(new PendingEncounterSpawn(room, spawnData, masterId, Time.time + delay, creditConsumed));

            if (ModConfig.EnableDebugLogging.Value)
            {
                ModLog.Debug(Feature, $"Bonus encounter scheduled — master={masterId}, marker={spawnData.Index}, " +
                    $"pos={SpawnScalingLog.FormatLocation(room, spawnData.PosVector)}, delay={delay:0.0}s");
            }
        }

        private static void DeferNextAttempt(int index, PendingEncounterSpawn pending, float now)
        {
            PendingEncounters[index] = pending.WithNextAttemptAt(now + EncounterSpawnTiming.RetryIntervalSeconds);
        }

        private static bool TryFindRoomState(
            SpawnedActorData spawnData,
            out RoomSpawnScalingState state,
            out DungeonRoom room)
        {
            foreach (KeyValuePair<DungeonRoom, RoomSpawnScalingState> entry in RoomSpawnScalingRegistry.EnumerateAll())
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

        private readonly struct PendingEncounterSpawn
        {
            internal PendingEncounterSpawn(
                DungeonRoom room,
                SpawnedActorData data,
                int masterId,
                float executeAt,
                bool creditConsumed)
            {
                Room = room;
                Data = data;
                MasterId = masterId;
                ExecuteAt = executeAt;
                NextAttemptAt = executeAt;
                CreditConsumed = creditConsumed;
            }

            internal DungeonRoom Room { get; }

            internal SpawnedActorData Data { get; }

            internal int MasterId { get; }

            internal float ExecuteAt { get; }

            internal float NextAttemptAt { get; }

            internal bool CreditConsumed { get; }

            internal PendingEncounterSpawn WithNextAttemptAt(float nextAttemptAt)
            {
                return new PendingEncounterSpawn(Room, Data, MasterId, ExecuteAt, CreditConsumed, nextAttemptAt);
            }

            private PendingEncounterSpawn(
                DungeonRoom room,
                SpawnedActorData data,
                int masterId,
                float executeAt,
                bool creditConsumed,
                float nextAttemptAt)
            {
                Room = room;
                Data = data;
                MasterId = masterId;
                ExecuteAt = executeAt;
                NextAttemptAt = nextAttemptAt;
                CreditConsumed = creditConsumed;
            }
        }
    }
}
