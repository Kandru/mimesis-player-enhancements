using System.Collections;
using System.Reflection;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class MapPlacedEncounterScheduler
    {
        private const string Feature = "SpawnScaling";

        // game@0.3.1 Assembly-CSharp/IVroom.cs:L3920-3930
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
            if (!SceneScopedConfigGate.Spawn.EnableSpawnScaling
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
            SpawnScalingSceneConfig config = SceneScopedConfigGate.Spawn;

            bool needsDensityScaling = NeedsMapPlacedEncounterScaling(spawnDatas, playerCount, config);
            bool needsTrapSlotRegistration = TrapRespawnDelayResolver.IsForceRespawnActive(config)
                && HasMapPlacedTrapMarkers(spawnDatas);

            if (!needsDensityScaling && !needsTrapSlotRegistration)
            {
                return;
            }

            RoomSpawnScalingState state = RoomSpawnScalingRegistry.GetOrCreate(room);
            state.SetSnapshot(config);

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

            if (!needsDensityScaling)
            {
                return;
            }

            MapMarker_CreatureSpawnPoint[] allMarkers = CreatureSpawnMarkerAccess.CollectSceneMarkers();

            foreach (KeyValuePair<int, List<RoomSpawnScalingState.EncounterSlot>> group in state.GroupSlotsByMasterId())
            {
                int masterId = group.Key;
                SpawnCategory category = SpawnCategoryLookup.GetCategory(masterId);
                float multiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(category, playerCount, config);
                int vanillaCount = group.Value.Count;
                int targetTotal = ScalingMath.ScaleCount(vanillaCount, multiplier);
                int need = targetTotal - vanillaCount;

                if (need <= 0)
                {
                    continue;
                }

                string entityName = MonsterTypeLookup.GetDisplayName(masterId);
                SpawnSlotFactory.MapPlacedScaleResult result = SpawnSlotFactory.ScaleMapPlacedGroup(
                    room,
                    state,
                    spawnDatas,
                    masterId,
                    category,
                    group.Value,
                    need,
                    allMarkers);

                ModLog.Info(Feature, $"Map-placed encounter scaling — category={SpawnCategoryLookup.Format(category)}, name={entityName}, master={masterId}, " +
                    $"{multiplier:0.##}× (vanilla={vanillaCount}, target={targetTotal}, recovered={result.Recovered}, synthetic={result.Synthesized}, shortfall={result.Shortfall})");
            }
        }

        private static bool NeedsMapPlacedEncounterScaling(
            IDictionary spawnDatas,
            int playerCount,
            SpawnScalingSceneConfig config)
        {
            Dictionary<int, int> countsByMasterId = [];

            foreach (DictionaryEntry entry in spawnDatas)
            {
                if (entry.Value is not FixedSpawnedActorData spawnData || !IsMapPlacedCreature(spawnData))
                {
                    continue;
                }

                int masterId = spawnData.MasterID;
                countsByMasterId[masterId] = countsByMasterId.GetValueOrDefault(masterId) + 1;
            }

            foreach (KeyValuePair<int, int> entry in countsByMasterId)
            {
                SpawnCategory category = SpawnCategoryLookup.GetCategory(entry.Key);
                float multiplier = SpawnMultiplierResolver.GetEffectiveMultiplier(category, playerCount, config);
                if (ScalingMath.ScaleCount(entry.Value, multiplier) > entry.Value)
                {
                    return true;
                }
            }

            return false;
        }

        internal static void OnActorDead(SpawnedActorData spawnData)
        {
            if (!SceneScopedConfigGate.Spawn.EnableSpawnScaling || spawnData == null)
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

            if (!TryFindRoomState(spawnData, out _, out DungeonRoom? room))
            {
                return;
            }

            SpawnCategory category = SpawnCategoryLookup.GetCategory(spawnData.MasterID);
            bool hasRespawnBudget = MapPlacedEncounterScheduleResolver.HasRespawnBudget(
                spawnData.SpawnType,
                spawnData.MaxRespawnCount,
                spawnData.CurrentSpawnCount,
                spawnData.EnableReset);

            if (!MapPlacedEncounterScheduleResolver.ShouldScheduleEncounter(
                    ResolveRoomConfig(room),
                    category,
                    hasRespawnBudget))
            {
                return;
            }

            ScheduleEncounter(
                room,
                spawnData,
                spawnData.MasterID,
                useTrapRespawnRules: category == SpawnCategory.Trap
                    && TrapRespawnDelayResolver.IsForceRespawnActive(ResolveRoomConfig(room)));
        }

        internal static void ProcessPendingEncounters()
        {
            if (PendingEncounters.Count == 0
                || !SceneScopedConfigGate.Spawn.EnableSpawnScaling
                || !HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            // Bonus encounters are only valid while a dungeon run is active; drop leftovers
            // so they cannot spawn into a stale DungeonRoom from the tram or maintenance.
            if (SceneScopedConfigGate.ActiveKind != SceneScopeKind.Dungeon)
            {
                ClearPendingEncounters();
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

                SpawnScalingSceneConfig config = ResolveRoomConfig(pending.Room);
                if (pending.UseTrapRespawnRules
                    ? MapPlacedEncounterProximity.ShouldBlockTrapRespawn(pending.Room, pending.Data, throttle: false)
                    : config.BonusEncounterMinPlayerDistanceMeters > 0f
                        && MapPlacedEncounterProximity.IsPlayerBlockingSpawn(
                            pending.Room,
                            pending.Data.PosVector,
                            config.BonusEncounterMinPlayerDistanceMeters))
                {
                    if (ModConfig.EnableDebugLogging.Value)
                    {
                        float minDistance = pending.UseTrapRespawnRules
                            ? TrapRespawnDelayResolver.ResolveMinPlayerDistanceMeters(config)
                            : config.BonusEncounterMinPlayerDistanceMeters;
                        SpawnCategory category = SpawnCategoryLookup.GetCategory(pending.MasterId);
                        ModLog.Debug(Feature, $"Pending encounter waiting — category={SpawnCategoryLookup.Format(category)}, master={pending.MasterId}, marker={pending.Data.Index}, " +
                            $"players within {minDistance:0.#}m");
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

        private static bool HasMapPlacedTrapMarkers(IDictionary spawnDatas)
        {
            foreach (DictionaryEntry entry in spawnDatas)
            {
                if (entry.Value is not FixedSpawnedActorData spawnData || !IsMapPlacedCreature(spawnData))
                {
                    continue;
                }

                if (SpawnCategoryLookup.GetCategory(spawnData.MasterID) == SpawnCategory.Trap)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsMapPlacedCreature(SpawnedActorData spawnData)
        {
            return spawnData is FixedSpawnedActorData
                && (spawnData.MarkerType.Equals(MapMarkerType.Creature)
                    || spawnData.MarkerType.Equals(MapMarkerType.SpecialMonster));
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
            bool useTrapRespawnRules)
        {
            foreach (PendingEncounterSpawn pending in PendingEncounters)
            {
                if (pending.Room == room && ReferenceEquals(pending.Data, spawnData))
                {
                    return;
                }
            }

            SpawnScalingSceneConfig config = ResolveRoomConfig(room);
            float delay = ResolveEncounterDelay(config, spawnData, masterId, useTrapRespawnRules);

            PendingEncounters.Add(new PendingEncounterSpawn(room, spawnData, masterId, Time.time + delay, useTrapRespawnRules));

            if (ModConfig.EnableDebugLogging.Value)
            {
                ModLog.Debug(Feature, $"Bonus encounter scheduled — master={masterId}, marker={spawnData.Index}, " +
                    $"pos={SpawnScalingLog.FormatLocation(room, spawnData.PosVector)}, delay={delay:0.0}s");
            }
        }

        private static float ResolveEncounterDelay(
            SpawnScalingSceneConfig config,
            SpawnedActorData spawnData,
            int masterId,
            bool useTrapRespawnRules)
        {
            SpawnCategory category = SpawnCategoryLookup.GetCategory(masterId);
            float delay;

            if (useTrapRespawnRules && category == SpawnCategory.Trap)
            {
                delay = TrapRespawnDelayResolver.ResolveDelaySeconds(config);
            }
            else
            {
                float minDelay = config.BonusEncounterDelayMinSeconds;
                float maxDelay = config.BonusEncounterDelayMaxSeconds;
                delay = minDelay >= maxDelay ? minDelay : UnityEngine.Random.Range(minDelay, maxDelay);
            }

            if (spawnData.SpawnWaitTime > 0)
            {
                delay = Math.Max(delay, spawnData.SpawnWaitTime / 1000f);
            }

            return delay;
        }

        private static void DeferNextAttempt(int index, PendingEncounterSpawn pending, float now)
        {
            PendingEncounters[index] = pending.WithNextAttemptAt(now + EncounterSpawnTiming.RetryIntervalSeconds);
        }

        /// <summary>Room snapshot when one exists, otherwise the live scene config.</summary>
        private static SpawnScalingSceneConfig ResolveRoomConfig(DungeonRoom? room)
        {
            if (room != null
                && RoomSpawnScalingRegistry.TryGet(room, out RoomSpawnScalingState? state)
                && state.HasSnapshot)
            {
                return state.Snapshot;
            }

            return SceneScopedConfigGate.Spawn;
        }

        private static bool TryFindRoomState(
            SpawnedActorData spawnData,
            out RoomSpawnScalingState state,
            out DungeonRoom room)
        {
            return SpawnDataRoomLookup.TryFindRoomState(
                RoomSpawnScalingRegistry.EnumerateAll(),
                spawnData,
                out state,
                out room);
        }

        private readonly struct PendingEncounterSpawn
        {
            internal PendingEncounterSpawn(
                DungeonRoom room,
                SpawnedActorData data,
                int masterId,
                float executeAt,
                bool useTrapRespawnRules,
                float? nextAttemptAt = null)
            {
                Room = room;
                Data = data;
                MasterId = masterId;
                ExecuteAt = executeAt;
                UseTrapRespawnRules = useTrapRespawnRules;
                NextAttemptAt = nextAttemptAt ?? executeAt;
            }

            internal DungeonRoom Room { get; }

            internal SpawnedActorData Data { get; }

            internal int MasterId { get; }

            internal bool UseTrapRespawnRules { get; }

            internal float ExecuteAt { get; }

            internal float NextAttemptAt { get; }

            internal PendingEncounterSpawn WithNextAttemptAt(float nextAttemptAt)
            {
                return new PendingEncounterSpawn(Room, Data, MasterId, ExecuteAt, UseTrapRespawnRules, nextAttemptAt);
            }
        }
    }
}
