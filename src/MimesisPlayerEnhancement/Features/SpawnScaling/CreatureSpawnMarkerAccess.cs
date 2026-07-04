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

        private static readonly MethodInfo? GetAllMonsterSpawnPointsMethod =
            AccessTools.Method(typeof(DynamicDataManager), "GetAllMonsterSpawnPoints");

        private static readonly MethodInfo? GetAllSpecialMonsterSpawnPointsMethod =
            AccessTools.Method(typeof(DynamicDataManager), "GetAllSpecialMonsterSpawnPoints");

        internal static IEnumerable<MapMarker_CreatureSpawnPoint> GetAllCreatureSpawnMarkers()
        {
            if (Hub.s == null
                || HubDynamicDataManProperty?.GetValue(Hub.s) is not DynamicDataManager dynamicDataMan)
            {
                yield break;
            }

            foreach (MapMarker_CreatureSpawnPoint marker in EnumerateMarkers(dynamicDataMan, GetAllMonsterSpawnPointsMethod))
            {
                yield return marker;
            }

            foreach (MapMarker_CreatureSpawnPoint marker in EnumerateMarkers(dynamicDataMan, GetAllSpecialMonsterSpawnPointsMethod))
            {
                yield return marker;
            }
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
