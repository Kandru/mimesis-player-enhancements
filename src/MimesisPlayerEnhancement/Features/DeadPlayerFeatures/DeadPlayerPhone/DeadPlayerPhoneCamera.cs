using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    /// <summary>
    /// Locks the death spectator camera onto a phone during ring/talk, mirroring mimic possession follow behavior.
    /// </summary>
    internal static class DeadPlayerPhoneCamera
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? SpectatorCameraField =
            AccessTools.Field(typeof(CameraManager), "spectatorCamera");

        private static readonly MethodInfo? SetupSpectatorCameraMethod =
            AccessTools.Method(typeof(CameraManager), "SetupSpectatorCamera");

        private static readonly MethodInfo? CancelPendingSpectatorSwitchMethod =
            AccessTools.Method(typeof(CameraManager), "CancelPendingSpectatorSwitch");

        private static bool _locked;
        private static int _savedSpectatorTargetActorId;
        private static Transform? _phoneFollowTarget;

        internal static bool IsLocked => _locked;

        internal static void Enter(PhoneLevelObject phone)
        {
            if (phone == null || !DeadPlayerPhoneResolver.IsPhoneRingEnabled)
            {
                return;
            }

            CameraManager? cameraman = DeadPlayerPhoneGameAccess.TryGetCameraManager();
            if (cameraman == null || !cameraman.IsSpectatorMode)
            {
                return;
            }

            _savedSpectatorTargetActorId = cameraman.SpectatorTargetActorID ?? 0;
            _phoneFollowTarget = phone.transform;
            _locked = true;

            CancelPendingSpectatorSwitchMethod?.Invoke(cameraman, null);
            ApplyFollow(cameraman);
        }

        internal static void UpdateFollow()
        {
            if (!_locked)
            {
                return;
            }

            CameraManager? cameraman = DeadPlayerPhoneGameAccess.TryGetCameraManager();
            if (cameraman == null)
            {
                Exit();
                return;
            }

            if (_phoneFollowTarget == null)
            {
                PhoneLevelObject? phone = DeadPlayerPhoneAccess.TryFindClientPhone(
                    DeadPlayerPhoneLocalState.PhoneLevelObjectId);
                _phoneFollowTarget = phone?.transform;
            }

            if (_phoneFollowTarget == null)
            {
                return;
            }

            ApplyFollow(cameraman);
        }

        internal static void Exit()
        {
            if (!_locked)
            {
                return;
            }

            _locked = false;
            CameraManager? cameraman = DeadPlayerPhoneGameAccess.TryGetCameraManager();
            if (cameraman != null && _savedSpectatorTargetActorId > 0)
            {
                ProtoActor? actor = DeadPlayerPhoneGameAccess.TryGetMain()
                    ?.GetActorByActorID(_savedSpectatorTargetActorId);
                if (actor != null && !actor.dead)
                {
                    SetupSpectatorCameraMethod?.Invoke(cameraman, [actor]);
                }
            }

            _savedSpectatorTargetActorId = 0;
            _phoneFollowTarget = null;
        }

        internal static void ForceReset()
        {
            _locked = false;
            _savedSpectatorTargetActorId = 0;
            _phoneFollowTarget = null;
        }

        private static void ApplyFollow(CameraManager cameraman)
        {
            if (SpectatorCameraField?.GetValue(cameraman) is not Component spectatorCamera
                || _phoneFollowTarget == null)
            {
                return;
            }

            PropertyInfo? followProperty = spectatorCamera.GetType().GetProperty("Follow", InstanceFlags);
            PropertyInfo? lookAtProperty = spectatorCamera.GetType().GetProperty("LookAt", InstanceFlags);
            followProperty?.SetValue(spectatorCamera, _phoneFollowTarget);
            lookAtProperty?.SetValue(spectatorCamera, _phoneFollowTarget);
        }
    }
}
