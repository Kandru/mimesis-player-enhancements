namespace MimesisPlayerEnhancement.Util
{
    /// <summary>
    /// Room-state types that can resolve which <see cref="DungeonRoom"/> a spawn-data
    /// instance belongs to (LootMultiplicator and SpawnScaling pending-spawn schedulers).
    /// </summary>
    internal interface ISpawnDataRoomIndex
    {
        bool TryGetRoomForSpawnData(SpawnedActorData data, out DungeonRoom room);
    }

    internal static class SpawnDataRoomLookup
    {
        /// <summary>Finds the first registered room state owning <paramref name="spawnData"/>.</summary>
        internal static bool TryFindRoomState<TState>(
            IEnumerable<KeyValuePair<DungeonRoom, TState>> entries,
            SpawnedActorData spawnData,
            out TState state,
            out DungeonRoom room)
            where TState : class, ISpawnDataRoomIndex
        {
            foreach (KeyValuePair<DungeonRoom, TState> entry in entries)
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
    }
}
