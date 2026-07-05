using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.DeadPlayerPhone
{
    internal static class DeadPlayerPhoneClient
    {
        private static readonly FieldInfo? LevelObjectIdField =
            AccessTools.Field(typeof(LevelObject), "levelObjectID");

        private static PhoneLevelObject? _availablePhone;
        private static float _mimicLookAngle = float.MaxValue;
        private static float _phoneLookAngle = float.MaxValue;
        private static int _pendingPhoneLevelObjectId;

        internal static PhoneLevelObject? AvailablePhone => _availablePhone;

        internal static PreferredDeadPlayerAction PreferredAction { get; private set; }

        internal static bool HasPendingRingRequest => _pendingPhoneLevelObjectId > 0;

        internal static void ClearPendingRingRequest()
        {
            _pendingPhoneLevelObjectId = 0;
        }

        internal static void MarkPendingRing(int levelObjectId)
        {
            _pendingPhoneLevelObjectId = levelObjectId;
        }

        internal static void Reset()
        {
            _availablePhone = null;
            _mimicLookAngle = float.MaxValue;
            _phoneLookAngle = float.MaxValue;
            _pendingPhoneLevelObjectId = 0;
            PreferredAction = PreferredDeadPlayerAction.None;
        }

        internal static void UpdateAfterMimicCheck(ProtoActor? availableMimic)
        {
            PreferredAction = PreferredDeadPlayerAction.None;
            _availablePhone = null;
            _mimicLookAngle = float.MaxValue;
            _phoneLookAngle = float.MaxValue;

            if (!DeadPlayerPhoneResolver.IsPhoneRingEnabled)
            {
                return;
            }

            CameraManager? cameraman = DeadPlayerPhoneGameAccess.TryGetCameraManager();
            if (cameraman == null || !cameraman.IsSpectatorMode)
            {
                return;
            }

            if (cameraman.Mode == CameraManager.CameraMode.MimicPossession)
            {
                return;
            }

            if (DeadPlayerPhoneLocalState.HasActiveLocalSession)
            {
                return;
            }

            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            if (pdata == null)
            {
                return;
            }

            int myActorId = pdata.MyActorID;
            if (myActorId <= 0)
            {
                return;
            }

            if (DeadPlayerPhoneSessions.IsInCooldown(myActorId))
            {
                return;
            }

            int? spectatedId = cameraman.SpectatorTargetActorID;
            if (spectatedId is not int targetActorId)
            {
                return;
            }

            GameMainBase? main = Hub.Main;
            if (main is not GamePlayScene scene)
            {
                return;
            }

            ProtoActor? spectated = main.GetActorByActorID(targetActorId);
            if (spectated == null)
            {
                return;
            }

            Camera? camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            float maxDistance = DeadPlayerPhoneResolver.MaxDistanceMeters;
            float maxAngle = DeadPlayerPhoneResolver.MaxLookAngleDegrees;
            float maxSqr = maxDistance * maxDistance;

            Vector3 cameraPos = camera.transform.position;
            Vector3 cameraForward = camera.transform.forward;
            Vector3 flatForward = new Vector3(cameraForward.x, 0f, cameraForward.z).normalized;

            if (availableMimic != null)
            {
                _mimicLookAngle = ComputeLookAngle(flatForward, cameraPos, availableMimic.transform.position);
            }

            PhoneLevelObject? bestPhone = null;
            float bestPhoneAngle = float.MaxValue;
            float bestPhoneSqr = float.MaxValue;

            foreach (LevelObject levelObject in scene.CollectLevelObjects())
            {
                if (levelObject is not PhoneLevelObject phone
                    || phone.PhoneStateValue != PhoneState.Idle)
                {
                    continue;
                }

                float sqrDist = (spectated.transform.position - phone.transform.position).sqrMagnitude;
                if (sqrDist > maxSqr)
                {
                    continue;
                }

                Vector3 rayOrigin = spectated.transform.position + Vector3.up * 1.6f;
                Vector3 rayTarget = phone.transform.position + Vector3.up * 1.6f;
                if (PhysicsUtility.CheckBlockByWall(rayOrigin, rayTarget))
                {
                    continue;
                }

                float angle = ComputeLookAngle(flatForward, cameraPos, phone.transform.position);
                if (angle > maxAngle)
                {
                    continue;
                }

                if (sqrDist < bestPhoneSqr)
                {
                    bestPhoneSqr = sqrDist;
                    bestPhoneAngle = angle;
                    bestPhone = phone;
                }
            }

            if (bestPhone != null)
            {
                _availablePhone = bestPhone;
                _phoneLookAngle = bestPhoneAngle;
            }

            bool mimicValid = availableMimic != null && _mimicLookAngle <= maxAngle;
            bool phoneValid = _availablePhone != null;

            if (mimicValid && phoneValid)
            {
                PreferredAction = _mimicLookAngle <= _phoneLookAngle
                    ? PreferredDeadPlayerAction.Mimic
                    : PreferredDeadPlayerAction.Phone;
            }
            else if (phoneValid)
            {
                PreferredAction = PreferredDeadPlayerAction.Phone;
            }
            else if (mimicValid)
            {
                PreferredAction = PreferredDeadPlayerAction.Mimic;
            }
        }

        internal static bool TrySendRingRequest()
        {
            if (_availablePhone == null)
            {
                return false;
            }

            int levelObjectId = GetLevelObjectId(_availablePhone);
            if (levelObjectId <= 0)
            {
                return false;
            }

            MarkPendingRing(levelObjectId);
            DeadPlayerPhoneNetwork.TryRingPhone(levelObjectId);
            return true;
        }

        internal static void TryEndInteraction()
        {
            if (!DeadPlayerPhoneLocalState.HasActiveLocalSession)
            {
                return;
            }

            int phoneId = DeadPlayerPhoneLocalState.PhoneLevelObjectId;
            if (phoneId <= 0)
            {
                return;
            }

            DeadPlayerPhoneNetwork.TryEndPhoneInteraction(
                phoneId,
                DeadPlayerPhoneLocalState.Phase);
        }

        internal static int GetLevelObjectId(PhoneLevelObject phone)
        {
            if (LevelObjectIdField?.GetValue(phone) is int id)
            {
                return id;
            }

            return 0;
        }

        internal static float GetMimicLookAngle() => _mimicLookAngle;

        internal static float GetPhoneLookAngle() => _phoneLookAngle;

        private static float ComputeLookAngle(Vector3 flatForward, Vector3 cameraPos, Vector3 targetPos)
        {
            Vector3 flatDir = new Vector3(targetPos.x - cameraPos.x, 0f, targetPos.z - cameraPos.z).normalized;
            return Vector3.Angle(flatForward, flatDir);
        }
    }
}
