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

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? DungeonGeneratorField =
            typeof(GamePlayScene).GetField("dungeonGenerator", InstanceFlags);
        private static readonly FieldInfo? BgRootField =
            typeof(GameMainBase).GetField("BGRoot", InstanceFlags);
        private static readonly FieldInfo? MaintenanceRoomRootField =
            typeof(MaintenanceScene).GetField("maintenanceRoomRoot", InstanceFlags);
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

                if (Current.LayoutKind is "hub" or "hub-waiting" or "none" && runKey.StartsWith("hub:", StringComparison.Ordinal))
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
                InTramWaitingScene => BuildWaitingRoomLayout(main),
                MaintenanceScene => BuildHubLayout(main, "Maintenance bay", "hub"),
                _ => BuildHubLayout(main, ResolveSceneLabel(main), "none"),
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
                return BuildHubLayout(gps, ResolveDungeonLabel(gps), "dungeon");
            }

            try
            {
                return BuildFromDungeon(dungeon, ResolveDungeonLabel(gps));
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Minimap layout extraction failed for {runKey}: {ex.Message}");
                return BuildHubLayout(gps, ResolveDungeonLabel(gps), "dungeon");
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

                Bounds bounds = tile.Placement?.Bounds ?? tile.Bounds;
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
                return BuildHubLayout(null, sceneLabel, "dungeon");
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

        private static WebDashboardMinimapLayoutDto BuildWaitingRoomLayout(GameMainBase? main)
        {
            return new WebDashboardMinimapLayoutDto
            {
                LayoutKind = "hub-waiting",
                SceneLabel = "Tram waiting room",
                Bounds = WebDashboardMinimapTramSpace.BuildWaitingRoomBounds(main),
            };
        }

        private static WebDashboardMinimapLayoutDto BuildHubLayout(GameMainBase? main, string sceneLabel, string layoutKind)
        {
            return new WebDashboardMinimapLayoutDto
            {
                LayoutKind = layoutKind,
                SceneLabel = sceneLabel,
                Bounds = TryBuildHubBounds(main),
            };
        }

        private static WebDashboardMinimapBoundsDto TryBuildHubBounds(GameMainBase? main)
        {
            Transform? bgRoot = main != null ? BgRootField?.GetValue(main) as Transform : null;
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
                        MaintenanceRoomRootField?.GetValue(maintenanceScene) as Transform,
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
