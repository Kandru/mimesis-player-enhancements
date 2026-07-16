using System.Collections.Immutable;

namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonDataAccess
    {
        internal static ExcelDataManager? Excel => HubGameDataAccess.Excel;

        internal static List<int> GetFilteredActiveDungeonIds(HashSet<int> allowlist, HashSet<int> blocklist)
        {
            List<int> pool = [];
            ExcelDataManager? excel = Excel;
            if (excel == null)
            {
                return pool;
            }

            foreach (KeyValuePair<int, DungeonMasterInfo> entry in excel.DungeonInfoDict)
            {
                DungeonMasterInfo info = entry.Value;
                if (info is not { IsActive: true })
                {
                    continue;
                }

                int id = info.ID;
                if (allowlist.Count > 0 && !allowlist.Contains(id))
                {
                    continue;
                }

                if (blocklist.Contains(id))
                {
                    continue;
                }

                pool.Add(id);
            }

            return pool;
        }

        internal static bool IsExcluded(int dungeonId, IReadOnlyList<int> excludeIds)
        {
            for (int i = 0; i < excludeIds.Count; i++)
            {
                if (excludeIds[i] == dungeonId)
                {
                    return true;
                }
            }

            return false;
        }

        internal static List<int> FilterExcluded(IReadOnlyList<int> pool, IReadOnlyList<int> excludeIds)
        {
            if (excludeIds.Count == 0)
            {
                return [.. pool];
            }

            List<int> filtered = new(pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                int id = pool[i];
                if (!IsExcluded(id, excludeIds))
                {
                    filtered.Add(id);
                }
            }

            return filtered;
        }

        internal static bool TryPickUniform(IReadOnlyList<int> pool, out int dungeonId)
        {
            dungeonId = 0;
            if (pool.Count == 0)
            {
                return false;
            }

            dungeonId = pool[UnityEngine.Random.Range(0, pool.Count)];
            return true;
        }

        internal static bool TryPickUniformMapId(DungeonMasterInfo info, out int mapId)
        {
            mapId = 0;
            ImmutableArray<int> mapIds = info.MapIDs;
            if (mapIds.Length == 0)
            {
                return false;
            }

            mapId = mapIds[UnityEngine.Random.Range(0, mapIds.Length)];
            return true;
        }
    }
}
