namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsMapNames
    {
        internal static void Resolve(int mapId, out string mapKey, out string mapName)
        {
            mapKey = "";
            mapName = $"Map {mapId}";
            if (mapId <= 0)
            {
                return;
            }

            try
            {
                ExcelDataManager? excel = HubGameDataAccess.Excel;
                MapMasterInfo? info = excel?.GetMapInfo(mapId);
                if (info == null)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(info.SceneName))
                {
                    mapKey = info.SceneName;
                }

                if (!string.IsNullOrWhiteSpace(info.SceneName))
                {
                    mapName = info.SceneName;
                }
            }
            catch
            {
                // best-effort
            }
        }
    }
}
