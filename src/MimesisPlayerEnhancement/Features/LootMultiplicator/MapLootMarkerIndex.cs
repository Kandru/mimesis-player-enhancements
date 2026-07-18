using UnityEngine;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator
{
    internal static class MapLootMarkerIndex
    {
        internal static void ShuffleMarkers<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        internal static List<MapMarker_LootingObjectSpawnPoint> CollectUnusedMarkers(
            int masterId,
            HashSet<int> usedMarkerIds,
            IReadOnlyList<MapMarker_LootingObjectSpawnPoint> allMarkers)
        {
            List<MapMarker_LootingObjectSpawnPoint> unused = [];

            foreach (MapMarker_LootingObjectSpawnPoint marker in allMarkers)
            {
                if (marker.masterID != masterId || usedMarkerIds.Contains(marker.ID))
                {
                    continue;
                }

                unused.Add(marker);
            }

            return unused;
        }

        internal static MapMarker_LootingObjectSpawnPoint[] CollectLootMarkers()
        {
            return UnityEngine.Object.FindObjectsByType<MapMarker_LootingObjectSpawnPoint>(FindObjectsSortMode.None);
        }
    }
}
