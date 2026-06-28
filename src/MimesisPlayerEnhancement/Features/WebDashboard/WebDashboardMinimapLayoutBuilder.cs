using System;
using System.Collections.Generic;
using System.Reflection;
using Bifrost.Cooked;
using DunGen;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMinimapLayoutBuilder
    {
        private const string Feature = "WebDashboard";
        private const float BoundsPadding = 0.05f;

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? DungeonGeneratorField =
            typeof(GamePlayScene).GetField("dungeonGenerator", InstanceFlags);
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
                if (Current.LayoutKind == "dungeon" && Current.Tiles.Count > 0)
                {
                    return;
                }

                if (Current.LayoutKind is "hub" or "none" && runKey.StartsWith("hub:", StringComparison.Ordinal))
                {
                    return;
                }

                if (main is GamePlayScene gps && Current.LayoutKind == "dungeon")
                {
                    Dungeon? dungeon = TryGetCurrentDungeon(gps);
                    if (dungeon?.AllTiles == null || dungeon.AllTiles.Count == 0)
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
                InTramWaitingScene => BuildHubLayout("Tram waiting room", "hub"),
                MaintenanceScene => BuildHubLayout("Maintenance bay", "hub"),
                _ => BuildHubLayout(ResolveSceneLabel(main), "none"),
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
            Dungeon? dungeon = TryGetCurrentDungeon(gps);
            if (dungeon?.AllTiles == null || dungeon.AllTiles.Count == 0)
            {
                return BuildHubLayout(ResolveDungeonLabel(gps), "dungeon");
            }

            try
            {
                return BuildFromDungeon(dungeon, ResolveDungeonLabel(gps));
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Minimap layout extraction failed for {runKey}: {ex.Message}");
                return BuildHubLayout(ResolveDungeonLabel(gps), "dungeon");
            }
        }

        private static Dungeon? TryGetCurrentDungeon(GamePlayScene gps)
        {
            if (DungeonGeneratorField?.GetValue(gps) is DungeonGenerator generator && generator.CurrentDungeon != null)
            {
                return generator.CurrentDungeon;
            }

            MethodInfo? getRuntime = typeof(GamePlayScene).GetMethod("GetRuntimeDungeon", InstanceFlags);
            if (getRuntime?.Invoke(gps, null) is RuntimeDungeon runtime)
            {
                FieldInfo? dungeonField = typeof(RuntimeDungeon).GetField("dungeon", InstanceFlags)
                    ?? typeof(RuntimeDungeon).GetField("_dungeon", InstanceFlags);
                if (dungeonField?.GetValue(runtime) is Dungeon fromRuntime)
                {
                    return fromRuntime;
                }
            }

            return null;
        }

        private static WebDashboardMinimapLayoutDto BuildFromDungeon(Dungeon dungeon, string sceneLabel)
        {
            HashSet<Tile> mainPath = [];
            if (dungeon.MainPathTiles != null)
            {
                foreach (Tile tile in dungeon.MainPathTiles)
                {
                    if (tile != null)
                    {
                        _ = mainPath.Add(tile);
                    }
                }
            }

            Dictionary<Tile, string> tileIds = [];
            List<(Tile tile, float centerX, float centerZ, float width, float height)> rawTiles = [];

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minZ = float.PositiveInfinity;
            float maxZ = float.NegativeInfinity;

            int index = 0;
            foreach (Tile tile in dungeon.AllTiles)
            {
                if (tile == null)
                {
                    continue;
                }

                string tileId = $"tile-{tile.GetInstanceID()}";
                if (tileId == "tile-0")
                {
                    tileId = $"tile-{index}";
                }

                tileIds[tile] = tileId;
                index++;

                Bounds bounds = tile.Bounds;
                Vector3 center = bounds.center;
                float halfW = Mathf.Max(bounds.extents.x, 0.5f);
                float halfH = Mathf.Max(bounds.extents.z, 0.5f);

                rawTiles.Add((tile, center.x, center.z, halfW * 2f, halfH * 2f));
                minX = Mathf.Min(minX, center.x - halfW);
                maxX = Mathf.Max(maxX, center.x + halfW);
                minZ = Mathf.Min(minZ, center.z - halfH);
                maxZ = Mathf.Max(maxZ, center.z + halfH);
            }

            if (rawTiles.Count == 0)
            {
                return BuildHubLayout(sceneLabel, "dungeon");
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

            WebDashboardMinimapLayoutDto layout = new()
            {
                LayoutKind = "dungeon",
                SceneLabel = sceneLabel,
                Bounds = new WebDashboardMinimapBoundsDto
                {
                    MinX = minX,
                    MinZ = minZ,
                    MaxX = maxX,
                    MaxZ = maxZ,
                },
            };

            foreach ((Tile tile, float centerX, float centerZ, float width, float height) in rawTiles)
            {
                layout.Tiles.Add(new WebDashboardMinimapTileDto
                {
                    Id = tileIds[tile],
                    Label = ResolveTileLabel(tile),
                    X = Normalize(centerX - (width * 0.5f), minX, spanX),
                    Z = Normalize(centerZ - (height * 0.5f), minZ, spanZ),
                    W = Mathf.Clamp01(width / spanX),
                    H = Mathf.Clamp01(height / spanZ),
                    IsMainPath = mainPath.Contains(tile)
                        || (tile.Placement?.IsOnMainPath ?? false),
                });
            }

            if (dungeon.Connections != null)
            {
                HashSet<string> seenConnections = [];
                foreach (object connectionObj in dungeon.Connections)
                {
                    if (connectionObj is not DungeonGraphConnection connection)
                    {
                        continue;
                    }

                    Tile? tileA = connection.A?.Tile ?? connection.DoorwayA?.Tile;
                    Tile? tileB = connection.B?.Tile ?? connection.DoorwayB?.Tile;
                    if (tileA == null || tileB == null
                        || !tileIds.TryGetValue(tileA, out string? fromId)
                        || !tileIds.TryGetValue(tileB, out string? toId)
                        || fromId == toId)
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

                    layout.Connections.Add(new WebDashboardMinimapConnectionDto
                    {
                        From = fromId,
                        To = toId,
                    });
                }
            }

            return layout;
        }

        private static WebDashboardMinimapLayoutDto BuildHubLayout(string sceneLabel, string layoutKind)
        {
            return new WebDashboardMinimapLayoutDto
            {
                LayoutKind = layoutKind,
                SceneLabel = sceneLabel,
                Bounds = new WebDashboardMinimapBoundsDto
                {
                    MinX = 0f,
                    MinZ = 0f,
                    MaxX = 1f,
                    MaxZ = 1f,
                },
                Train = TryFindTrainMarker(),
            };
        }

        private static WebDashboardMinimapTrainDto? TryFindTrainMarker()
        {
            try
            {
                Type? tramType = typeof(GamePlayScene).Assembly.GetType("TramConsole")
                    ?? typeof(GamePlayScene).Assembly.GetType("TramStarter");
                if (tramType == null)
                {
                    return null;
                }

                Object[] found = Object.FindObjectsByType(tramType, FindObjectsSortMode.None);
                if (found.Length == 0 || found[0] is not Component component)
                {
                    return null;
                }

                Transform transform = component.transform;
                Vector3 position = transform.position;
                return new WebDashboardMinimapTrainDto
                {
                    X = position.x,
                    Z = position.z,
                    Yaw = transform.eulerAngles.y,
                };
            }
            catch
            {
                return null;
            }
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
                if (!dungeonInfo.MapIDs.IsDefaultOrEmpty)
                {
                    MapMasterInfo? mapInfo = dataman.ExcelDataManager.GetMapInfo(dungeonInfo.MapIDs[0]);
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
