using System.Collections;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class LootSpawnDataLookup
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo SpawnedActorDatasField =
            typeof(DungeonRoom).GetField("_spawnedActorDatas", InstanceFlags)
            ?? throw new InvalidOperationException("DungeonRoom._spawnedActorDatas not found");

        private static readonly DungeonRoomStateRegistry<Dictionary<int, SpawnedActorData>> IndexByRoom = new();

        internal static void RebuildIndex(DungeonRoom room)
        {
            Dictionary<int, SpawnedActorData> index = [];

            if (SpawnedActorDatasField.GetValue(room) is IDictionary spawnDatas)
            {
                foreach (DictionaryEntry entry in spawnDatas)
                {
                    if (entry.Value is not SpawnedActorData candidate || candidate.Index == 0)
                    {
                        continue;
                    }

                    index[candidate.Index] = candidate;
                }
            }

            IndexByRoom.Register(room, index);
        }

        internal static bool TryFindByMarkerIndex(DungeonRoom room, int markerIndex, out SpawnedActorData? spawnData)
        {
            spawnData = null;
            if (markerIndex == 0)
            {
                return false;
            }

            if (!IndexByRoom.TryGet(room, out Dictionary<int, SpawnedActorData>? index))
            {
                RebuildIndex(room);
                if (!IndexByRoom.TryGet(room, out index))
                {
                    return false;
                }
            }

            if (!index.TryGetValue(markerIndex, out SpawnedActorData? candidate))
            {
                return false;
            }

            spawnData = candidate;
            return true;
        }
    }
}
