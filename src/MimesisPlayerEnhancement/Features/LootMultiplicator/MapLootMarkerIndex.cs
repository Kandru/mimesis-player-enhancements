using System.Collections;
using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class MapLootMarkerIndex
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo SpawnedActorDatasField =
            typeof(DungeonRoom).GetField("_spawnedActorDatas", InstanceFlags)
            ?? throw new System.InvalidOperationException("DungeonRoom._spawnedActorDatas not found");

        internal static void ShuffleMarkers<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        internal static List<MapMarker_LootingObjectSpawnPoint> CollectUnusedMarkers(
            int masterId,
            HashSet<int> usedMarkerIds)
        {
            List<MapMarker_LootingObjectSpawnPoint> unused = [];

            foreach (MapMarker_LootingObjectSpawnPoint marker in CollectLootMarkers())
            {
                if (marker.masterID != masterId || usedMarkerIds.Contains(marker.ID))
                {
                    continue;
                }

                unused.Add(marker);
            }

            return unused;
        }

        internal static HashSet<int> CollectUsedMarkerIds(DungeonRoom room)
        {
            HashSet<int> used = [];

            if (SpawnedActorDatasField.GetValue(room) is not IDictionary spawnDatas)
            {
                return used;
            }

            foreach (DictionaryEntry entry in spawnDatas)
            {
                if (entry.Value is not SpawnedActorData spawnData || spawnData.Index == 0)
                {
                    continue;
                }

                _ = used.Add(spawnData.Index);
            }

            return used;
        }

        internal static MapMarker_LootingObjectSpawnPoint[] CollectLootMarkers()
        {
            return Object.FindObjectsByType<MapMarker_LootingObjectSpawnPoint>(FindObjectsSortMode.None);
        }
    }
}
