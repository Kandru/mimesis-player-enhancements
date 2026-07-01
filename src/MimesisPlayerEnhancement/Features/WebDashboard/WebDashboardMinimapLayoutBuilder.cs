using System;
using System.Collections.Generic;
using System.Reflection;
using Bifrost.Cooked;
using DunGen;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMinimapLayoutBuilder
    {
        private const string Feature = "WebDashboard";
        private const float BoundsPadding = 0.05f;
        private const float HubMinSpan = 40f;
        private const float HubMaxSpan = 120f;
        private const float HubFallbackHalfSpan = 75f;
        private const float EdgeTouchEpsilon = 0.002f;

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static string _cachedRunKey = "";
        private static bool _rebuildRequested;

        internal static WebDashboardMinimapLayoutDto Current { get; private set; } = new();

        internal static int LayoutVersion { get; private set; }

        internal static void RequestRebuild()
        {
            _rebuildRequested = true;
            _cachedRunKey = "";
            WebDashboardSnapshotCache.MarkDirty();
        }

        internal static void EnsureLayout()
        {
            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            GameMainBase? main = pdata?.main;
            string runKey = BuildRunKey(main);

            if (!_rebuildRequested && runKey == _cachedRunKey)
            {
                if (Current.DisplayMode == "map" && HasAnyAreaTiles(Current))
                {
                    return;
                }

                if (Current.DisplayMode is "hidden" or "markers-only" && runKey.StartsWith("hub:", StringComparison.Ordinal))
                {
                    return;
                }

                if (main is GamePlayScene gps && Current.LayoutKind == "dungeon")
                {
                    if (!WebDashboardMinimapDungeonSource.HasTiles(gps))
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            _rebuildRequested = false;
            _cachedRunKey = runKey;
            Current = main switch
            {
                GamePlayScene gps => TryBuildDungeonLayout(gps, runKey),
                InTramWaitingScene => BuildHiddenLayout("Tram waiting room"),
                MaintenanceScene => BuildHiddenLayout("Maintenance bay"),
                DeathMatchScene => BuildHiddenLayout("Death match"),
                _ => WebDashboardMinimapAreaResolver.ShouldHideMap(main)
                    ? BuildHiddenLayout(ResolveSceneLabel(main))
                    : BuildHubLayout(main, ResolveSceneLabel(main)),
            };

            Current.LayoutVersion = ++LayoutVersion;
        }

        private static string BuildRunKey(GameMainBase? main)
        {
            if (main is GamePlayScene gps)
            {
                long roomUid = ResolveRoomUid(gps);
                return $"dungeon:{gps.DungeonMasterID}:{gps.RandDungeonSeed}:{roomUid}";
            }

            return main is InTramWaitingScene
                ? "hub:waiting"
                : main is MaintenanceScene ? "hub:maintenance" : $"hub:{main?.GetType().Name ?? "unknown"}";
        }

        private static WebDashboardMinimapLayoutDto TryBuildDungeonLayout(GamePlayScene gps, string runKey)
        {
            string sceneLabel = ResolveDungeonLabel(gps);
            WebDashboardMinimapLayoutDto layout = new()
            {
                LayoutKind = "dungeon",
                DisplayMode = "map",
                SceneLabel = sceneLabel,
            };

            WebDashboardMinimapTileRegistry.Clear();
            bool addedArea = false;
            if (JoinAnytimeRoomTools.GetActiveDungeonRoom() is DungeonRoom room)
            {
                if (WebDashboardMinimapOutdoorSource.TryBuildOutdoorArea(gps) is WebDashboardMinimapAreaDto outdoorArea)
                {
                    layout.Areas.Add(outdoorArea);
                    addedArea = true;
                }
                else if (WebDashboardMinimapAreaResolver.TryGetOutdoorGridGroup(room) is VSpaceGridGroup outdoorGrid
                    && WebDashboardMinimapGridSource.TryBuildFromGridGroup(outdoorGrid, out WebDashboardMinimapGridGraph outdoorGridGraph))
                {
                    outdoorArea = BuildAreaFromGridGraph(
                        outdoorGridGraph,
                        WebDashboardMinimapAreaResolver.OutdoorAreaId,
                        "Outdoor",
                        "outdoor");
                    WebDashboardMinimapTileRegistry.RegisterGridGraph(outdoorGridGraph, outdoorArea.Id);
                    layout.Areas.Add(outdoorArea);
                    addedArea = true;
                }
                else if (WebDashboardMinimapAreaResolver.TryGetOutdoorSpaceGroup(room) is VSpaceTileGroup outdoorTileGroup
                    && WebDashboardMinimapDungeonSource.TryBuildFromTileGroup(outdoorTileGroup, out WebDashboardMinimapDungeonGraph outdoorGraph))
                {
                    outdoorArea = BuildAreaFromGraph(
                        outdoorGraph,
                        WebDashboardMinimapAreaResolver.OutdoorAreaId,
                        "Outdoor",
                        "outdoor");
                    WebDashboardMinimapTileRegistry.RegisterGraph(outdoorGraph, outdoorArea.Id);
                    layout.Areas.Add(outdoorArea);
                    addedArea = true;
                }

                VSpaceTileGroup? indoorGroup = WebDashboardMinimapAreaResolver.TryGetIndoorTileGroup(room);

                if (indoorGroup != null
                    && WebDashboardMinimapDungeonSource.TryBuildFromTileGroup(indoorGroup, out WebDashboardMinimapDungeonGraph indoorGraph))
                {
                    addedArea = AddIndoorHeightLayerAreas(layout, indoorGraph) || addedArea;
                    AppendCrossAreaConnections(layout, indoorGraph);
                }
            }

            if (!addedArea && WebDashboardMinimapDungeonSource.TryBuildGraph(gps, out WebDashboardMinimapDungeonGraph fallbackGraph))
            {
                addedArea = AddIndoorHeightLayerAreas(layout, fallbackGraph);
                AppendCrossAreaConnections(layout, fallbackGraph);
            }

            if (!addedArea)
            {
                WebDashboardMinimapTileRegistry.Clear();
                return BuildMarkersOnlyLayout(gps, sceneLabel, "dungeon");
            }

            AppendTeleporterConnectionPoints(layout, gps);
            layout.DefaultAreaId = ResolveDefaultAreaId(layout.Areas);
            MirrorLegacyFields(layout);
            return layout;
        }

        private static bool AddIndoorHeightLayerAreas(
            WebDashboardMinimapLayoutDto layout,
            WebDashboardMinimapDungeonGraph indoorGraph)
        {
            List<List<Tile>> layers = WebDashboardMinimapHeightLayers.ClusterTilesByHeight(indoorGraph.Tiles);
            if (layers.Count == 0)
            {
                return false;
            }

            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                WebDashboardMinimapDungeonGraph layerGraph =
                    WebDashboardMinimapHeightLayers.ExtractSubgraph(indoorGraph, layers[layerIndex]);
                if (layerGraph.Tiles.Count == 0)
                {
                    continue;
                }

                string areaId = WebDashboardMinimapHeightLayers.BuildIndoorLayerAreaId(layerIndex, layers.Count);
                string label = WebDashboardMinimapHeightLayers.BuildIndoorLayerLabel(layerIndex, layers.Count);
                WebDashboardMinimapAreaDto area = BuildAreaFromGraph(layerGraph, areaId, label, "indoor");
                WebDashboardMinimapTileRegistry.RegisterGraph(layerGraph, areaId);
                layout.Areas.Add(area);
            }

            return layout.Areas.Exists(static area => area.Kind == "indoor");
        }

        private readonly struct RawMapTile
        {
            internal RawMapTile(string id, string label, float centerX, float centerZ, float width, float height, bool isMainPath)
            {
                Id = id;
                Label = label;
                CenterX = centerX;
                CenterZ = centerZ;
                Width = width;
                Height = height;
                IsMainPath = isMainPath;
            }

            internal readonly string Id;
            internal readonly string Label;
            internal readonly float CenterX;
            internal readonly float CenterZ;
            internal readonly float Width;
            internal readonly float Height;
            internal readonly bool IsMainPath;
        }

        private static WebDashboardMinimapAreaDto BuildAreaFromGraph(
            WebDashboardMinimapDungeonGraph graph,
            string areaId,
            string label,
            string kind)
        {
            List<RawMapTile> rawTiles = [];
            foreach (Tile tile in graph.Tiles)
            {
                if (tile == null || !graph.TileIds.TryGetValue(tile, out int tileIndex))
                {
                    continue;
                }

                Bounds bounds = tile.Placement?.Bounds ?? tile.Bounds;
                Vector3 center = bounds.center;
                float halfW = Mathf.Max(bounds.extents.x, 0.5f);
                float halfH = Mathf.Max(bounds.extents.z, 0.5f);
                rawTiles.Add(new RawMapTile(
                    $"tile-{tileIndex}",
                    ResolveTileLabel(tile),
                    center.x,
                    center.z,
                    halfW * 2f,
                    halfH * 2f,
                    graph.MainPath.Contains(tile)));
            }

            List<(string fromId, string toId)> connections = [];
            foreach ((int from, int to) in graph.Connections)
            {
                connections.Add(($"tile-{from}", $"tile-{to}"));
            }

            return BuildAreaFromRawTiles(areaId, label, kind, rawTiles, connections);
        }

        private static WebDashboardMinimapAreaDto BuildAreaFromGridGraph(
            WebDashboardMinimapGridGraph graph,
            string areaId,
            string label,
            string kind)
        {
            List<RawMapTile> rawTiles = [];
            foreach (WebDashboardMinimapGridCell cell in graph.Cells)
            {
                rawTiles.Add(new RawMapTile(
                    WebDashboardMinimapGridSource.BuildCellId(cell.Coordinate),
                    "Sector",
                    cell.CenterX,
                    cell.CenterZ,
                    cell.Size,
                    cell.Size,
                    isMainPath: false));
            }

            List<(string fromId, string toId)> connections = [];
            foreach ((int from, int to) in graph.Connections)
            {
                connections.Add((
                    WebDashboardMinimapGridSource.BuildCellId(graph.Cells[from].Coordinate),
                    WebDashboardMinimapGridSource.BuildCellId(graph.Cells[to].Coordinate)));
            }

            return BuildAreaFromRawTiles(areaId, label, kind, rawTiles, connections);
        }

        private static WebDashboardMinimapAreaDto BuildAreaFromRawTiles(
            string areaId,
            string label,
            string kind,
            List<RawMapTile> rawTiles,
            List<(string fromId, string toId)> connections)
        {
            WebDashboardMinimapAreaDto area = new()
            {
                Id = areaId,
                Label = label,
                Kind = kind,
            };

            if (rawTiles.Count == 0)
            {
                return area;
            }

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minZ = float.PositiveInfinity;
            float maxZ = float.NegativeInfinity;
            foreach (RawMapTile tile in rawTiles)
            {
                float halfW = tile.Width * 0.5f;
                float halfH = tile.Height * 0.5f;
                minX = Mathf.Min(minX, tile.CenterX - halfW);
                maxX = Mathf.Max(maxX, tile.CenterX + halfW);
                minZ = Mathf.Min(minZ, tile.CenterZ - halfH);
                maxZ = Mathf.Max(maxZ, tile.CenterZ + halfH);
            }

            float spanX = Mathf.Max(maxX - minX, 1f);
            float spanZ = Mathf.Max(maxZ - minZ, 1f);
            float padX = spanX * BoundsPadding;
            float padZ = spanZ * BoundsPadding;
            minX -= padX;
            maxX += padX;
            minZ -= padZ;
            maxZ += padZ;
            spanX = maxX - minX;
            spanZ = maxZ - minZ;

            area.Bounds = new WebDashboardMinimapBoundsDto
            {
                MinX = minX,
                MinZ = minZ,
                MaxX = maxX,
                MaxZ = maxZ,
            };

            Dictionary<string, WebDashboardMinimapTileDto> tilesById = [];
            foreach (RawMapTile tile in rawTiles)
            {
                WebDashboardMinimapTileDto tileDto = new()
                {
                    Id = tile.Id,
                    Label = tile.Label,
                    X = Normalize(tile.CenterX - (tile.Width * 0.5f), minX, spanX),
                    Z = Normalize(tile.CenterZ - (tile.Height * 0.5f), minZ, spanZ),
                    W = Mathf.Clamp01(tile.Width / spanX),
                    H = Mathf.Clamp01(tile.Height / spanZ),
                    IsMainPath = tile.IsMainPath,
                };
                area.Tiles.Add(tileDto);
                tilesById[tileDto.Id] = tileDto;
            }

            HashSet<string> seenConnections = [];
            foreach ((string fromId, string toId) in connections)
            {
                if (fromId == toId)
                {
                    continue;
                }

                string pairKey = string.CompareOrdinal(fromId, toId) < 0
                    ? fromId + "|" + toId
                    : toId + "|" + fromId;
                if (!seenConnections.Add(pairKey))
                {
                    continue;
                }

                if (!tilesById.TryGetValue(fromId, out WebDashboardMinimapTileDto? fromTile)
                    || !tilesById.TryGetValue(toId, out WebDashboardMinimapTileDto? toTile))
                {
                    continue;
                }

                if (TryComputeConnectionPoint(fromTile, toTile, out float pointX, out float pointZ)
                    && TryComputeConnectionDirection(fromTile, toTile, out float dirX, out float dirZ))
                {
                    area.ConnectionPoints.Add(new WebDashboardMinimapConnectionPointDto
                    {
                        X = pointX,
                        Z = pointZ,
                        DirX = dirX,
                        DirZ = dirZ,
                        FromTileId = fromId,
                        ToTileId = toId,
                        CrossArea = false,
                    });
                }
            }

            return area;
        }

        // Teleporters between indoor and outdoor are not part of the dungeon tile graph,
        // so they are collected from the scene and pinned into their containing area.
        private static void AppendTeleporterConnectionPoints(
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
                        X = Normalize(position.x, area.Bounds.MinX, spanX),
                        Z = Normalize(position.z, area.Bounds.MinZ, spanZ),
                        DirX = forward.x,
                        DirZ = forward.z,
                        TargetAreaId = targetAreaId,
                        CrossArea = true,
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

        private static void AppendCrossAreaConnections(
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
            pointX = Normalize(edgeX, areaBounds.MinX, spanX);
            pointZ = Normalize(edgeZ, areaBounds.MinZ, spanZ);

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

        private static bool TryComputeConnectionDirection(
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

        private static bool TryComputeConnectionPoint(
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

        private static WebDashboardMinimapLayoutDto BuildHiddenLayout(string sceneLabel)
        {
            return new WebDashboardMinimapLayoutDto
            {
                LayoutKind = "none",
                DisplayMode = "hidden",
                SceneLabel = sceneLabel,
            };
        }

        private static WebDashboardMinimapLayoutDto BuildMarkersOnlyLayout(
            GameMainBase? main,
            string sceneLabel,
            string layoutKind)
        {
            WebDashboardMinimapLayoutDto layout = new()
            {
                LayoutKind = layoutKind,
                DisplayMode = "markers-only",
                SceneLabel = sceneLabel,
                Bounds = TryBuildHubBounds(main),
                DefaultAreaId = WebDashboardMinimapAreaResolver.HubAreaId,
            };

            layout.Areas.Add(new WebDashboardMinimapAreaDto
            {
                Id = WebDashboardMinimapAreaResolver.HubAreaId,
                Label = sceneLabel,
                Kind = layoutKind,
                Bounds = layout.Bounds,
            });

            return layout;
        }

        private static WebDashboardMinimapLayoutDto BuildHubLayout(GameMainBase? main, string sceneLabel)
        {
            WebDashboardMinimapBoundsDto bounds = TryBuildHubBounds(main);
            WebDashboardMinimapLayoutDto layout = new()
            {
                LayoutKind = "hub",
                DisplayMode = "markers-only",
                SceneLabel = sceneLabel,
                Bounds = bounds,
                DefaultAreaId = WebDashboardMinimapAreaResolver.HubAreaId,
            };

            layout.Areas.Add(new WebDashboardMinimapAreaDto
            {
                Id = WebDashboardMinimapAreaResolver.HubAreaId,
                Label = sceneLabel,
                Kind = "hub",
                Bounds = bounds,
            });

            return layout;
        }

        private static string ResolveDefaultAreaId(List<WebDashboardMinimapAreaDto> areas)
        {
            foreach (WebDashboardMinimapAreaDto area in areas)
            {
                if (area.Id == WebDashboardMinimapAreaResolver.OutdoorAreaId && area.Tiles.Count > 0)
                {
                    return area.Id;
                }
            }

            WebDashboardMinimapAreaDto? lowestIndoor = null;
            foreach (WebDashboardMinimapAreaDto area in areas)
            {
                if (area.Kind != "indoor" || area.Tiles.Count == 0)
                {
                    continue;
                }

                if (lowestIndoor == null || string.CompareOrdinal(area.Id, lowestIndoor.Id) < 0)
                {
                    lowestIndoor = area;
                }
            }

            if (lowestIndoor != null)
            {
                return lowestIndoor.Id;
            }

            foreach (WebDashboardMinimapAreaDto area in areas)
            {
                if (area.Tiles.Count > 0)
                {
                    return area.Id;
                }
            }

            return areas.Count > 0 ? areas[0].Id : "";
        }

        private static bool HasAnyAreaTiles(WebDashboardMinimapLayoutDto layout)
        {
            foreach (WebDashboardMinimapAreaDto area in layout.Areas)
            {
                if (area.Tiles.Count > 0)
                {
                    return true;
                }
            }

            return layout.Tiles.Count > 0;
        }

        private static void MirrorLegacyFields(WebDashboardMinimapLayoutDto layout)
        {
            layout.Tiles.Clear();
            layout.Connections.Clear();

            WebDashboardMinimapAreaDto? primary = null;
            foreach (WebDashboardMinimapAreaDto area in layout.Areas)
            {
                if (area.Id == layout.DefaultAreaId || primary == null)
                {
                    primary = area;
                }
            }

            if (primary == null)
            {
                return;
            }

            layout.Bounds = primary.Bounds;
            layout.Tiles.AddRange(primary.Tiles);
            foreach (WebDashboardMinimapConnectionPointDto point in primary.ConnectionPoints)
            {
                layout.Connections.Add(new WebDashboardMinimapConnectionDto
                {
                    From = point.FromTileId,
                    To = point.ToTileId,
                });
            }
        }

        private static WebDashboardMinimapBoundsDto TryBuildHubBounds(GameMainBase? main)
        {
            Transform? bgRoot = WebDashboardSceneRoots.TryGetBgRoot(main);
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minZ = float.PositiveInfinity;
            float maxZ = float.NegativeInfinity;

            if (main != null)
            {
                ExpandBoundsFromTransform(bgRoot, ref minX, ref maxX, ref minZ, ref maxZ);
                if (main is MaintenanceScene maintenanceScene)
                {
                    ExpandBoundsFromTransform(
                        WebDashboardSceneRoots.TryGetMaintenanceRoomRoot(maintenanceScene),
                        ref minX,
                        ref maxX,
                        ref minZ,
                        ref maxZ);
                }
            }

            if (float.IsPositiveInfinity(minX))
            {
                return bgRoot != null
                    ? CenteredBounds(bgRoot.position.x, bgRoot.position.z, HubFallbackHalfSpan)
                    : PlaceholderBounds();
            }

            return FinalizeHubBounds(minX, maxX, minZ, maxZ, bgRoot);
        }

        private static WebDashboardMinimapBoundsDto FinalizeHubBounds(
            float minX,
            float maxX,
            float minZ,
            float maxZ,
            Transform? anchor)
        {
            float centerX = anchor != null ? anchor.position.x : (minX + maxX) * 0.5f;
            float centerZ = anchor != null ? anchor.position.z : (minZ + maxZ) * 0.5f;
            float spanX = Mathf.Clamp(maxX - minX, HubMinSpan, HubMaxSpan);
            float spanZ = Mathf.Clamp(maxZ - minZ, HubMinSpan, HubMaxSpan);

            if (maxX - minX > HubMaxSpan || maxZ - minZ > HubMaxSpan)
            {
                minX = centerX - (spanX * 0.5f);
                maxX = centerX + (spanX * 0.5f);
                minZ = centerZ - (spanZ * 0.5f);
                maxZ = centerZ + (spanZ * 0.5f);
            }

            float padX = spanX * BoundsPadding;
            float padZ = spanZ * BoundsPadding;

            return new WebDashboardMinimapBoundsDto
            {
                MinX = minX - padX,
                MinZ = minZ - padZ,
                MaxX = maxX + padX,
                MaxZ = maxZ + padZ,
            };
        }

        private static WebDashboardMinimapBoundsDto CenteredBounds(float centerX, float centerZ, float halfSpan)
        {
            float pad = halfSpan * BoundsPadding;
            return new WebDashboardMinimapBoundsDto
            {
                MinX = centerX - halfSpan - pad,
                MinZ = centerZ - halfSpan - pad,
                MaxX = centerX + halfSpan + pad,
                MaxZ = centerZ + halfSpan + pad,
            };
        }

        private static void ExpandBoundsFromTransform(
            Transform? root,
            ref float minX,
            ref float maxX,
            ref float minZ,
            ref float maxZ)
        {
            if (root == null)
            {
                return;
            }

            bool expanded = false;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Bounds bounds = renderer.bounds;
                minX = Mathf.Min(minX, bounds.min.x);
                maxX = Mathf.Max(maxX, bounds.max.x);
                minZ = Mathf.Min(minZ, bounds.min.z);
                maxZ = Mathf.Max(maxZ, bounds.max.z);
                expanded = true;
            }

            if (!expanded)
            {
                Vector3 position = root.position;
                minX = Mathf.Min(minX, position.x - HubFallbackHalfSpan);
                maxX = Mathf.Max(maxX, position.x + HubFallbackHalfSpan);
                minZ = Mathf.Min(minZ, position.z - HubFallbackHalfSpan);
                maxZ = Mathf.Max(maxZ, position.z + HubFallbackHalfSpan);
            }
        }

        private static WebDashboardMinimapBoundsDto PlaceholderBounds()
        {
            return new WebDashboardMinimapBoundsDto
            {
                MinX = 0f,
                MinZ = 0f,
                MaxX = 1f,
                MaxZ = 1f,
            };
        }

        private static string ResolveDungeonLabel(GamePlayScene gps)
        {
            try
            {
                if (Hub.s == null)
                {
                    return "Dungeon";
                }

                PropertyInfo? datamanProperty = typeof(Hub).GetProperty("dataman", InstanceFlags);
                if (datamanProperty?.GetValue(Hub.s) is not DataManager dataman)
                {
                    return "Dungeon";
                }

                DungeonMasterInfo? dungeonInfo = dataman.ExcelDataManager.GetDungeonInfo(gps.DungeonMasterID);
                if (dungeonInfo == null)
                {
                    return "Dungeon";
                }

                string name = dungeonInfo.ID.ToString();
                int mapId = JoinAnytimeRoomTools.ResolvePickedMapId(null);
                if (mapId == 0 && !dungeonInfo.MapIDs.IsDefaultOrEmpty)
                {
                    mapId = dungeonInfo.MapIDs[0];
                }

                if (mapId != 0)
                {
                    MapMasterInfo? mapInfo = dataman.ExcelDataManager.GetMapInfo(mapId);
                    if (!string.IsNullOrWhiteSpace(mapInfo?.SceneName))
                    {
                        return name + " / " + mapInfo.SceneName;
                    }
                }

                return name;
            }
            catch
            {
                return "Dungeon";
            }
        }

        private static string ResolveSceneLabel(GameMainBase? main)
        {
            return main?.GetType().Name switch
            {
                nameof(DeathMatchScene) => "Death match",
                null => "Session",
                _ => main.GetType().Name,
            };
        }

        private static long ResolveRoomUid(GamePlayScene gps)
        {
            FieldInfo? roomUidField = typeof(GamePlayScene).GetField("RoomUID", InstanceFlags)
                ?? typeof(GamePlayScene).GetField("roomUID", InstanceFlags);
            return roomUidField != null ? Convert.ToInt64(roomUidField.GetValue(gps)) : 0L;
        }

        private static string ResolveTileLabel(Tile tile)
        {
            string? fromPlacement = tile.Placement?.TileSet?.name;
            string? raw = SanitizeRoomName(tile.name)
                ?? SanitizeRoomName(tile.gameObject?.name)
                ?? SanitizeRoomName(fromPlacement);
            return string.IsNullOrWhiteSpace(raw) ? "Room" : raw;
        }

        private static string? SanitizeRoomName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            string trimmed = name.Trim();
            return trimmed.Equals("GameObject", StringComparison.OrdinalIgnoreCase) ? null : trimmed;
        }

        private static float Normalize(float value, float min, float span)
        {
            return span <= 0f ? 0.5f : Mathf.Clamp01((value - min) / span);
        }
    }
}
