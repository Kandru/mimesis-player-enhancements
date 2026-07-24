using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using DunGen;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.SpawnScaling
{
    internal static class SpawnScalingRoomLookup
    {
        // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L79
        private static readonly FieldInfo DungeonSpaceGroupField =
            AccessTools.Field(typeof(DungeonRoom), "_dungeonSpaceGroup")
            ?? throw new InvalidOperationException("DungeonRoom._dungeonSpaceGroup not found");

        // Optional legacy field; removed in 0.3.1.
        private static readonly FieldInfo? SpaceGroupField =
            AccessTools.Field(typeof(DungeonRoom), "_spaceGroup");

        // game@0.3.1 Assembly-CSharp/VSpaceTileGroup.cs:L14
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
            else if (SpaceGroupField?.GetValue(room) is VSpaceTileGroup spaceTileGroup)
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
            if (tileId <= 0 || TilesField.GetValue(tileGroup) is not IDictionary tiles)
            {
                return string.Empty;
            }

            if (tiles[tileId] is not Tile tile)
            {
                return string.Empty;
            }

            return WebDashboardMinimapTileLabels.ResolveTileLabel(tile);
        }
    }
}
