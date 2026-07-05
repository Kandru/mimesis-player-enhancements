using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardMinimapTramSpace
    {
        internal static bool IsWaitingRoom(GameMainBase? main)
        {
            return main is InTramWaitingScene;
        }

        internal static Transform? TryGetAnchor(GameMainBase? main)
        {
            return WebDashboardSceneRoots.TryGetBgRoot(main);
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

    }
}
