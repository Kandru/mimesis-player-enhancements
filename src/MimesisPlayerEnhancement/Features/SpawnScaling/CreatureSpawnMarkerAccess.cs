using System.Collections.Generic;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class CreatureSpawnMarkerAccess
    {
        internal static MapMarker_CreatureSpawnPoint[] CollectSceneMarkers()
        {
            return UnityEngine.Object.FindObjectsByType<MapMarker_CreatureSpawnPoint>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        }

        internal static void ShuffleMarkers<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        internal static List<MapMarker_CreatureSpawnPoint> CollectUnusedMarkers(
            int masterId,
            ICollection<int> usedMarkerIds,
            IReadOnlyList<MapMarker_CreatureSpawnPoint>? allMarkers = null)
        {
            List<MapMarker_CreatureSpawnPoint> unused = [];
            MapMarker_CreatureSpawnPoint[] markers = allMarkers as MapMarker_CreatureSpawnPoint[]
                ?? CollectSceneMarkers();

            foreach (MapMarker_CreatureSpawnPoint marker in markers)
            {
                if (marker.masterID != masterId || usedMarkerIds.Contains(marker.ID))
                {
                    continue;
                }

                unused.Add(marker);
            }

            return unused;
        }
    }
}
