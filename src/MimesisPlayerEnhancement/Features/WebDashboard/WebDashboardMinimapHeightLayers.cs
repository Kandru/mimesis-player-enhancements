using System;
using System.Collections.Generic;
using DunGen;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMinimapHeightLayers
    {
        private const float MinFloorCenterGap = 3.5f;

        internal static List<List<Tile>> ClusterTilesByHeight(IReadOnlyList<Tile> tiles)
        {
            List<(Tile tile, float centerY)> entries = [];

            foreach (Tile tile in tiles)
            {
                if (tile == null)
                {
                    continue;
                }

                Bounds bounds = tile.Placement?.Bounds ?? tile.Bounds;
                entries.Add((tile, bounds.center.y));
            }

            if (entries.Count == 0)
            {
                return [];
            }

            entries.Sort((left, right) => left.centerY.CompareTo(right.centerY));

            List<List<Tile>> clusters = [];
            List<Tile> current = [entries[0].tile];
            float centerSum = entries[0].centerY;

            for (int index = 1; index < entries.Count; index++)
            {
                (Tile tile, float centerY) entry = entries[index];
                float averageCenter = centerSum / current.Count;
                if (entry.centerY - averageCenter >= MinFloorCenterGap)
                {
                    clusters.Add(current);
                    current = [entry.tile];
                    centerSum = entry.centerY;
                    continue;
                }

                current.Add(entry.tile);
                centerSum += entry.centerY;
            }

            clusters.Add(current);
            return clusters;
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
