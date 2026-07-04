using System.Reflection;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal sealed class WebDashboardMinimapGridCell
    {
        internal IVSpace Space = null!;
        internal GridCoordinate Coordinate;
        internal int Id;
        internal float CenterX;
        internal float CenterZ;
        internal float Size;
    }

    internal sealed class WebDashboardMinimapGridGraph
    {
        internal List<WebDashboardMinimapGridCell> Cells = [];
        internal Dictionary<IVSpace, int> SpaceIds = [];
        internal List<(int From, int To)> Connections = [];
    }

    internal static class WebDashboardMinimapGridSource
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? VSpaceSizeField =
            AccessTools.Field(typeof(VSpaceGridGroup), "m_VSpaceSize");

        internal static bool TryBuildFromGridGroup(
            VSpaceGridGroup gridGroup,
            out WebDashboardMinimapGridGraph graph)
        {
            graph = new WebDashboardMinimapGridGraph();
            if (gridGroup == null)
            {
                return false;
            }

            float cellSize = ResolveCellSize(gridGroup);
            IReadOnlyList<IVSpace> spaces = gridGroup.GetAllSpaces();
            if (spaces == null || spaces.Count == 0)
            {
                return false;
            }

            graph.Cells.Clear();
            graph.SpaceIds.Clear();
            graph.Connections.Clear();

            int nextId = 0;
            foreach (IVSpace space in spaces)
            {
                if (space?.Coordinate is not GridCoordinate coordinate)
                {
                    continue;
                }

                if (!TryGetCellCenter(gridGroup, coordinate, out float centerX, out float centerZ))
                {
                    continue;
                }

                int id = nextId++;
                WebDashboardMinimapGridCell cell = new()
                {
                    Space = space,
                    Coordinate = coordinate,
                    Id = id,
                    CenterX = centerX,
                    CenterZ = centerZ,
                    Size = cellSize,
                };
                graph.Cells.Add(cell);
                graph.SpaceIds[space] = id;
            }

            if (graph.Cells.Count == 0)
            {
                return false;
            }

            HashSet<(int, int)> seenConnections = [];
            foreach (WebDashboardMinimapGridCell cell in graph.Cells)
            {
                IReadOnlyList<IVSpace> neighbors = gridGroup.GetAroundSpaces(cell.Space);
                if (neighbors == null)
                {
                    continue;
                }

                foreach (IVSpace neighbor in neighbors)
                {
                    if (!graph.SpaceIds.TryGetValue(neighbor, out int toId) || toId == cell.Id)
                    {
                        continue;
                    }

                    int fromId = cell.Id;
                    (int, int) pair = fromId <= toId ? (fromId, toId) : (toId, fromId);
                    if (!seenConnections.Add(pair))
                    {
                        continue;
                    }

                    graph.Connections.Add(pair);
                }
            }

            return true;
        }

        internal static string BuildCellId(GridCoordinate coordinate)
        {
            return $"grid-{coordinate.X}-{coordinate.Y}";
        }

        internal static string BuildCellId(int gridX, int gridY)
        {
            return $"grid-{gridX}-{gridY}";
        }

        private static float ResolveCellSize(VSpaceGridGroup gridGroup)
        {
            if (VSpaceSizeField?.GetValue(gridGroup) is float size && size > 0f)
            {
                return size;
            }

            return 1f;
        }

        private static bool TryGetCellCenter(
            VSpaceGridGroup gridGroup,
            GridCoordinate coordinate,
            out float centerX,
            out float centerZ)
        {
            centerX = 0f;
            centerZ = 0f;

            try
            {
                float cellSize = ResolveCellSize(gridGroup);
                SPoint center = gridGroup.GetCenter();
                centerX = center.X + (coordinate.X * cellSize) + (cellSize * 0.5f);
                centerZ = center.Z + (coordinate.Y * cellSize) + (cellSize * 0.5f);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
