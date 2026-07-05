using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    /// <summary>
    /// Computes minimap world bounds for hub scenes (tram waiting room, maintenance bay)
    /// from scene renderers, with fallbacks when no geometry is available.
    /// </summary>
    internal static class WebDashboardMinimapHubBounds
    {
        private const float HubMinSpan = 40f;
        private const float HubMaxSpan = 120f;
        private const float HubFallbackHalfSpan = 75f;

        internal static WebDashboardMinimapBoundsDto TryBuildHubBounds(GameMainBase? main)
        {
            Transform? bgRoot = WebDashboardSceneRoots.TryGetBgRoot(main);
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
                        WebDashboardSceneRoots.TryGetMaintenanceRoomRoot(maintenanceScene),
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

            float padX = spanX * WebDashboardMinimapMath.BoundsPadding;
            float padZ = spanZ * WebDashboardMinimapMath.BoundsPadding;

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
            float pad = halfSpan * WebDashboardMinimapMath.BoundsPadding;
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
    }
}
