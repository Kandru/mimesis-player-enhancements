using DunGen;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    /// <summary>
    /// Builds connection points from DunGen doorway transforms for accurate door placement.
    /// </summary>
    internal static class WebDashboardMinimapDoorwayBuilder
    {
        private const float DoorOpeningMeters = 2f;

        internal static void AppendDoorwayConnections(
            WebDashboardMinimapAreaDto area,
            WebDashboardMinimapDungeonGraph graph,
            Dictionary<string, WebDashboardMinimapTileDto> tilesById,
            WebDashboardMinimapBoundsDto areaBounds)
        {
            HashSet<string> seen = [];
            foreach (Tile tile in graph.Tiles)
            {
                if (tile == null || !graph.TileIds.TryGetValue(tile, out int fromId))
                {
                    continue;
                }

                string fromTileId = $"tile-{fromId}";
                if (!tilesById.ContainsKey(fromTileId))
                {
                    continue;
                }

                foreach (Doorway doorway in tile.UsedDoorways)
                {
                    if (doorway == null || doorway.ConnectedDoorway == null)
                    {
                        continue;
                    }

                    Tile? remoteTile = doorway.ConnectedDoorway.Tile;
                    if (remoteTile == null || !graph.TileIds.TryGetValue(remoteTile, out int toId))
                    {
                        continue;
                    }

                    string toTileId = $"tile-{toId}";
                    if (!tilesById.ContainsKey(toTileId))
                    {
                        continue;
                    }

                    string pairKey = fromId < toId ? $"{fromId}|{toId}" : $"{toId}|{fromId}";
                    if (!seen.Add(pairKey))
                    {
                        continue;
                    }

                    if (!TryBuildDoorwayPoint(doorway, tile, remoteTile, areaBounds, fromTileId, toTileId, out WebDashboardMinimapConnectionPointDto point))
                    {
                        continue;
                    }

                    area.ConnectionPoints.Add(point);
                }
            }
        }

        private static bool TryBuildDoorwayPoint(
            Doorway doorway,
            Tile localTile,
            Tile remoteTile,
            WebDashboardMinimapBoundsDto areaBounds,
            string fromTileId,
            string toTileId,
            out WebDashboardMinimapConnectionPointDto point)
        {
            point = new WebDashboardMinimapConnectionPointDto();

            Bounds localBounds = localTile.Placement?.Bounds ?? localTile.Bounds;
            Vector3 worldPos = doorway.ProjectPositionToTileBounds(localBounds);
            Vector3 forward = doorway.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                Bounds remoteBounds = remoteTile.Placement?.Bounds ?? remoteTile.Bounds;
                Vector3 delta = remoteBounds.center - localBounds.center;
                delta.y = 0f;
                forward = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.forward;
            }
            else
            {
                forward.Normalize();
            }

            float spanX = Mathf.Max(areaBounds.MaxX - areaBounds.MinX, 1f);
            float spanZ = Mathf.Max(areaBounds.MaxZ - areaBounds.MinZ, 1f);
            float avgSpan = (spanX + spanZ) * 0.5f;
            float width = DoorOpeningMeters / Mathf.Max(avgSpan, 1f);

            point.X = WebDashboardMinimapMath.Normalize(worldPos.x, areaBounds.MinX, spanX);
            point.Z = WebDashboardMinimapMath.Normalize(worldPos.z, areaBounds.MinZ, spanZ);

            Vector3 tangent = new(-forward.z, 0f, forward.x);
            point.DirX = tangent.x / spanX;
            point.DirZ = tangent.z / spanZ;
            float dirLen = Mathf.Sqrt((point.DirX * point.DirX) + (point.DirZ * point.DirZ));
            if (dirLen > 0.0001f)
            {
                point.DirX /= dirLen;
                point.DirZ /= dirLen;
            }

            point.FromTileId = fromTileId;
            point.ToTileId = toTileId;
            point.Width = width;
            point.CrossArea = false;
            return true;
        }

        internal static void AppendCrossFloorConnections(
            WebDashboardMinimapLayoutDto layout,
            WebDashboardMinimapDungeonGraph fullGraph,
            Dictionary<int, int> tileFloorById)
        {
            Dictionary<string, WebDashboardMinimapAreaDto> areasById = [];
            foreach (WebDashboardMinimapAreaDto area in layout.Areas)
            {
                areasById[area.Id] = area;
            }

            HashSet<string> seen = [];
            foreach ((int from, int to) in fullGraph.Connections)
            {
                if (!tileFloorById.TryGetValue(from, out int fromFloor)
                    || !tileFloorById.TryGetValue(to, out int toFloor)
                    || fromFloor == toFloor)
                {
                    continue;
                }

                string? fromAreaId = WebDashboardMinimapTileRegistry.TryGetAreaId(from);
                string? toAreaId = WebDashboardMinimapTileRegistry.TryGetAreaId(to);
                if (string.IsNullOrWhiteSpace(fromAreaId) || !areasById.TryGetValue(fromAreaId, out WebDashboardMinimapAreaDto? fromArea))
                {
                    continue;
                }

                if (!TryFindDoorwayPair(fullGraph, from, to, out Doorway? doorway, out Tile? localTile))
                {
                    continue;
                }

                string dedupe = $"{from}|{to}|stairs";
                if (!seen.Add(dedupe))
                {
                    continue;
                }

                Bounds bounds = localTile!.Placement?.Bounds ?? localTile.Bounds;
                Vector3 worldPos = doorway!.ProjectPositionToTileBounds(bounds);
                float spanX = Mathf.Max(fromArea.Bounds.MaxX - fromArea.Bounds.MinX, 1f);
                float spanZ = Mathf.Max(fromArea.Bounds.MaxZ - fromArea.Bounds.MinZ, 1f);

                fromArea.ConnectionPoints.Add(new WebDashboardMinimapConnectionPointDto
                {
                    X = WebDashboardMinimapMath.Normalize(worldPos.x, fromArea.Bounds.MinX, spanX),
                    Z = WebDashboardMinimapMath.Normalize(worldPos.z, fromArea.Bounds.MinZ, spanZ),
                    FromTileId = $"tile-{from}",
                    ToTileId = $"tile-{to}",
                    TargetAreaId = toAreaId ?? "",
                    CrossFloor = true,
                    CrossArea = false,
                });
            }
        }

        private static bool TryFindDoorwayPair(
            WebDashboardMinimapDungeonGraph graph,
            int fromId,
            int toId,
            out Doorway? doorway,
            out Tile? localTile)
        {
            doorway = null;
            localTile = null;
            foreach (Tile tile in graph.Tiles)
            {
                if (tile == null || !graph.TileIds.TryGetValue(tile, out int tileId) || tileId != fromId)
                {
                    continue;
                }

                localTile = tile;
                foreach (Doorway used in tile.UsedDoorways)
                {
                    if (used?.ConnectedDoorway?.Tile == null)
                    {
                        continue;
                    }

                    if (graph.TileIds.TryGetValue(used.ConnectedDoorway.Tile, out int remoteId) && remoteId == toId)
                    {
                        doorway = used;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
