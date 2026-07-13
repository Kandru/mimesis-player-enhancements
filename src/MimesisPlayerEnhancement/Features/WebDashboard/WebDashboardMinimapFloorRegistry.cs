namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMinimapFloorRegistry
    {
        private sealed class FloorLayerEntry
        {
            internal int FloorIndex;
            internal string AreaId = "";
            internal float MinY;
            internal float MaxY;
        }

        private static readonly List<FloorLayerEntry> Layers = [];
        private static readonly Dictionary<string, List<int>> TileFloorSpanById = [];
        private static readonly Dictionary<int, string> AreaIdByFloorIndex = [];

        internal static void Clear()
        {
            Layers.Clear();
            TileFloorSpanById.Clear();
            AreaIdByFloorIndex.Clear();
        }

        internal static void RegisterLayer(int floorIndex, string areaId, float minY, float maxY)
        {
            Layers.Add(new FloorLayerEntry
            {
                FloorIndex = floorIndex,
                AreaId = areaId,
                MinY = minY,
                MaxY = maxY,
            });
            AreaIdByFloorIndex[floorIndex] = areaId;
        }

        internal static void RegisterTileFloorSpan(string tileId, IReadOnlyList<int> floorSpan)
        {
            if (floorSpan.Count == 0)
            {
                return;
            }

            TileFloorSpanById[tileId] = [.. floorSpan];
        }

        internal static int ResolveFloorIndex(float worldY, string areaId)
        {
            int bestIndex = 0;
            float bestDistance = float.MaxValue;
            bool matchedArea = false;

            foreach (FloorLayerEntry layer in Layers)
            {
                if (!string.IsNullOrWhiteSpace(areaId)
                    && layer.AreaId != areaId
                    && !WebDashboardMinimapAreaResolver.IsIndoorAreaId(areaId))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(areaId)
                    && WebDashboardMinimapAreaResolver.IsIndoorAreaId(areaId)
                    && layer.AreaId != areaId)
                {
                    continue;
                }

                matchedArea = true;
                if (worldY >= layer.MinY - 0.5f && worldY <= layer.MaxY + 0.5f)
                {
                    return layer.FloorIndex;
                }

                float centerY = (layer.MinY + layer.MaxY) * 0.5f;
                float distance = UnityEngine.Mathf.Abs(worldY - centerY);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = layer.FloorIndex;
                }
            }

            return matchedArea ? bestIndex : 0;
        }

        internal static IReadOnlyList<int> TryGetTileFloorSpan(string tileId)
        {
            return TileFloorSpanById.TryGetValue(tileId, out List<int>? span) ? span : [];
        }

        internal static string? TryGetAreaIdForFloor(int floorIndex)
        {
            return AreaIdByFloorIndex.TryGetValue(floorIndex, out string? areaId) ? areaId : null;
        }
    }
}
