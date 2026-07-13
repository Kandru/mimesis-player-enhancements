using DunGen;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    /// <summary>
    /// Appends teleporter, in-area, and cross-area connection points to minimap layouts.
    /// </summary>
    internal static class WebDashboardMinimapConnectionBuilder
    {
        private const string Feature = "WebDashboard";
        private const float EdgeTouchEpsilon = 0.002f;

        internal static void AppendTeleporterConnectionPoints(
            WebDashboardMinimapLayoutDto layout,
            GamePlayScene gps)
        {
            try
            {
                DungeonRoom? room = JoinAnytimeRoomTools.GetActiveDungeonRoom() as DungeonRoom;
                TeleporterLevelObject[] teleporters =
                    UnityEngine.Object.FindObjectsByType<TeleporterLevelObject>(FindObjectsSortMode.None);
                foreach (TeleporterLevelObject teleporter in teleporters)
                {
                    if (teleporter == null)
                    {
                        continue;
                    }

                    Vector3 position = teleporter.transform.position;
                    string? areaId = room != null
                        ? WebDashboardMinimapAreaResolver.ResolvePlayerAreaId(gps, room, position)
                        : null;
                    if (string.IsNullOrWhiteSpace(areaId))
                    {
                        continue;
                    }

                    WebDashboardMinimapAreaDto? area = null;
                    foreach (WebDashboardMinimapAreaDto candidate in layout.Areas)
                    {
                        if (candidate.Id == areaId)
                        {
                            area = candidate;
                            break;
                        }
                    }

                    if (area == null || area.Bounds.MaxX <= area.Bounds.MinX || area.Bounds.MaxZ <= area.Bounds.MinZ)
                    {
                        continue;
                    }

                    string targetAreaId = teleporter.DestinationIsToInDoor
                        ? ResolveFirstIndoorAreaId(layout, areaId)
                        : WebDashboardMinimapAreaResolver.OutdoorAreaId;
                    if (string.IsNullOrWhiteSpace(targetAreaId) || targetAreaId == areaId)
                    {
                        continue;
                    }

                    float spanX = area.Bounds.MaxX - area.Bounds.MinX;
                    float spanZ = area.Bounds.MaxZ - area.Bounds.MinZ;
                    Vector3 forward = teleporter.transform.forward;
                    area.ConnectionPoints.Add(new WebDashboardMinimapConnectionPointDto
                    {
                        X = WebDashboardMinimapMath.Normalize(position.x, area.Bounds.MinX, spanX),
                        Z = WebDashboardMinimapMath.Normalize(position.z, area.Bounds.MinZ, spanZ),
                        DirX = forward.x,
                        DirZ = forward.z,
                        TargetAreaId = targetAreaId,
                        CrossArea = true,
                        TeleporterId = teleporter.StartCallSign,
                        Label = WebDashboardMinimapPoiLabels.ResolveTeleporterLabel(
                            teleporter,
                            targetAreaId,
                            layout),
                    });
                }
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"Teleporter collection failed — {ex.Message}");
            }
        }

        private static string ResolveFirstIndoorAreaId(WebDashboardMinimapLayoutDto layout, string excludeAreaId)
        {
            foreach (WebDashboardMinimapAreaDto area in layout.Areas)
            {
                if (area.Kind == "indoor" && area.Id != excludeAreaId && area.Tiles.Count > 0)
                {
                    return area.Id;
                }
            }

            return WebDashboardMinimapAreaResolver.IndoorAreaId;
        }

        internal static void AppendCrossAreaConnections(
            WebDashboardMinimapLayoutDto layout,
            WebDashboardMinimapDungeonGraph fullGraph)
        {
            Dictionary<string, WebDashboardMinimapAreaDto> areasById = [];
            foreach (WebDashboardMinimapAreaDto area in layout.Areas)
            {
                areasById[area.Id] = area;
            }

            HashSet<string> seen = [];
            foreach ((int fromTileId, int toTileId) in fullGraph.Connections)
            {
                string? fromAreaId = WebDashboardMinimapTileRegistry.TryGetAreaId(fromTileId);
                string? toAreaId = WebDashboardMinimapTileRegistry.TryGetAreaId(toTileId);
                if (string.IsNullOrWhiteSpace(fromAreaId)
                    || string.IsNullOrWhiteSpace(toAreaId)
                    || fromAreaId == toAreaId)
                {
                    continue;
                }

                if (WebDashboardMinimapAreaResolver.IsIndoorAreaId(fromAreaId)
                    && WebDashboardMinimapAreaResolver.IsIndoorAreaId(toAreaId))
                {
                    continue;
                }

                TryAppendCrossAreaConnection(
                    fullGraph,
                    areasById,
                    seen,
                    fromAreaId,
                    fromTileId,
                    toTileId,
                    toAreaId);
                TryAppendCrossAreaConnection(
                    fullGraph,
                    areasById,
                    seen,
                    toAreaId,
                    toTileId,
                    fromTileId,
                    fromAreaId);
            }
        }

        private static void TryAppendCrossAreaConnection(
            WebDashboardMinimapDungeonGraph fullGraph,
            Dictionary<string, WebDashboardMinimapAreaDto> areasById,
            HashSet<string> seen,
            string areaId,
            int localTileId,
            int remoteTileId,
            string targetAreaId)
        {
            string dedupeKey = areaId + "|" + localTileId + "|" + remoteTileId;
            if (!seen.Add(dedupeKey))
            {
                return;
            }

            if (!areasById.TryGetValue(areaId, out WebDashboardMinimapAreaDto? area)
                || FindTileById(fullGraph, localTileId) is not Tile localTile
                || FindTileById(fullGraph, remoteTileId) is not Tile remoteTile)
            {
                return;
            }

            if (!TryComputeCrossAreaConnectionPoint(
                    localTile,
                    remoteTile,
                    area.Bounds,
                    out float pointX,
                    out float pointZ,
                    out float dirX,
                    out float dirZ))
            {
                return;
            }

            area.ConnectionPoints.Add(new WebDashboardMinimapConnectionPointDto
            {
                X = pointX,
                Z = pointZ,
                DirX = dirX,
                DirZ = dirZ,
                FromTileId = "tile-" + localTileId,
                ToTileId = "tile-" + remoteTileId,
                TargetAreaId = targetAreaId,
                CrossArea = true,
            });
        }

        private static Tile? FindTileById(WebDashboardMinimapDungeonGraph graph, int tileId)
        {
            foreach (KeyValuePair<Tile, int> entry in graph.TileIds)
            {
                if (entry.Value == tileId)
                {
                    return entry.Key;
                }
            }

            return null;
        }

        private static bool TryComputeCrossAreaConnectionPoint(
            Tile localTile,
            Tile remoteTile,
            WebDashboardMinimapBoundsDto areaBounds,
            out float pointX,
            out float pointZ,
            out float dirX,
            out float dirZ)
        {
            pointX = 0f;
            pointZ = 0f;
            dirX = 0f;
            dirZ = 0f;

            Bounds localBounds = localTile.Placement?.Bounds ?? localTile.Bounds;
            Bounds remoteBounds = remoteTile.Placement?.Bounds ?? remoteTile.Bounds;

            float localCenterX = localBounds.center.x;
            float localCenterZ = localBounds.center.z;
            float remoteCenterX = remoteBounds.center.x;
            float remoteCenterZ = remoteBounds.center.z;

            float worldDirX = remoteCenterX - localCenterX;
            float worldDirZ = remoteCenterZ - localCenterZ;
            float worldLength = Mathf.Sqrt((worldDirX * worldDirX) + (worldDirZ * worldDirZ));
            if (worldLength <= 0.001f)
            {
                return false;
            }

            worldDirX /= worldLength;
            worldDirZ /= worldLength;

            float halfW = Mathf.Max(localBounds.extents.x, 0.5f);
            float halfH = Mathf.Max(localBounds.extents.z, 0.5f);
            float edgeX = localCenterX + (worldDirX * halfW);
            float edgeZ = localCenterZ + (worldDirZ * halfH);

            float spanX = Mathf.Max(areaBounds.MaxX - areaBounds.MinX, 1f);
            float spanZ = Mathf.Max(areaBounds.MaxZ - areaBounds.MinZ, 1f);
            pointX = WebDashboardMinimapMath.Normalize(edgeX, areaBounds.MinX, spanX);
            pointZ = WebDashboardMinimapMath.Normalize(edgeZ, areaBounds.MinZ, spanZ);

            float normDirX = worldDirX / spanX;
            float normDirZ = worldDirZ / spanZ;
            float normLength = Mathf.Sqrt((normDirX * normDirX) + (normDirZ * normDirZ));
            if (normLength <= 0.001f)
            {
                dirX = worldDirX;
                dirZ = worldDirZ;
            }
            else
            {
                dirX = normDirX / normLength;
                dirZ = normDirZ / normLength;
            }

            return true;
        }

        internal static bool TryComputeConnectionDirection(
            WebDashboardMinimapTileDto from,
            WebDashboardMinimapTileDto to,
            out float dirX,
            out float dirZ)
        {
            float fromCenterX = from.X + (from.W * 0.5f);
            float fromCenterZ = from.Z + (from.H * 0.5f);
            float toCenterX = to.X + (to.W * 0.5f);
            float toCenterZ = to.Z + (to.H * 0.5f);

            dirX = toCenterX - fromCenterX;
            dirZ = toCenterZ - fromCenterZ;
            float length = Mathf.Sqrt((dirX * dirX) + (dirZ * dirZ));
            if (length <= 0.0001f)
            {
                return false;
            }

            dirX /= length;
            dirZ /= length;
            return true;
        }

        internal static bool TryComputeConnectionPoint(
            WebDashboardMinimapTileDto from,
            WebDashboardMinimapTileDto to,
            out float x,
            out float z)
        {
            float fromLeft = from.X;
            float fromRight = from.X + from.W;
            float fromTop = from.Z;
            float fromBottom = from.Z + from.H;
            float toLeft = to.X;
            float toRight = to.X + to.W;
            float toTop = to.Z;
            float toBottom = to.Z + to.H;

            float overlapLeft = Mathf.Max(fromLeft, toLeft);
            float overlapRight = Mathf.Min(fromRight, toRight);
            float overlapTop = Mathf.Max(fromTop, toTop);
            float overlapBottom = Mathf.Min(fromBottom, toBottom);

            if (Mathf.Abs(fromRight - toLeft) <= EdgeTouchEpsilon && overlapTop < overlapBottom)
            {
                x = fromRight;
                z = (overlapTop + overlapBottom) * 0.5f;
                return true;
            }

            if (Mathf.Abs(toRight - fromLeft) <= EdgeTouchEpsilon && overlapTop < overlapBottom)
            {
                x = fromLeft;
                z = (overlapTop + overlapBottom) * 0.5f;
                return true;
            }

            if (Mathf.Abs(fromBottom - toTop) <= EdgeTouchEpsilon && overlapLeft < overlapRight)
            {
                x = (overlapLeft + overlapRight) * 0.5f;
                z = fromBottom;
                return true;
            }

            if (Mathf.Abs(toBottom - fromTop) <= EdgeTouchEpsilon && overlapLeft < overlapRight)
            {
                x = (overlapLeft + overlapRight) * 0.5f;
                z = fromTop;
                return true;
            }

            x = ((fromLeft + fromRight) * 0.5f + (toLeft + toRight) * 0.5f) * 0.5f;
            z = ((fromTop + fromBottom) * 0.5f + (toTop + toBottom) * 0.5f) * 0.5f;
            return true;
        }
    }
}
