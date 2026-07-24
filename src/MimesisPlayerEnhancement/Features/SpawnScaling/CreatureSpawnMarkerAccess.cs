using System.Collections;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class CreatureSpawnMarkerAccess
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly PropertyInfo? HubDynamicDataManProperty =
            typeof(Hub).GetProperty("dynamicDataMan", InstanceFlags);

        // game@0.3.1 Assembly-CSharp/DynamicDataManager.cs:L485-488
        private static readonly MethodInfo? GetAllMonsterSpawnPointsMethod =
            AccessTools.Method(typeof(DynamicDataManager), "GetAllMonsterSpawnPoints");

        // game@0.3.1 Assembly-CSharp/DynamicDataManager.cs:L490-493
        private static readonly MethodInfo? GetAllSpecialMonsterSpawnPointsMethod =
            AccessTools.Method(typeof(DynamicDataManager), "GetAllSpecialMonsterSpawnPoints");

        internal static Dictionary<int, List<MapMarker_CreatureSpawnPoint>> CollectByMasterId()
        {
            Dictionary<int, List<MapMarker_CreatureSpawnPoint>> byMasterId = [];

            if (Hub.s == null
                || HubDynamicDataManProperty?.GetValue(Hub.s) is not DynamicDataManager dynamicDataMan)
            {
                return byMasterId;
            }

            foreach (MapMarker_CreatureSpawnPoint marker in EnumerateMarkers(dynamicDataMan, GetAllMonsterSpawnPointsMethod))
            {
                AddMarker(byMasterId, marker);
            }

            foreach (MapMarker_CreatureSpawnPoint marker in EnumerateMarkers(dynamicDataMan, GetAllSpecialMonsterSpawnPointsMethod))
            {
                AddMarker(byMasterId, marker);
            }

            return byMasterId;
        }

        private static void AddMarker(
            Dictionary<int, List<MapMarker_CreatureSpawnPoint>> byMasterId,
            MapMarker_CreatureSpawnPoint marker)
        {
            if (!byMasterId.TryGetValue(marker.masterID, out List<MapMarker_CreatureSpawnPoint>? list))
            {
                list = [];
                byMasterId[marker.masterID] = list;
            }

            list.Add(marker);
        }

        private static IEnumerable<MapMarker_CreatureSpawnPoint> EnumerateMarkers(
            DynamicDataManager dynamicDataMan,
            MethodInfo? method)
        {
            if (method == null)
            {
                yield break;
            }

            if (method.Invoke(dynamicDataMan, null) is not IDictionary dictionary)
            {
                yield break;
            }

            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Value is MapMarker_CreatureSpawnPoint marker)
                {
                    yield return marker;
                }
            }
        }
    }
}
