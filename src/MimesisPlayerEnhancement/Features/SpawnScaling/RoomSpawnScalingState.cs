namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal sealed class RoomSpawnScalingState : ISpawnDataRoomIndex
    {
        private readonly Dictionary<int, int> _bonusGroupWavesByGroupId = [];
        private readonly Dictionary<SpawnedActorData, DungeonRoom> _spawnDataToRoom = [];
        private readonly List<EncounterSlot> _slots = [];

        internal RoomSpawnScalingState(DungeonRoom room)
        {
            Room = room;
        }

        internal DungeonRoom Room { get; }

        internal SpawnScalingSceneConfig Snapshot { get; private set; }

        internal bool HasSnapshot { get; private set; }

        internal void SetSnapshot(SpawnScalingSceneConfig snapshot)
        {
            Snapshot = snapshot;
            HasSnapshot = true;
        }

        internal SpawnTimingOverrides? TimingOverrides { get; set; }

        internal int NextGruntWavePeriodMs { get; set; }

        internal int NextMimicWavePeriodMs { get; set; }

        internal int NextSyntheticIndex { get; set; } = SpawnSlotFactory.SyntheticIndexBase;

        internal int SyntheticSlotCount { get; private set; }

        internal void ClearForReinit()
        {
            _slots.Clear();
            _spawnDataToRoom.Clear();
            NextSyntheticIndex = SpawnSlotFactory.SyntheticIndexBase;
            SyntheticSlotCount = 0;
        }

        internal void TrackSyntheticSlot()
        {
            SyntheticSlotCount++;
        }

        internal void TrackSyntheticSlotRollback()
        {
            if (SyntheticSlotCount > 0)
            {
                SyntheticSlotCount--;
            }
        }

        internal bool CanAddSyntheticSlot()
        {
            return SyntheticSlotCount < SpawnSlotFactory.MaxSyntheticSlotsPerRoom;
        }

        internal void RegisterSlot(int markerId, FixedSpawnedActorData data)
        {
            _slots.Add(new EncounterSlot(markerId, data));
            _spawnDataToRoom[data] = Room;
        }

        internal void UnregisterSlot(int markerId, FixedSpawnedActorData data)
        {
            for (int i = _slots.Count - 1; i >= 0; i--)
            {
                if (_slots[i].MarkerId == markerId && ReferenceEquals(_slots[i].Data, data))
                {
                    _slots.RemoveAt(i);
                    break;
                }
            }

            _ = _spawnDataToRoom.Remove(data);
        }

        public bool TryGetRoomForSpawnData(SpawnedActorData data, out DungeonRoom room)
        {
            return _spawnDataToRoom.TryGetValue(data, out room!);
        }

        internal IEnumerable<KeyValuePair<int, List<EncounterSlot>>> GroupSlotsByMasterId()
        {
            Dictionary<int, List<EncounterSlot>> groups = [];

            foreach (EncounterSlot slot in _slots)
            {
                if (!groups.TryGetValue(slot.Data.MasterID, out List<EncounterSlot>? list))
                {
                    list = [];
                    groups[slot.Data.MasterID] = list;
                }

                list.Add(slot);
            }

            return groups;
        }

        internal void SetBonusGroupWaves(int groupId, int waves)
        {
            if (waves <= 0)
            {
                _ = _bonusGroupWavesByGroupId.Remove(groupId);
            }
            else
            {
                _bonusGroupWavesByGroupId[groupId] = waves;
            }
        }

        internal bool TryConsumeBonusGroupWave(int groupId)
        {
            if (!_bonusGroupWavesByGroupId.TryGetValue(groupId, out int waves) || waves <= 0)
            {
                return false;
            }

            _bonusGroupWavesByGroupId[groupId] = waves - 1;
            return true;
        }

        /// <summary>True when this room configured bonus waves for the group (including exhausted waves).</summary>
        internal bool TracksBonusGroup(int groupId)
        {
            return _bonusGroupWavesByGroupId.ContainsKey(groupId);
        }

        internal readonly struct EncounterSlot
        {
            internal EncounterSlot(int markerId, FixedSpawnedActorData data)
            {
                MarkerId = markerId;
                Data = data;
            }

            internal int MarkerId { get; }

            internal FixedSpawnedActorData Data { get; }
        }
    }

    internal static class RoomSpawnScalingRegistry
    {
        private static readonly DungeonRoomStateRegistry<RoomSpawnScalingState> States = new();

        internal static RoomSpawnScalingState GetOrCreate(DungeonRoom room)
        {
            return States.GetOrCreate(room, () => new RoomSpawnScalingState(room));
        }

        internal static bool TryGet(DungeonRoom room, out RoomSpawnScalingState state)
        {
            return States.TryGet(room, out state);
        }

        internal static IEnumerable<KeyValuePair<DungeonRoom, RoomSpawnScalingState>> EnumerateAll()
        {
            return States.EnumerateAll();
        }
    }
}
