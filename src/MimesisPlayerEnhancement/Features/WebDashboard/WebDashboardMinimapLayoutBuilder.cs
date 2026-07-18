using System.Reflection;
using DunGen;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMinimapLayoutBuilder
    {
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
            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            GameMainBase? main = pdata?.main;
            string runKey = BuildRunKey(main);

            if (!_rebuildRequested && runKey == _cachedRunKey)
            {
                if (Current.DisplayMode == "map" && HasAnyAreaTiles(Current))
                {
                    return;
                }

                if ((Current.DisplayMode is "hidden" or "markers-only" or "open")
                    && runKey.StartsWith("hub:", StringComparison.Ordinal))
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
                InTramWaitingScene => BuildOpenAreaLayout(main, "Tram waiting room", "tram"),
                MaintenanceScene => BuildOpenAreaLayout(main, "Maintenance bay", "maintenance"),
                DeathMatchScene => BuildHiddenLayout("Death match"),
                _ => WebDashboardMinimapAreaResolver.ShouldHideMap(main)
                    ? BuildHiddenLayout(ResolveSceneLabel(main))
                    : BuildOpenAreaLayout(main, ResolveSceneLabel(main), "hub"),
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
                        WebDashboardMinimapTileLabels.ResolveLabel("Outdoor"),
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
                        WebDashboardMinimapTileLabels.ResolveLabel("Outdoor"),
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
                    WebDashboardMinimapConnectionBuilder.AppendCrossAreaConnections(layout, indoorGraph);
                }
            }

            if (!addedArea && WebDashboardMinimapDungeonSource.TryBuildGraph(gps, out WebDashboardMinimapDungeonGraph fallbackGraph))
            {
                addedArea = AddIndoorHeightLayerAreas(layout, fallbackGraph);
                WebDashboardMinimapConnectionBuilder.AppendCrossAreaConnections(layout, fallbackGraph);
            }

            if (!addedArea)
            {
                WebDashboardMinimapTileRegistry.Clear();
                return BuildMarkersOnlyLayout(gps, sceneLabel, "dungeon");
            }

            WebDashboardMinimapConnectionBuilder.AppendTeleporterConnectionPoints(layout, gps);
            layout.DefaultAreaId = ResolveDefaultAreaId(layout.Areas);
            MirrorLegacyFields(layout);
            return layout;
        }

        private static bool AddIndoorHeightLayerAreas(
            WebDashboardMinimapLayoutDto layout,
            WebDashboardMinimapDungeonGraph indoorGraph)
        {
            List<WebDashboardMinimapHeightLayers.HeightLayer> layers =
                WebDashboardMinimapHeightLayers.ClusterTilesByHeight(indoorGraph.Tiles);
            if (layers.Count == 0)
            {
                return false;
            }

            WebDashboardMinimapFloorRegistry.Clear();
            Dictionary<int, int> tileFloorById = [];
            Dictionary<int, List<int>> tileFloorSpanById = [];
            List<RawMapTile> sharedRawTiles = CollectRawTilesFromGraph(indoorGraph, 0);
            WorldBounds? sharedBounds = sharedRawTiles.Count > 0
                ? ComputeWorldBounds(sharedRawTiles)
                : null;

            foreach (Tile tile in indoorGraph.Tiles)
            {
                if (tile == null || !indoorGraph.TileIds.TryGetValue(tile, out int tileId))
                {
                    continue;
                }

                Bounds bounds = tile.Placement?.Bounds ?? tile.Bounds;
                List<int> span = WebDashboardMinimapHeightLayers.ResolveFloorSpan(bounds.min.y, bounds.max.y, layers);
                tileFloorSpanById[tileId] = span;
            }

            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                WebDashboardMinimapHeightLayers.HeightLayer layer = layers[layerIndex];
                WebDashboardMinimapDungeonGraph layerGraph =
                    WebDashboardMinimapHeightLayers.ExtractSubgraph(indoorGraph, layer.Tiles);
                if (layerGraph.Tiles.Count == 0)
                {
                    continue;
                }

                string areaId = WebDashboardMinimapHeightLayers.BuildIndoorLayerAreaId(layerIndex, layers.Count);
                string label = WebDashboardMinimapHeightLayers.BuildIndoorLayerLabel(layerIndex, layers.Count);
                WebDashboardMinimapAreaDto area = BuildAreaFromGraph(
                    layerGraph,
                    areaId,
                    label,
                    "indoor",
                    layerIndex,
                    sharedBounds);
                WebDashboardMinimapFloorRegistry.RegisterLayer(layerIndex, areaId, layer.MinY, layer.MaxY);

                foreach (KeyValuePair<Tile, int> entry in layerGraph.TileIds)
                {
                    tileFloorById[entry.Value] = layerIndex;
                    if (!tileFloorSpanById.TryGetValue(entry.Value, out List<int>? span))
                    {
                        continue;
                    }

                    string tileId = $"tile-{entry.Value}";
                    foreach (WebDashboardMinimapTileDto tileDto in area.Tiles)
                    {
                        if (tileDto.Id != tileId)
                        {
                            continue;
                        }

                        tileDto.FloorSpan = [.. span];
                        tileDto.MultiFloor = span.Count > 1;
                        break;
                    }
                }

                WebDashboardMinimapTileRegistry.RegisterGraph(layerGraph, areaId);
                layout.Areas.Add(area);
            }

            WebDashboardMinimapDoorwayBuilder.AppendCrossFloorConnections(layout, indoorGraph, tileFloorById);
            return layout.Areas.Exists(static area => area.Kind == "indoor");
        }

        private readonly struct RawMapTile
        {
            internal RawMapTile(
                string id,
                string label,
                float centerX,
                float centerZ,
                float width,
                float height,
                bool isMainPath,
                float centerY = 0f,
                int floorIndex = 0)
            {
                Id = id;
                Label = label;
                CenterX = centerX;
                CenterZ = centerZ;
                Width = width;
                Height = height;
                IsMainPath = isMainPath;
                CenterY = centerY;
                FloorIndex = floorIndex;
            }

            internal readonly string Id;
            internal readonly string Label;
            internal readonly float CenterX;
            internal readonly float CenterZ;
            internal readonly float Width;
            internal readonly float Height;
            internal readonly bool IsMainPath;
            internal readonly float CenterY;
            internal readonly int FloorIndex;
        }

        private readonly struct WorldBounds
        {
            internal WorldBounds(float minX, float maxX, float minZ, float maxZ)
            {
                MinX = minX;
                MaxX = maxX;
                MinZ = minZ;
                MaxZ = maxZ;
            }

            internal readonly float MinX;
            internal readonly float MaxX;
            internal readonly float MinZ;
            internal readonly float MaxZ;
        }

        private static List<RawMapTile> CollectRawTilesFromGraph(
            WebDashboardMinimapDungeonGraph graph,
            int floorIndex)
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
                    WebDashboardMinimapTileLabels.ResolveTileLabel(tile),
                    center.x,
                    center.z,
                    halfW * 2f,
                    halfH * 2f,
                    graph.MainPath.Contains(tile),
                    center.y,
                    floorIndex));
            }

            return rawTiles;
        }

        private static WorldBounds ComputeWorldBounds(List<RawMapTile> rawTiles)
        {
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
            float padX = spanX * WebDashboardMinimapMath.BoundsPadding;
            float padZ = spanZ * WebDashboardMinimapMath.BoundsPadding;
            return new WorldBounds(minX - padX, maxX + padX, minZ - padZ, maxZ + padZ);
        }

        private static WebDashboardMinimapAreaDto BuildAreaFromGraph(
            WebDashboardMinimapDungeonGraph graph,
            string areaId,
            string label,
            string kind,
            int floorIndex = 0,
            WorldBounds? sharedBounds = null)
        {
            List<RawMapTile> rawTiles = CollectRawTilesFromGraph(graph, floorIndex);
            List<(string fromId, string toId)> connections = [];
            foreach ((int from, int to) in graph.Connections)
            {
                connections.Add(($"tile-{from}", $"tile-{to}"));
            }

            WebDashboardMinimapAreaDto area = BuildAreaFromRawTiles(
                areaId,
                label,
                kind,
                rawTiles,
                connections,
                includeEdgeConnections: false,
                sharedBounds);
            Dictionary<string, WebDashboardMinimapTileDto> tilesById = [];
            foreach (WebDashboardMinimapTileDto tileDto in area.Tiles)
            {
                tilesById[tileDto.Id] = tileDto;
            }

            area.ConnectionPoints.Clear();
            WebDashboardMinimapDoorwayBuilder.AppendDoorwayConnections(area, graph, tilesById, area.Bounds);
            return area;
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
                    WebDashboardMinimapTileLabels.ResolveLabel("Sector"),
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
            List<(string fromId, string toId)> connections,
            bool includeEdgeConnections = true,
            WorldBounds? sharedBounds = null)
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

            float minX;
            float maxX;
            float minZ;
            float maxZ;
            if (sharedBounds.HasValue)
            {
                minX = sharedBounds.Value.MinX;
                maxX = sharedBounds.Value.MaxX;
                minZ = sharedBounds.Value.MinZ;
                maxZ = sharedBounds.Value.MaxZ;
            }
            else
            {
                minX = float.PositiveInfinity;
                maxX = float.NegativeInfinity;
                minZ = float.PositiveInfinity;
                maxZ = float.NegativeInfinity;
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
                float padX = spanX * WebDashboardMinimapMath.BoundsPadding;
                float padZ = spanZ * WebDashboardMinimapMath.BoundsPadding;
                minX -= padX;
                maxX += padX;
                minZ -= padZ;
                maxZ += padZ;
            }

            float spanXNorm = maxX - minX;
            float spanZNorm = maxZ - minZ;

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
                    X = WebDashboardMinimapMath.Normalize(tile.CenterX - (tile.Width * 0.5f), minX, spanXNorm),
                    Z = WebDashboardMinimapMath.Normalize(tile.CenterZ - (tile.Height * 0.5f), minZ, spanZNorm),
                    W = Mathf.Clamp01(tile.Width / spanXNorm),
                    H = Mathf.Clamp01(tile.Height / spanZNorm),
                    IsMainPath = tile.IsMainPath,
                    CenterY = tile.CenterY,
                    FloorIndex = tile.FloorIndex,
                };
                area.Tiles.Add(tileDto);
                tilesById[tileDto.Id] = tileDto;
            }

            HashSet<string> seenConnections = [];
            if (includeEdgeConnections)
            {
                // Doorway connections are added by BuildAreaFromGraph when a dungeon graph is available.
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

                    if (WebDashboardMinimapConnectionBuilder.TryComputeConnectionPoint(fromTile, toTile, out float pointX, out float pointZ)
                        && WebDashboardMinimapConnectionBuilder.TryComputeConnectionDirection(fromTile, toTile, out float dirX, out float dirZ))
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
                            Width = DoorOpeningMetersNormalized(area.Bounds),
                        });
                    }
                }
            }

            return area;
        }

        private const float DoorOpeningMeters = 2f;

        private static float DoorOpeningMetersNormalized(WebDashboardMinimapBoundsDto bounds)
        {
            float spanX = Mathf.Max(bounds.MaxX - bounds.MinX, 1f);
            float spanZ = Mathf.Max(bounds.MaxZ - bounds.MinZ, 1f);
            return DoorOpeningMeters / ((spanX + spanZ) * 0.5f);
        }

        // Teleporters between indoor and outdoor are not part of the dungeon tile graph,
        // so they are collected from the scene and pinned into their containing area.
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
                Bounds = WebDashboardMinimapHubBounds.TryBuildHubBounds(main),
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

        private static WebDashboardMinimapLayoutDto BuildOpenAreaLayout(
            GameMainBase? main,
            string sceneLabel,
            string kind)
        {
            WebDashboardMinimapBoundsDto bounds = WebDashboardMinimapHubBounds.TryBuildOpenAreaBounds(main, kind);

            WebDashboardMinimapLayoutDto layout = new()
            {
                LayoutKind = kind,
                DisplayMode = "open",
                SceneLabel = sceneLabel,
                Bounds = bounds,
                DefaultAreaId = WebDashboardMinimapAreaResolver.HubAreaId,
            };

            layout.Areas.Add(new WebDashboardMinimapAreaDto
            {
                Id = WebDashboardMinimapAreaResolver.HubAreaId,
                Label = sceneLabel,
                Kind = kind,
                Borderless = true,
                Bounds = bounds,
            });

            layout.PointsOfInterest = WebDashboardMinimapPoiCollector.CollectForLayout(main, bounds);

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

        private static string ResolveDungeonLabel(GamePlayScene gps)
        {
            try
            {
                if (HubGameDataAccess.Excel is not ExcelDataManager excel)
                {
                    return "Dungeon";
                }

                DungeonMasterInfo? dungeonInfo = excel.GetDungeonInfo(gps.DungeonMasterID);
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
                    MapMasterInfo? mapInfo = excel.GetMapInfo(mapId);
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

    }
}
