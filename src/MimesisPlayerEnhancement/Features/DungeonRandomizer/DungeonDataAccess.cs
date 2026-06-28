using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Bifrost.Cooked;

namespace MimesisPlayerEnhancement.Features.DungeonRandomizer;

internal static class DungeonDataAccess
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly PropertyInfo? HubDatamanProperty =
        typeof(Hub).GetProperty("dataman", InstanceFlags);

    internal static bool TryGetExcelDataManager(out ExcelDataManager excel)
    {
        excel = null!;
        if (Hub.s == null)
            return false;

        if (HubDatamanProperty?.GetValue(Hub.s) is not DataManager dataman)
            return false;

        excel = dataman.ExcelDataManager;
        return excel != null;
    }

    internal static List<int> GetFilteredActiveDungeonIds(HashSet<int> allowlist, HashSet<int> blocklist)
    {
        var pool = new List<int>();
        if (!TryGetExcelDataManager(out ExcelDataManager excel))
            return pool;

        ImmutableDictionary<int, DungeonMasterInfo> dict = excel.DungeonInfoDict;
        foreach (KeyValuePair<int, DungeonMasterInfo> entry in dict)
        {
            DungeonMasterInfo info = entry.Value;
            if (info == null || !info.IsActive)
                continue;

            int id = info.ID;
            if (allowlist.Count > 0 && !allowlist.Contains(id))
                continue;

            if (blocklist.Contains(id))
                continue;

            pool.Add(id);
        }

        return pool;
    }

    internal static bool TryPickUniform(IReadOnlyList<int> pool, out int dungeonId)
    {
        dungeonId = 0;
        if (pool.Count == 0)
            return false;

        int index = UnityEngine.Random.Range(0, pool.Count);
        dungeonId = pool[index];
        return true;
    }

    internal static bool TryPickUniformLayoutFlow(DungeonMasterInfo info, out string flowName)
    {
        flowName = string.Empty;
        ImmutableDictionary<string, int> candidates = info.DungenCandidates;
        if (candidates == null || candidates.Count == 0)
            return false;

        int index = UnityEngine.Random.Range(0, candidates.Count);
        foreach (string key in candidates.Keys)
        {
            if (index-- == 0)
            {
                flowName = key;
                return true;
            }
        }

        return false;
    }

    internal static bool TryPickUniformMapId(DungeonMasterInfo info, out int mapId)
    {
        mapId = 0;
        ImmutableArray<int> mapIds = info.MapIDs;
        if (mapIds.IsDefaultOrEmpty)
            return false;

        int index = UnityEngine.Random.Range(0, mapIds.Length);
        mapId = mapIds[index];
        return true;
    }
}
