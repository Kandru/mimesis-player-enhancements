namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal sealed class RoomSpawnScalingState
    {
        private readonly Dictionary<int, int> _remainingCreditsByMasterId = [];
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

        internal int NextJakoWavePeriodMs { get; set; }

        internal int NextMimicWavePeriodMs { get; set; }

        internal void RegisterSlot(int markerId, FixedSpawnedActorData data)
        {
            _slots.Add(new EncounterSlot(markerId, data));
            _spawnDataToRoom[data] = Room;
        }

        internal bool TryGetRoomForSpawnData(SpawnedActorData data, out DungeonRoom room)
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

        internal void SetRemainingCredits(int masterId, int credits)
        {
            if (credits <= 0)
            {
                _ = _remainingCreditsByMasterId.Remove(masterId);
            }
            else
            {
                _remainingCreditsByMasterId[masterId] = credits;
            }
        }

        internal bool TryConsumeCredit(int masterId)
        {
            if (!_remainingCreditsByMasterId.TryGetValue(masterId, out int credits) || credits <= 0)
            {
                return false;
            }

            _remainingCreditsByMasterId[masterId] = credits - 1;
            return true;
        }

        internal void RestoreCredit(int masterId)
        {
            if (!_remainingCreditsByMasterId.TryGetValue(masterId, out int credits))
            {
                return;
            }

            _remainingCreditsByMasterId[masterId] = credits + 1;
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
