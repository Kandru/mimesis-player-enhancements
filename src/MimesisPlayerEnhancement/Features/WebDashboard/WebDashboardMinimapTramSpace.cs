using System.Reflection;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMinimapTramSpace
    {
        private const float BoundsPadding = 0.05f;
        private const float WaitingRoomMinSpan = 8f;
        private const float WaitingRoomMaxSpan = 80f;
        private const float WaitingRoomFallbackHalfSpanX = 18f;
        private const float WaitingRoomFallbackHalfSpanZ = 30f;

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? BgRootField =
            typeof(GameMainBase).GetField("BGRoot", InstanceFlags);

        private static readonly FieldInfo? TramConsoleField =
            typeof(GameMainBase).GetField("tramConsole", InstanceFlags);

        internal static bool IsWaitingRoom(GameMainBase? main)
        {
            return main is InTramWaitingScene;
        }

        internal static Transform? TryGetAnchor(GameMainBase? main)
        {
            return main != null ? BgRootField?.GetValue(main) as Transform : null;
        }

        internal static Transform? TryGetInteriorScope(GameMainBase? main)
        {
            if (main == null)
            {
                return null;
            }

            if (TramConsoleField?.GetValue(main) is Component console)
            {
                Transform parent = console.transform.parent;
                return parent != null ? parent : console.transform;
            }

            return TryGetAnchor(main);
        }

        internal static void WorldToLocal(
            GameMainBase? main,
            Transform transform,
            out float x,
            out float z,
            out float yaw)
        {
            Transform? anchor = TryGetAnchor(main);
            if (anchor == null)
            {
                Vector3 worldPosition = transform.position;
                x = worldPosition.x;
                z = worldPosition.z;
                yaw = ResolveHorizontalYaw(transform);
                return;
            }

            Vector3 local = anchor.InverseTransformPoint(transform.position);
            x = local.x;
            z = local.z;

            Vector3 localForward = anchor.InverseTransformDirection(transform.forward);
            localForward.y = 0f;
            yaw = localForward.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(localForward.x, localForward.z) * Mathf.Rad2Deg
                : 0f;
        }

        private static float ResolveHorizontalYaw(Transform transform)
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return transform.eulerAngles.y;
            }

            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        internal static WebDashboardMinimapBoundsDto BuildWaitingRoomBounds(GameMainBase? main)
        {
            Transform? anchor = TryGetAnchor(main);
            Transform? scope = TryGetInteriorScope(main);
            if (anchor == null || scope == null)
            {
                return WaitingRoomFallbackBounds();
            }

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minZ = float.PositiveInfinity;
            float maxZ = float.NegativeInfinity;

            Renderer[] renderers = scope.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                IncludeWorldBoundsInLocalSpace(anchor, renderer.bounds, ref minX, ref maxX, ref minZ, ref maxZ);
            }

            if (float.IsPositiveInfinity(minX))
            {
                return WaitingRoomFallbackBounds();
            }

            float spanX = maxX - minX;
            float spanZ = maxZ - minZ;
            if (spanX > WaitingRoomMaxSpan || spanZ > WaitingRoomMaxSpan)
            {
                return WaitingRoomFallbackBounds();
            }

            spanX = Mathf.Max(spanX, WaitingRoomMinSpan);
            spanZ = Mathf.Max(spanZ, WaitingRoomMinSpan);
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

        internal static WebDashboardMinimapTrainDto CreateWaitingRoomTrainMarker()
        {
            return new WebDashboardMinimapTrainDto
            {
                X = 0f,
                Z = 0f,
                Yaw = 0f,
            };
        }

        private static WebDashboardMinimapBoundsDto WaitingRoomFallbackBounds()
        {
            float padX = WaitingRoomFallbackHalfSpanX * BoundsPadding;
            float padZ = WaitingRoomFallbackHalfSpanZ * BoundsPadding;

            return new WebDashboardMinimapBoundsDto
            {
                MinX = -WaitingRoomFallbackHalfSpanX - padX,
                MinZ = -WaitingRoomFallbackHalfSpanZ - padZ,
                MaxX = WaitingRoomFallbackHalfSpanX + padX,
                MaxZ = WaitingRoomFallbackHalfSpanZ + padZ,
            };
        }

        private static void IncludeWorldBoundsInLocalSpace(
            Transform anchor,
            Bounds worldBounds,
            ref float minX,
            ref float maxX,
            ref float minZ,
            ref float maxZ)
        {
            Vector3 center = worldBounds.center;
            Vector3 extents = worldBounds.extents;

            for (int xi = -1; xi <= 1; xi += 2)
            {
                for (int yi = -1; yi <= 1; yi += 2)
                {
                    for (int zi = -1; zi <= 1; zi += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(extents, new Vector3(xi, yi, zi));
                        Vector3 local = anchor.InverseTransformPoint(corner);
                        minX = Mathf.Min(minX, local.x);
                        maxX = Mathf.Max(maxX, local.x);
                        minZ = Mathf.Min(minZ, local.z);
                        maxZ = Mathf.Max(maxZ, local.z);
                    }
                }
            }
        }
    }
}
