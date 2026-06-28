using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DunGen;
using HarmonyLib;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal sealed class WebDashboardMinimapDungeonGraph
    {
        internal List<Tile> Tiles = [];
        internal Dictionary<Tile, int> TileIds = [];
        internal List<(int From, int To)> Connections = [];
        internal HashSet<Tile> MainPath = [];
    }

    internal static class WebDashboardMinimapDungeonSource
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? RuntimeDungeonField =
            typeof(GamePlayScene).GetField("runtimeDungeon", InstanceFlags);

        private static readonly FieldInfo? DungeonGeneratorField =
            typeof(GamePlayScene).GetField("dungeonGenerator", InstanceFlags);

        private static readonly FieldInfo? DungeonSpaceGroupField =
            AccessTools.Field(typeof(DungeonRoom), "_dungeonSpaceGroup");

        private static readonly FieldInfo? SpaceGroupField =
            AccessTools.Field(typeof(DungeonRoom), "_spaceGroup");

        private static readonly FieldInfo? VSpaceTilesField =
            AccessTools.Field(typeof(VSpaceTileGroup), "m_tiles");

        private static readonly FieldInfo? VSpaceAdjacencyField =
            AccessTools.Field(typeof(VSpaceTileGroup), "m_adjacency");

        private static readonly MethodInfo? GetAllIdToTileMethod =
            typeof(RuntimeDungeon).GetMethod("GetAllIDToTile", InstanceFlags);

        private static readonly MethodInfo? GetAllAdjacencyMethod =
            typeof(RuntimeDungeon).GetMethod("GetAllAdjacency", InstanceFlags);

        internal static bool TryBuildGraph(GamePlayScene gps, out WebDashboardMinimapDungeonGraph graph)
        {
            graph = new WebDashboardMinimapDungeonGraph();

            if (TryAddFromRuntimeDungeon(gps, graph)
                || TryAddFromDungeonRoom(graph)
                || TryAddFromLegacyGenerator(gps, graph))
            {
                return graph.Tiles.Count > 0;
            }

            return false;
        }

        internal static bool TryBuildFromTileGroup(VSpaceTileGroup tileGroup, out WebDashboardMinimapDungeonGraph graph)
        {
            graph = new WebDashboardMinimapDungeonGraph();
            if (VSpaceTilesField?.GetValue(tileGroup) is not IDictionary tilesDictionary)
            {
                return false;
            }

            graph.Tiles.Clear();
            graph.TileIds.Clear();
            graph.Connections.Clear();
            graph.MainPath.Clear();

            foreach (DictionaryEntry entry in tilesDictionary)
            {
                if (entry.Value is not Tile tile)
                {
                    continue;
                }

                int tileId = Convert.ToInt32(entry.Key);
                graph.Tiles.Add(tile);
                graph.TileIds[tile] = tileId;
                if (tile.Placement?.IsOnMainPath ?? false)
                {
                    _ = graph.MainPath.Add(tile);
                }
            }

            if (VSpaceAdjacencyField?.GetValue(tileGroup) is IDictionary adjacencyDictionary)
            {
                AddVSpaceAdjacency(graph, adjacencyDictionary);
            }

            return graph.Tiles.Count > 0;
        }

        internal static bool HasTiles(GamePlayScene gps)
        {
            return TryBuildGraph(gps, out WebDashboardMinimapDungeonGraph graph) && graph.Tiles.Count > 0;
        }

        private static bool TryAddFromRuntimeDungeon(GamePlayScene gps, WebDashboardMinimapDungeonGraph graph)
        {
            if (RuntimeDungeonField?.GetValue(gps) is not RuntimeDungeon runtime)
            {
                return false;
            }

            if (GetAllIdToTileMethod?.Invoke(runtime, null) is not Tile[] idToTile || idToTile.Length == 0)
            {
                return TryAddFromDungeonObject(runtime.Generator?.CurrentDungeon, graph);
            }

            graph.Tiles.Clear();
            graph.TileIds.Clear();
            graph.Connections.Clear();
            graph.MainPath.Clear();

            for (int index = 0; index < idToTile.Length; index++)
            {
                Tile? tile = idToTile[index];
                if (tile == null)
                {
                    continue;
                }

                graph.Tiles.Add(tile);
                graph.TileIds[tile] = index;
                if (tile.Placement?.IsOnMainPath ?? false)
                {
                    _ = graph.MainPath.Add(tile);
                }
            }

            if (GetAllAdjacencyMethod?.Invoke(runtime, null) is IList adjacencyList)
            {
                AddRuntimeAdjacency(graph, adjacencyList);
            }

            return graph.Tiles.Count > 0;
        }

        private static bool TryAddFromLegacyGenerator(GamePlayScene gps, WebDashboardMinimapDungeonGraph graph)
        {
            if (DungeonGeneratorField?.GetValue(gps) is not DungeonGenerator generator)
            {
                return false;
            }

            return TryAddFromDungeonObject(generator.CurrentDungeon, graph);
        }

        private static bool TryAddFromDungeonRoom(WebDashboardMinimapDungeonGraph graph)
        {
            if (JoinAnytimeRoomTools.GetActiveDungeonRoom() is not DungeonRoom room)
            {
                return false;
            }

            VSpaceTileGroup? tileGroup = null;
            if (DungeonSpaceGroupField?.GetValue(room) is VSpaceTileGroup dungeonGroup)
            {
                tileGroup = dungeonGroup;
            }
            else if (SpaceGroupField?.GetValue(room) is VSpaceTileGroup spaceGroup)
            {
                tileGroup = spaceGroup;
            }

            if (tileGroup == null || VSpaceTilesField?.GetValue(tileGroup) is not IDictionary tilesDictionary)
            {
                return false;
            }

            graph.Tiles.Clear();
            graph.TileIds.Clear();
            graph.Connections.Clear();
            graph.MainPath.Clear();

            foreach (DictionaryEntry entry in tilesDictionary)
            {
                if (entry.Value is not Tile tile)
                {
                    continue;
                }

                int tileId = Convert.ToInt32(entry.Key);
                graph.Tiles.Add(tile);
                graph.TileIds[tile] = tileId;
                if (tile.Placement?.IsOnMainPath ?? false)
                {
                    _ = graph.MainPath.Add(tile);
                }
            }

            if (VSpaceAdjacencyField?.GetValue(tileGroup) is IDictionary adjacencyDictionary)
            {
                AddVSpaceAdjacency(graph, adjacencyDictionary);
            }

            return graph.Tiles.Count > 0;
        }

        private static bool TryAddFromDungeonObject(Dungeon? dungeon, WebDashboardMinimapDungeonGraph graph)
        {
            if (dungeon?.AllTiles == null || dungeon.AllTiles.Count == 0)
            {
                return false;
            }

            graph.Tiles.Clear();
            graph.TileIds.Clear();
            graph.Connections.Clear();
            graph.MainPath.Clear();

            int index = 0;
            foreach (Tile tile in dungeon.AllTiles)
            {
                if (tile == null)
                {
                    continue;
                }

                graph.Tiles.Add(tile);
                graph.TileIds[tile] = index++;
                if (tile.Placement?.IsOnMainPath ?? false)
                {
                    _ = graph.MainPath.Add(tile);
                }
            }

            if (dungeon.MainPathTiles != null)
            {
                foreach (Tile tile in dungeon.MainPathTiles)
                {
                    if (tile != null)
                    {
                        _ = graph.MainPath.Add(tile);
                    }
                }
            }

            if (dungeon.Connections != null)
            {
                AddDungeonConnections(dungeon, graph);
            }

            return graph.Tiles.Count > 0;
        }

        private static void AddRuntimeAdjacency(WebDashboardMinimapDungeonGraph graph, IList adjacencyList)
        {
            for (int fromId = 0; fromId < adjacencyList.Count; fromId++)
            {
                if (adjacencyList[fromId] is not IEnumerable neighbors)
                {
                    continue;
                }

                foreach (object neighbor in neighbors)
                {
                    int toId = Convert.ToInt32(neighbor);
                    AddConnectionPair(graph, fromId, toId);
                }
            }
        }

        private static void AddVSpaceAdjacency(WebDashboardMinimapDungeonGraph graph, IDictionary adjacencyDictionary)
        {
            foreach (DictionaryEntry entry in adjacencyDictionary)
            {
                int fromId = Convert.ToInt32(entry.Key);
                if (entry.Value is not IEnumerable neighbors)
                {
                    continue;
                }

                foreach (object neighbor in neighbors)
                {
                    int toId = Convert.ToInt32(neighbor);
                    AddConnectionPair(graph, fromId, toId);
                }
            }
        }

        private static void AddDungeonConnections(Dungeon dungeon, WebDashboardMinimapDungeonGraph graph)
        {
            Dictionary<Tile, int> reverseLookup = graph.TileIds;

            foreach (object connectionObj in dungeon.Connections)
            {
                if (connectionObj is not DungeonGraphConnection connection)
                {
                    continue;
                }

                Tile? tileA = connection.A?.Tile ?? connection.DoorwayA?.Tile;
                Tile? tileB = connection.B?.Tile ?? connection.DoorwayB?.Tile;
                if (tileA == null || tileB == null
                    || !reverseLookup.TryGetValue(tileA, out int fromId)
                    || !reverseLookup.TryGetValue(tileB, out int toId)
                    || fromId == toId)
                {
                    continue;
                }

                AddConnectionPair(graph, fromId, toId);
            }
        }

        private static void AddConnectionPair(WebDashboardMinimapDungeonGraph graph, int fromId, int toId)
        {
            if (fromId == toId)
            {
                return;
            }

            graph.Connections.Add(fromId <= toId ? (fromId, toId) : (toId, fromId));
        }
    }
}
