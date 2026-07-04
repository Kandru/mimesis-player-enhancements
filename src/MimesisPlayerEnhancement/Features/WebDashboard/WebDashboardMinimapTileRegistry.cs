using DunGen;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMinimapTileRegistry
    {
        private static readonly Dictionary<int, string> TileIdToAreaId = [];
        private static readonly Dictionary<(int X, int Y), string> GridCoordToAreaId = [];

        internal static void Clear()
        {
            TileIdToAreaId.Clear();
            GridCoordToAreaId.Clear();
        }

        internal static void RegisterGraph(WebDashboardMinimapDungeonGraph graph, string areaId)
        {
            foreach (KeyValuePair<Tile, int> entry in graph.TileIds)
            {
                TileIdToAreaId[entry.Value] = areaId;
            }
        }

        internal static void RegisterGridGraph(WebDashboardMinimapGridGraph graph, string areaId)
        {
            foreach (WebDashboardMinimapGridCell cell in graph.Cells)
            {
                GridCoordToAreaId[(cell.Coordinate.X, cell.Coordinate.Y)] = areaId;
            }
        }

        internal static string? TryGetAreaId(int tileId)
        {
            return TileIdToAreaId.TryGetValue(tileId, out string? areaId) ? areaId : null;
        }

        internal static string? TryGetGridAreaId(int gridX, int gridY)
        {
            return GridCoordToAreaId.TryGetValue((gridX, gridY), out string? areaId) ? areaId : null;
        }
    }
}
