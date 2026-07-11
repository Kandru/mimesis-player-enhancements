using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardDungeonCatalogService
    {
        internal static IReadOnlyList<WebDashboardDungeonOptionDto> BuildCatalog()
        {
            ExcelDataManager? excel = HubGameDataAccess.Excel;
            if (excel == null)
            {
                return [];
            }

            List<WebDashboardDungeonOptionDto> options = [];

            foreach (KeyValuePair<int, DungeonMasterInfo> entry in excel.DungeonInfoDict)
            {
                DungeonMasterInfo info = entry.Value;
                if (info is not { IsActive: true })
                {
                    continue;
                }

                int id = info.ID;
                options.Add(new WebDashboardDungeonOptionDto
                {
                    Id = id.ToString(),
                    Label = ResolveLabel(excel, info),
                });
            }

            options.Sort((a, b) => string.Compare(a.Label, b.Label, StringComparison.OrdinalIgnoreCase));
            return options;
        }

        private static string ResolveLabel(ExcelDataManager excel, DungeonMasterInfo info)
        {
            string idLabel = info.ID.ToString();
            if (info.MapIDs.IsDefaultOrEmpty)
            {
                return idLabel;
            }

            int mapId = info.MapIDs[0];
            MapMasterInfo? mapInfo = excel.GetMapInfo(mapId);
            if (mapInfo == null || string.IsNullOrWhiteSpace(mapInfo.SceneName))
            {
                return idLabel;
            }

            return mapInfo.SceneName + " (" + idLabel + ")";
        }
    }
}
