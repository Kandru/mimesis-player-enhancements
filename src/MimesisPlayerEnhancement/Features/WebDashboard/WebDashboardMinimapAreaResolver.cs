using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMinimapAreaResolver
    {
        internal const string OutdoorAreaId = "outdoor";
        internal const string IndoorAreaId = "indoor";
        internal const string HubAreaId = "hub";

        private static readonly FieldInfo? DungeonSpaceGroupField =
            AccessTools.Field(typeof(DungeonRoom), "_dungeonSpaceGroup");

        private static readonly FieldInfo? SpaceGroupField =
            AccessTools.Field(typeof(DungeonRoom), "_spaceGroup");

        internal static string? ResolvePlayerAreaId(GamePlayScene gps, DungeonRoom room, Vector3 position)
        {
            if (WebDashboardMinimapOutdoorSource.IsPositionOutdoor(gps, position))
            {
                return OutdoorAreaId;
            }

            return ResolveIndoorAreaId(room, position);
        }

        internal static string? ResolveIndoorAreaId(DungeonRoom room, Vector3 position)
        {
            if (TryResolveAreaFromSpaceGroup(position, TryGetIndoorTileGroup(room), out string? indoorArea))
            {
                return indoorArea;
            }

            return TryResolveInSpaceGroup(TryGetIndoorTileGroup(room), position) ? IndoorAreaId : null;
        }

        internal static bool IsIndoorAreaId(string areaId)
        {
            return areaId == IndoorAreaId
                || areaId.StartsWith(IndoorAreaId + "-", System.StringComparison.Ordinal);
        }

        internal static string? ResolvePlayerArea(DungeonRoom? room, Vector3 position)
        {
            if (room == null)
            {
                return null;
            }

            return ResolveIndoorAreaId(room, position);
        }

        private static bool TryResolveAreaFromSpaceGroup(
            Vector3 position,
            object? spaceGroupObj,
            out string? areaId)
        {
            areaId = null;
            if (spaceGroupObj is not ISpaceGroup spaceGroup)
            {
                return false;
            }

            try
            {
                IVSpace? space = spaceGroup.GetSpace(position);
                if (space?.Coordinate is TileCoordinate tileCoordinate)
                {
                    areaId = WebDashboardMinimapTileRegistry.TryGetAreaId(tileCoordinate.TileID);
                    return !string.IsNullOrWhiteSpace(areaId);
                }

                if (space?.Coordinate is GridCoordinate gridCoordinate)
                {
                    areaId = WebDashboardMinimapTileRegistry.TryGetGridAreaId(gridCoordinate.X, gridCoordinate.Y);
                    return !string.IsNullOrWhiteSpace(areaId);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        internal static bool ShouldHideMap(GameMainBase? main)
        {
            return main is InTramWaitingScene or MaintenanceScene or DeathMatchScene;
        }

        internal static ISpaceGroup? TryGetOutdoorSpaceGroup(DungeonRoom room)
        {
            return SpaceGroupField?.GetValue(room) as ISpaceGroup;
        }

        internal static VSpaceTileGroup? TryGetOutdoorTileGroup(DungeonRoom room)
        {
            return SpaceGroupField?.GetValue(room) as VSpaceTileGroup;
        }

        internal static VSpaceGridGroup? TryGetOutdoorGridGroup(DungeonRoom room)
        {
            return SpaceGroupField?.GetValue(room) as VSpaceGridGroup;
        }

        internal static VSpaceTileGroup? TryGetIndoorTileGroup(DungeonRoom room)
        {
            return DungeonSpaceGroupField?.GetValue(room) as VSpaceTileGroup;
        }

        private static bool TryResolveInSpaceGroup(object? spaceGroupObj, Vector3 position)
        {
            if (spaceGroupObj is not ISpaceGroup spaceGroup)
            {
                return false;
            }

            try
            {
                return spaceGroup.GetSpace(position) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
