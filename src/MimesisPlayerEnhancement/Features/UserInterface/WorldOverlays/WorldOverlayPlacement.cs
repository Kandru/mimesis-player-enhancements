using System.Reflection;
using Bifrost.Cooked;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal static class WorldOverlayPlacement
    {
        private const float HipHeightRatio = 0.52f;
        private const float HealthBarForwardOffset = 0.5f;
        private const float FloaterForwardOffset = 0.4f;

        private static readonly Type? CapsuleColliderType =
            AccessTools.TypeByName("UnityEngine.CapsuleCollider");

        private static readonly Type? CharacterControllerType =
            AccessTools.TypeByName("UnityEngine.CharacterController");

        private static readonly MethodInfo? GetComponentByTypeMethod =
            AccessTools.Method(typeof(ProtoActor), "GetComponent", [typeof(Type)]);

        private static readonly PropertyInfo? CapsuleHeightProperty =
            CapsuleColliderType != null ? AccessTools.Property(CapsuleColliderType, "height") : null;

        private static readonly PropertyInfo? CapsuleCenterProperty =
            CapsuleColliderType != null ? AccessTools.Property(CapsuleColliderType, "center") : null;

        private static readonly PropertyInfo? CapsuleEnabledProperty =
            CapsuleColliderType?.BaseType != null
                ? AccessTools.Property(CapsuleColliderType.BaseType, "enabled")
                : null;

        private static readonly PropertyInfo? ControllerHeightProperty =
            CharacterControllerType != null ? AccessTools.Property(CharacterControllerType, "height") : null;

        private static readonly PropertyInfo? ControllerCenterProperty =
            CharacterControllerType != null ? AccessTools.Property(CharacterControllerType, "center") : null;

        internal static Vector3 ResolveHealthBarWorldPosition(ProtoActor actor)
        {
            Vector3 anchor = ResolveCapsuleAnchor(actor, HipHeightRatio);
            return OffsetTowardLocalViewer(actor, anchor, HealthBarForwardOffset);
        }

        internal static Vector3 ResolveFloaterWorldPosition(ProtoActor actor, float heightRatio = 0.58f)
        {
            Vector3 anchor = ResolveCapsuleAnchor(actor, heightRatio);
            return OffsetTowardLocalViewer(actor, anchor, FloaterForwardOffset);
        }

        private static Vector3 ResolveCapsuleAnchor(ProtoActor actor, float heightRatio)
        {
            if (TryGetCapsuleMetrics(actor, out Vector3 worldCenter, out float height))
            {
                float hipY = worldCenter.y - (height * 0.5f) + (height * heightRatio);
                return new Vector3(worldCenter.x, hipY, worldCenter.z);
            }

            float fallbackHeight = ResolveFallbackBodyHeight(actor);
            return actor.transform.position + Vector3.up * (fallbackHeight * heightRatio);
        }

        private static bool TryGetCapsuleMetrics(ProtoActor actor, out Vector3 worldCenter, out float height)
        {
            worldCenter = default;
            height = 0f;

            object? capsule = GetComponent(CapsuleColliderType, actor);
            if (capsule != null && TryReadCapsuleMetrics(capsule, actor, out worldCenter, out height))
            {
                return true;
            }

            object? controller = GetComponent(CharacterControllerType, actor);
            if (controller != null && TryReadControllerMetrics(controller, actor, out worldCenter, out height))
            {
                return true;
            }

            return false;
        }

        private static bool TryReadCapsuleMetrics(
            object capsule,
            ProtoActor actor,
            out Vector3 worldCenter,
            out float height)
        {
            worldCenter = default;
            height = 0f;

            if (CapsuleEnabledProperty?.GetValue(capsule) is bool enabled && !enabled)
            {
                return false;
            }

            if (CapsuleHeightProperty?.GetValue(capsule) is not float capsuleHeight || capsuleHeight <= 0.01f)
            {
                return false;
            }

            if (CapsuleCenterProperty?.GetValue(capsule) is not Vector3 center)
            {
                return false;
            }

            worldCenter = actor.transform.TransformPoint(center);
            height = capsuleHeight;
            return true;
        }

        private static bool TryReadControllerMetrics(
            object controller,
            ProtoActor actor,
            out Vector3 worldCenter,
            out float height)
        {
            worldCenter = default;
            height = 0f;

            if (ControllerHeightProperty?.GetValue(controller) is not float controllerHeight
                || controllerHeight <= 0.01f)
            {
                return false;
            }

            if (ControllerCenterProperty?.GetValue(controller) is not Vector3 center)
            {
                return false;
            }

            worldCenter = actor.transform.TransformPoint(center);
            height = controllerHeight;
            return true;
        }

        private static object? GetComponent(Type? componentType, ProtoActor actor)
        {
            if (componentType == null || GetComponentByTypeMethod == null)
            {
                return null;
            }

            return GetComponentByTypeMethod.Invoke(actor, [componentType]);
        }

        private static float ResolveFallbackBodyHeight(ProtoActor actor)
        {
            if (actor.ActorType == ActorType.Monster && actor.monsterMasterID > 0)
            {
                MonsterInfo? info = HubGameDataAccess.Excel?.GetMonsterInfo(actor.monsterMasterID);
                if (info != null && info.HitCollisionRadius > 0f)
                {
                    return Mathf.Max(1.2f, info.HitCollisionRadius * 2.5f);
                }
            }

            return actor.IsPlayer() ? 1.75f : 2f;
        }

        private static Vector3 OffsetTowardLocalViewer(ProtoActor actor, Vector3 anchor, float forwardOffset)
        {
            Vector3? viewerPosition = WorldOverlayViewer.TryGetWorldPosition();
            Vector3 horizontal = viewerPosition.HasValue
                ? viewerPosition.Value - actor.transform.position
                : actor.transform.forward;
            horizontal.y = 0f;

            if (horizontal.sqrMagnitude < 0.0001f)
            {
                horizontal = actor.transform.forward;
                horizontal.y = 0f;
            }

            if (horizontal.sqrMagnitude < 0.0001f)
            {
                return anchor;
            }

            return anchor + horizontal.normalized * forwardOffset;
        }
    }
}
