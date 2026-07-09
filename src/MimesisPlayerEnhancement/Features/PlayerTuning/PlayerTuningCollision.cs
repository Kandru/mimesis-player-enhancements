using System.Reflection;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.PlayerTuning
{
    /// <summary>
    /// Disables remote player capsule colliders on the local client so the host avatar
    /// can move through other players. Server positions are not validated for overlap.
    /// </summary>
    internal static class PlayerTuningCollision
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Type? CapsuleColliderType =
            AccessTools.TypeByName("UnityEngine.CapsuleCollider");

        private static readonly MethodInfo? GetComponentByTypeMethod =
            AccessTools.Method(typeof(ProtoActor), "GetComponent", [typeof(Type)]);

        private static readonly PropertyInfo? ColliderEnabledProperty =
            CapsuleColliderType != null
                ? AccessTools.Property(CapsuleColliderType.BaseType!, "enabled")
                : null;

        internal static bool ShouldDisable =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() =>
                PlayerTuningResolver.IsFeatureEnabled && ModConfig.DisablePlayerCollision.Value);

        internal static void RefreshFromConfig()
        {
            GameMainBase? main = JoinAnytimeHub.GetPdata()?.main;
            if (main == null)
            {
                return;
            }

            Dictionary<int, ProtoActor>? map = main.GetProtoActorMap();
            if (map == null || map.Count == 0)
            {
                return;
            }

            int updated = 0;
            foreach (ProtoActor actor in map.Values)
            {
                if (TryApplyToActor(actor))
                {
                    updated++;
                }
            }

            if (updated > 0)
            {
                PlayerTuningLog.DebugUpdatedPlayerCollision(updated, ShouldDisable);
            }
        }

        internal static void OnRemotePlayerConfigured(ProtoActor actor)
        {
            TryApplyToActor(actor);
        }

        private static bool TryApplyToActor(ProtoActor actor)
        {
            if (!IsRemotePlayerProxy(actor))
            {
                return false;
            }

            SetCapsuleColliderEnabled(actor, enabled: !ShouldDisable);
            return true;
        }

        private static bool IsRemotePlayerProxy(ProtoActor actor)
        {
            return actor.ActorType == ActorType.Player && !actor.AmIAvatar();
        }

        private static void SetCapsuleColliderEnabled(ProtoActor actor, bool enabled)
        {
            if (actor.dead && enabled)
            {
                return;
            }

            object? capsule = TryGetCapsuleCollider(actor);
            if (capsule == null || ColliderEnabledProperty == null)
            {
                return;
            }

            if (ColliderEnabledProperty.GetValue(capsule) is bool isEnabled && isEnabled != enabled)
            {
                ColliderEnabledProperty.SetValue(capsule, enabled);
            }
        }

        private static object? TryGetCapsuleCollider(ProtoActor actor)
        {
            if (CapsuleColliderType == null || GetComponentByTypeMethod == null)
            {
                return null;
            }

            return GetComponentByTypeMethod.Invoke(actor, [CapsuleColliderType]);
        }
    }
}
