using System.Reflection;
using Bifrost.Cooked;
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
            float padX = spanX * WebDashboardMinimapMath.BoundsPadding;
            float padZ = spanZ * WebDashboardMinimapMath.BoundsPadding;
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
                    X = WebDashboardMinimapMath.Normalize(tile.CenterX - (tile.Width * 0.5f), minX, spanX),
                    Z = WebDashboardMinimapMath.Normalize(tile.CenterZ - (tile.Height * 0.5f), minZ, spanZ),
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
                    });
                }
            }

            return area;
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

        private static WebDashboardMinimapLayoutDto BuildHubLayout(GameMainBase? main, string sceneLabel)
        {
            WebDashboardMinimapBoundsDto bounds = WebDashboardMinimapHubBounds.TryBuildHubBounds(main);
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

    }
}
