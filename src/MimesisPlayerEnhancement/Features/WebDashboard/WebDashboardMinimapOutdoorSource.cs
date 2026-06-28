using System.Reflection;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMinimapOutdoorSource
    {
        private const float BoundsPadding = 0.05f;

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? OutdoorColliderField =
            typeof(GamePlayScene).GetField("outdoorCollider", InstanceFlags);

        internal static bool TryGetOutdoorWorldBounds(
            GamePlayScene gps,
            out WebDashboardMinimapBoundsDto bounds)
        {
            bounds = new WebDashboardMinimapBoundsDto();
            if (TryGetOutdoorCollider(gps) is not Component collider)
            {
                return false;
            }

            return TryReadWorldBounds(collider, out bounds);
        }

        internal static bool IsPositionOutdoor(GamePlayScene gps, Vector3 position)
        {
            if (TryGetOutdoorCollider(gps) is not Component collider)
            {
                return false;
            }

            if (collider.GetType().GetProperty("bounds")?.GetValue(collider) is not Bounds world)
            {
                return false;
            }

            return world.Contains(position);
        }

        internal static WebDashboardMinimapAreaDto? TryBuildOutdoorArea(GamePlayScene gps)
        {
            if (!TryGetOutdoorWorldBounds(gps, out WebDashboardMinimapBoundsDto bounds))
            {
                return null;
            }

            WebDashboardMinimapAreaDto area = new()
            {
                Id = WebDashboardMinimapAreaResolver.OutdoorAreaId,
                Label = "Outdoor",
                Kind = "outdoor",
                Bounds = bounds,
            };

            area.Tiles.Add(new WebDashboardMinimapTileDto
            {
                Id = "outdoor-zone",
                Label = "Outdoor",
                X = 0f,
                Z = 0f,
                W = 1f,
                H = 1f,
                IsMainPath = true,
            });

            return area;
        }

        private static Component? TryGetOutdoorCollider(GamePlayScene gps)
        {
            return OutdoorColliderField?.GetValue(gps) as Component;
        }

        private static bool TryReadWorldBounds(Component collider, out WebDashboardMinimapBoundsDto bounds)
        {
            bounds = new WebDashboardMinimapBoundsDto();
            if (collider.GetType().GetProperty("bounds")?.GetValue(collider) is not Bounds world)
            {
                return false;
            }

            if (world.size.x <= 0.01f || world.size.z <= 0.01f)
            {
                return false;
            }

            float spanX = world.size.x;
            float spanZ = world.size.z;
            float padX = spanX * BoundsPadding;
            float padZ = spanZ * BoundsPadding;

            bounds = new WebDashboardMinimapBoundsDto
            {
                MinX = world.min.x - padX,
                MinZ = world.min.z - padZ,
                MaxX = world.max.x + padX,
                MaxZ = world.max.z + padZ,
            };
            return true;
        }
    }
}
