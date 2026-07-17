using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal static class WorldOverlayViewer
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? HubCameramanField =
            typeof(Hub).GetField("cameraman", InstanceFlags);

        internal static Vector3? TryGetWorldPosition()
        {
            if (TryGetSpectatorTargetPosition(out Vector3 spectatorPosition))
            {
                return spectatorPosition;
            }

            GameMainBase? main = Hub.Main;
            ProtoActor? avatar = main?.GetMyAvatar();
            if (avatar != null)
            {
                return avatar.transform.position;
            }

            if (Camera.main != null)
            {
                return Camera.main.transform.position;
            }

            Camera[] cameras = Camera.allCameras;
            return cameras.Length > 0 ? cameras[0].transform.position : null;
        }

        internal static Vector3 ResolveDriftDirection(Vector3 spawnPosition)
        {
            Vector3? viewerPosition = TryGetWorldPosition();
            if (!viewerPosition.HasValue)
            {
                return Vector3.up;
            }

            Vector3 direction = viewerPosition.Value - spawnPosition;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return Vector3.up;
            }

            return direction.normalized;
        }

        internal static void BillboardTowardCamera(Transform target, Vector3 worldPosition, Camera? camera)
        {
            if (camera == null)
            {
                return;
            }

            Vector3 lookDirection = worldPosition - camera.transform.position;
            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                target.rotation = Quaternion.LookRotation(lookDirection);
            }
        }

        private static bool TryGetSpectatorTargetPosition(out Vector3 position)
        {
            position = default;
            if (Hub.s == null || HubCameramanField == null)
            {
                return false;
            }

            if (HubCameramanField.GetValue(Hub.s) is not CameraManager cameraman)
            {
                return false;
            }

            if (!cameraman.TryGetCurrentSpectatorTarget(out ProtoActor? target) || target == null)
            {
                return false;
            }

            position = target.transform.position;
            return true;
        }
    }
}
