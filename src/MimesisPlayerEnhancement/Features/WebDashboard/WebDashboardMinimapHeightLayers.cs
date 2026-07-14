using DunGen;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMinimapHeightLayers
    {
        private const float MinFloorCenterGap = 3.5f;

        internal readonly struct HeightLayer
        {
            internal HeightLayer(int index, List<Tile> tiles, float minY, float maxY)
            {
                Index = index;
                Tiles = tiles;
                MinY = minY;
                MaxY = maxY;
            }

            internal readonly int Index;
            internal readonly List<Tile> Tiles;
            internal readonly float MinY;
            internal readonly float MaxY;
        }

        internal static List<HeightLayer> ClusterTilesByHeight(IReadOnlyList<Tile> tiles)
        {
            List<(Tile tile, float centerY, float minY, float maxY)> entries = [];

            foreach (Tile tile in tiles)
            {
                if (tile == null)
                {
                    continue;
                }

                Bounds bounds = tile.Placement?.Bounds ?? tile.Bounds;
                entries.Add((tile, bounds.center.y, bounds.min.y, bounds.max.y));
            }

            if (entries.Count == 0)
            {
                return [];
            }

            entries.Sort((left, right) => left.centerY.CompareTo(right.centerY));

            List<HeightLayer> layers = [];
            List<Tile> current = [entries[0].tile];
            float centerSum = entries[0].centerY;
            float layerMinY = entries[0].minY;
            float layerMaxY = entries[0].maxY;

            for (int index = 1; index < entries.Count; index++)
            {
                (Tile tile, float centerY, float minY, float maxY) entry = entries[index];
                float averageCenter = centerSum / current.Count;
                if (entry.centerY - averageCenter >= MinFloorCenterGap)
                {
                    layers.Add(new HeightLayer(layers.Count, current, layerMinY, layerMaxY));
                    current = [entry.tile];
                    centerSum = entry.centerY;
                    layerMinY = entry.minY;
                    layerMaxY = entry.maxY;
                    continue;
                }

                current.Add(entry.tile);
                centerSum += entry.centerY;
                layerMinY = Mathf.Min(layerMinY, entry.minY);
                layerMaxY = Mathf.Max(layerMaxY, entry.maxY);
            }

            layers.Add(new HeightLayer(layers.Count, current, layerMinY, layerMaxY));
            return layers;
        }

        internal static List<int> ResolveFloorSpan(float tileMinY, float tileMaxY, IReadOnlyList<HeightLayer> layers)
        {
            List<int> span = [];
            const float overlapEpsilon = 0.5f;

            foreach (HeightLayer layer in layers)
            {
                if (tileMaxY >= layer.MinY - overlapEpsilon && tileMinY <= layer.MaxY + overlapEpsilon)
                {
                    span.Add(layer.Index);
                }
            }

            return span;
        }

        internal static WebDashboardMinimapDungeonGraph ExtractSubgraph(
            WebDashboardMinimapDungeonGraph source,
            IReadOnlyList<Tile> layerTiles)
        {
            WebDashboardMinimapDungeonGraph graph = new();
            HashSet<int> layerTileIds = [];

            foreach (Tile tile in layerTiles)
            {
                if (tile == null || !source.TileIds.TryGetValue(tile, out int tileId))
                {
                    continue;
                }

                graph.Tiles.Add(tile);
                graph.TileIds[tile] = tileId;
                _ = layerTileIds.Add(tileId);
                if (source.MainPath.Contains(tile))
                {
                    _ = graph.MainPath.Add(tile);
                }
            }

            foreach ((int from, int to) in source.Connections)
            {
                if (layerTileIds.Contains(from) && layerTileIds.Contains(to))
                {
                    graph.Connections.Add((from, to));
                }
            }

            return graph;
        }

        internal static string BuildIndoorLayerAreaId(int layerIndex, int layerCount)
        {
            return layerCount <= 1
                ? WebDashboardMinimapAreaResolver.IndoorAreaId
                : WebDashboardMinimapAreaResolver.IndoorAreaId + "-" + layerIndex;
        }

        internal static string BuildIndoorLayerLabel(int layerIndex, int layerCount)
        {
            return layerCount <= 1 ? "Indoor" : "Indoor · Floor " + (layerIndex + 1);
        }
    }
}
