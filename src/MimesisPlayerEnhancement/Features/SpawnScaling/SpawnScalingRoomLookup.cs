using System;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using DunGen;
using HarmonyLib;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class SpawnScalingRoomLookup
    {
        private static readonly FieldInfo DungeonSpaceGroupField =
            AccessTools.Field(typeof(DungeonRoom), "_dungeonSpaceGroup")
            ?? throw new InvalidOperationException("DungeonRoom._dungeonSpaceGroup not found");

        private static readonly FieldInfo SpaceGroupField =
            AccessTools.Field(typeof(DungeonRoom), "_spaceGroup")
            ?? throw new InvalidOperationException("DungeonRoom._spaceGroup not found");

        private static readonly FieldInfo TilesField =
            AccessTools.Field(typeof(VSpaceTileGroup), "m_tiles")
            ?? throw new InvalidOperationException("VSpaceTileGroup.m_tiles not found");

        private static readonly ConditionalWeakTable<DungeonRoom, VSpaceTileGroup> TileGroupCache = new();

        internal static string TryGetRoomName(DungeonRoom room, Vector3 position)
        {
            if (room == null)
            {
                return string.Empty;
            }

            if (!TryGetTileGroup(room, out VSpaceTileGroup? tileGroup) || tileGroup == null)
            {
                return string.Empty;
            }

            try
            {
                if (tileGroup is not ISpaceGroup spaceGroup)
                {
                    return string.Empty;
                }

                IVSpace? space = spaceGroup.GetSpace(position);
                return space?.Coordinate is not TileCoordinate tileCoordinate ? string.Empty : TryGetTileName(tileGroup, tileCoordinate.TileID);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool TryGetTileGroup(DungeonRoom room, out VSpaceTileGroup? tileGroup)
        {
            if (TileGroupCache.TryGetValue(room, out tileGroup))
            {
                return tileGroup != null;
            }

            tileGroup = null;

            if (DungeonSpaceGroupField.GetValue(room) is VSpaceTileGroup dungeonTileGroup)
            {
                tileGroup = dungeonTileGroup;
            }
            else if (SpaceGroupField.GetValue(room) is VSpaceTileGroup spaceTileGroup)
            {
                tileGroup = spaceTileGroup;
            }

            if (tileGroup != null)
            {
                TileGroupCache.Add(room, tileGroup);
                return true;
            }

            return false;
        }

        private static string TryGetTileName(VSpaceTileGroup tileGroup, int tileId)
        {
            return tileId <= 0 || TilesField.GetValue(tileGroup) is not IDictionary tiles
                ? string.Empty
                : tiles[tileId] is not Tile tile
                ? string.Empty
                : SanitizeRoomName(tile.name) ?? SanitizeRoomName(tile.gameObject?.name) ?? string.Empty;
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
