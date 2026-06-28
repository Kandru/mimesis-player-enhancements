using System;
using System.Collections;
using System.Reflection;
using DunGen;
using HarmonyLib;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.SpawnScaling;

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

    internal static string TryGetRoomName(DungeonRoom room, Vector3 position)
    {
        if (room == null)
            return string.Empty;

        if (!TryGetTileGroup(room, out VSpaceTileGroup? tileGroup) || tileGroup == null)
            return string.Empty;

        try
        {
            if (tileGroup is not ISpaceGroup spaceGroup)
                return string.Empty;

            IVSpace? space = spaceGroup.GetSpace(position);
            if (space?.Coordinate is not TileCoordinate tileCoordinate)
                return string.Empty;

            return TryGetTileName(tileGroup, tileCoordinate.TileID);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool TryGetTileGroup(DungeonRoom room, out VSpaceTileGroup? tileGroup)
    {
        tileGroup = null;

        if (DungeonSpaceGroupField.GetValue(room) is VSpaceTileGroup dungeonTileGroup)
        {
            tileGroup = dungeonTileGroup;
            return true;
        }

        if (SpaceGroupField.GetValue(room) is VSpaceTileGroup spaceTileGroup)
        {
            tileGroup = spaceTileGroup;
            return true;
        }

        return false;
    }

    private static string TryGetTileName(VSpaceTileGroup tileGroup, int tileId)
    {
        if (tileId <= 0 || TilesField.GetValue(tileGroup) is not IDictionary tiles)
            return string.Empty;

        if (tiles[tileId] is not Tile tile)
            return string.Empty;

        return SanitizeRoomName(tile.name) ?? SanitizeRoomName(tile.gameObject?.name) ?? string.Empty;
    }

    private static string? SanitizeRoomName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        string trimmed = name.Trim();
        if (trimmed.Equals("GameObject", System.StringComparison.OrdinalIgnoreCase))
            return null;

        return trimmed;
    }
}
