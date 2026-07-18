using System.Reflection;
using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.PlayerTuning
{
    /// <summary>
    /// Disables remote player and mimic capsule colliders on the local client so the avatar
    /// can move through them. Server positions are not validated for overlap.
    /// </summary>
    internal static class PlayerTuningCollision
    {
        private static readonly Type? CapsuleColliderType =
            AccessTools.TypeByName("UnityEngine.CapsuleCollider");

        private static readonly MethodInfo? GetComponentByTypeMethod =
            AccessTools.Method(typeof(ProtoActor), "GetComponent", [typeof(Type)]);

        private static readonly PropertyInfo? ColliderEnabledProperty =
            CapsuleColliderType != null
                ? AccessTools.Property(CapsuleColliderType.BaseType!, "enabled")
                : null;

        internal static bool ShouldDisable => PlayerTuningResolver.DisablePlayerCollision;

        internal static void RefreshFromConfig()
        {
            GameMainBase? main = GameSessionAccess.TryGetPdata()?.main;
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

        internal static void OnPassThroughActorConfigured(ProtoActor actor)
        {
            // When pass-through is off, vanilla actor setup already leaves the collider in the
            // correct state — RefreshFromConfig handles reverts on toggle-off. Skip the
            // reflection-based collider access on this per-actor lifecycle path.
            if (!ShouldDisable)
            {
                return;
            }

            TryApplyToActor(actor);
        }

        internal static void OnPassThroughActorConfigured(ProtoActor actor, int masterId)
        {
            if (!ShouldDisable || !IsMimicMasterId(masterId))
            {
                return;
            }

            SetCapsuleColliderEnabled(actor, GetTargetColliderEnabled(masterId));
        }

        private static bool TryApplyToActor(ProtoActor actor)
        {
            if (!ShouldApplyPassThrough(actor))
            {
                return false;
            }

            SetCapsuleColliderEnabled(actor, GetTargetColliderEnabled(actor));
            return true;
        }

        private static bool ShouldApplyPassThrough(ProtoActor actor)
        {
            return IsRemotePlayerProxy(actor) || IsMimicProxy(actor);
        }

        private static bool IsRemotePlayerProxy(ProtoActor actor)
        {
            return actor.ActorType == ActorType.Player && !actor.AmIAvatar();
        }

        private static bool IsMimicProxy(ProtoActor actor)
        {
            return actor.ActorType == ActorType.Monster && actor.IsMimic();
        }

        private static bool IsMimicMasterId(int masterId)
        {
            return MonsterTypeLookup.TryGetMonster(masterId, out MonsterInfo info) && info.IsMimic();
        }

        private static bool GetTargetColliderEnabled(ProtoActor actor)
        {
            if (ShouldDisable)
            {
                return false;
            }

            if (IsMimicProxy(actor))
            {
                return GetVanillaMimicColliderEnabled(actor.monsterMasterID);
            }

            return true;
        }

        private static bool GetTargetColliderEnabled(int masterId)
        {
            return ShouldDisable ? false : GetVanillaMimicColliderEnabled(masterId);
        }

        private static bool GetVanillaMimicColliderEnabled(int masterId)
        {
            if (!MonsterTypeLookup.TryGetMonster(masterId, out MonsterInfo info))
            {
                return true;
            }

            return !info.CapsuleColliderOff;
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
