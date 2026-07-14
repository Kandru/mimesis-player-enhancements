using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    /// <summary>
    /// Computes minimap bounds for hub scenes (tram waiting room, maintenance bay)
    /// in BGRoot-local space so rotated trams and player markers stay aligned.
    /// </summary>
    internal static class WebDashboardMinimapHubBounds
    {
        private const float HubMinSpan = 40f;
        private const float HubMaxSpan = 120f;
        private const float HubFallbackHalfSpan = 75f;
        private const float TramMinSpan = 15f;
        private const float TramMaxSpan = 40f;

        private static readonly Vector3[] BoundsCornerFactors =
        [
            new(0f, 0f, 0f),
            new(0f, 0f, 1f),
            new(0f, 1f, 0f),
            new(0f, 1f, 1f),
            new(1f, 0f, 0f),
            new(1f, 0f, 1f),
            new(1f, 1f, 0f),
            new(1f, 1f, 1f),
        ];

        internal static WebDashboardMinimapBoundsDto TryBuildOpenAreaBounds(GameMainBase? main, string kind)
        {
            float minSpan = kind == "tram" ? TramMinSpan : HubMinSpan;
            float maxSpan = kind == "tram" ? TramMaxSpan : HubMaxSpan;
            float fallbackHalfSpan = kind == "tram" ? TramMinSpan * 0.5f : HubFallbackHalfSpan;
            bool includeMaintenanceRoot = kind == "maintenance";

            return TryBuildLocalSceneBounds(main, minSpan, maxSpan, fallbackHalfSpan, includeMaintenanceRoot);
        }

        internal static WebDashboardMinimapBoundsDto TryBuildHubBounds(GameMainBase? main)
        {
            return TryBuildOpenAreaBounds(main, "maintenance");
        }

        internal static bool TryGetTramLocalBounds(
            GameMainBase? main,
            out float centerX,
            out float centerZ,
            out float spanX,
            out float spanZ,
            out float yaw)
        {
            centerX = 0f;
            centerZ = 0f;
            spanX = TramMinSpan;
            spanZ = TramMinSpan;
            yaw = 0f;

            Transform? root = WebDashboardSceneRoots.TryGetBgRoot(main);
            if (root == null)
            {
                return false;
            }

            if (TryGetTramLocalBoundsFromVolume(root, out centerX, out centerZ, out spanX, out spanZ))
            {
                yaw = ResolveTramYaw(root);
                return true;
            }

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minZ = float.PositiveInfinity;
            float maxZ = float.NegativeInfinity;
            ExpandLocalBoundsFromTransform(root, root, ref minX, ref maxX, ref minZ, ref maxZ);
            if (float.IsPositiveInfinity(minX))
            {
                return true;
            }

            centerX = (minX + maxX) * 0.5f;
            centerZ = (minZ + maxZ) * 0.5f;
            spanX = Mathf.Clamp(maxX - minX, TramMinSpan, TramMaxSpan);
            spanZ = Mathf.Clamp(maxZ - minZ, TramMinSpan, TramMaxSpan);
            yaw = ResolveTramYaw(root);
            return true;
        }

        private static bool TryGetTramLocalBoundsFromVolume(
            Transform bgRoot,
            out float centerX,
            out float centerZ,
            out float spanX,
            out float spanZ)
        {
            centerX = 0f;
            centerZ = 0f;
            spanX = TramMinSpan;
            spanZ = TramMinSpan;

            DynamicDataManager? dynamicData = HubGameDataAccess.DynamicData;
            if (dynamicData == null)
            {
                return false;
            }

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minZ = float.PositiveInfinity;
            float maxZ = float.NegativeInfinity;
            bool expanded = false;

            foreach ((MapTrigger _, Bounds bounds) in dynamicData.GetInTramVolume())
            {
                ExpandLocalBoundsFromRendererBounds(bgRoot, bounds, ref minX, ref maxX, ref minZ, ref maxZ);
                expanded = true;
            }

            if (!expanded || float.IsPositiveInfinity(minX))
            {
                return false;
            }

            centerX = (minX + maxX) * 0.5f;
            centerZ = (minZ + maxZ) * 0.5f;
            spanX = Mathf.Clamp(maxX - minX, TramMinSpan, TramMaxSpan);
            spanZ = Mathf.Clamp(maxZ - minZ, TramMinSpan, TramMaxSpan);
            return true;
        }

        private static float ResolveTramYaw(Transform bgRoot)
        {
            // Tram markers live in BGRoot-local space where the tram is axis-aligned.
            return 0f;
        }

        private static WebDashboardMinimapBoundsDto TryBuildLocalSceneBounds(
            GameMainBase? main,
            float minSpan,
            float maxSpan,
            float fallbackHalfSpan,
            bool includeMaintenanceRoot)
        {
            Transform? bgRoot = WebDashboardSceneRoots.TryGetBgRoot(main);
            if (bgRoot == null)
            {
                return PlaceholderBounds();
            }

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minZ = float.PositiveInfinity;
            float maxZ = float.NegativeInfinity;

            ExpandLocalBoundsFromTransform(bgRoot, bgRoot, ref minX, ref maxX, ref minZ, ref maxZ);
            if (main is MaintenanceScene maintenanceScene && includeMaintenanceRoot)
            {
                ExpandLocalBoundsFromTransform(
                    bgRoot,
                    WebDashboardSceneRoots.TryGetMaintenanceRoomRoot(maintenanceScene),
                    ref minX,
                    ref maxX,
                    ref minZ,
                    ref maxZ);
            }

            if (float.IsPositiveInfinity(minX))
            {
                return CenteredBounds(0f, 0f, fallbackHalfSpan);
            }

            return FinalizeLocalBounds(minX, maxX, minZ, maxZ, minSpan, maxSpan);
        }

        private static WebDashboardMinimapBoundsDto FinalizeLocalBounds(
            float minX,
            float maxX,
            float minZ,
            float maxZ,
            float minSpan,
            float maxSpan)
        {
            float centerX = (minX + maxX) * 0.5f;
            float centerZ = (minZ + maxZ) * 0.5f;
            float spanX = Mathf.Clamp(maxX - minX, minSpan, maxSpan);
            float spanZ = Mathf.Clamp(maxZ - minZ, minSpan, maxSpan);

            if (maxX - minX > maxSpan || maxZ - minZ > maxSpan)
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

        private static void ExpandLocalBoundsFromTransform(
            Transform localRoot,
            Transform? source,
            ref float minX,
            ref float maxX,
            ref float minZ,
            ref float maxZ)
        {
            if (source == null)
            {
                return;
            }

            bool expanded = false;
            Renderer[] renderers = source.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                ExpandLocalBoundsFromRendererBounds(localRoot, renderer.bounds, ref minX, ref maxX, ref minZ, ref maxZ);
                expanded = true;
            }

            if (!expanded && source == localRoot)
            {
                minX = Mathf.Min(minX, -HubFallbackHalfSpan);
                maxX = Mathf.Max(maxX, HubFallbackHalfSpan);
                minZ = Mathf.Min(minZ, -HubFallbackHalfSpan);
                maxZ = Mathf.Max(maxZ, HubFallbackHalfSpan);
            }
        }

        private static void ExpandLocalBoundsFromRendererBounds(
            Transform localRoot,
            Bounds worldBounds,
            ref float minX,
            ref float maxX,
            ref float minZ,
            ref float maxZ)
        {
            Vector3 boundsMin = worldBounds.min;
            Vector3 boundsSize = worldBounds.size;
            foreach (Vector3 factor in BoundsCornerFactors)
            {
                Vector3 worldCorner = boundsMin + new Vector3(
                    boundsSize.x * factor.x,
                    boundsSize.y * factor.y,
                    boundsSize.z * factor.z);
                Vector3 local = localRoot.InverseTransformPoint(worldCorner);
                minX = Mathf.Min(minX, local.x);
                maxX = Mathf.Max(maxX, local.x);
                minZ = Mathf.Min(minZ, local.z);
                maxZ = Mathf.Max(maxZ, local.z);
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
