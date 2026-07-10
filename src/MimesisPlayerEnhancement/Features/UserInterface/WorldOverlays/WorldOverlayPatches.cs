using System.Reflection;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays
{
    internal static class WorldOverlayPatches
    {
        private const string Feature = "Ui";

        private static readonly MethodInfo ResolvePacketHitTargetSigMethod =
            AccessTools.Method(typeof(ProtoActor), "ResolvePacket_HitTargetSig");

        private static readonly MethodInfo FieldHitTargetSigMethod =
            AccessTools.Method(typeof(GameMainBase), "OnPacket", [typeof(FieldHitTargetSig)]);

        private static readonly MethodInfo ProjectileHitTargetSigMethod =
            AccessTools.Method(typeof(GameMainBase), "OnPacket", [typeof(ProjectileHitTargetSig)]);

        internal static IEnumerable<Type> GetPatchTypes()
        {
            yield return typeof(UpdateHpPrefix);
            yield return typeof(UpdateHpPostfix);
            yield return typeof(UpdateContaPrefix);
            yield return typeof(UpdateContaPostfix);
            if (ResolvePacketHitTargetSigMethod != null)
            {
                yield return typeof(HitTargetSigPostfix);
            }

            if (FieldHitTargetSigMethod != null)
            {
                yield return typeof(FieldHitTargetSigPostfix);
            }

            if (ProjectileHitTargetSigMethod != null)
            {
                yield return typeof(ProjectileHitTargetSigPostfix);
            }
        }

        [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.UpdateHp))]
        internal static class UpdateHpPrefix
        {
            [HarmonyPrefix]
            private static void Prefix(ProtoActor __instance, out long __state)
            {
                __state = __instance.netSyncActorData?.hp ?? 0L;
            }
        }

        [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.UpdateHp))]
        internal static class UpdateHpPostfix
        {
            [HarmonyPostfix]
            private static void Postfix(ProtoActor __instance, long hp, long __state)
            {
                if (!WorldOverlayGate.AnyOverlayEnabled)
                {
                    return;
                }

                try
                {
                    WorldOverlayHpTracker.NotifySynced(__instance);
                    if (__state <= hp)
                    {
                        return;
                    }

                    WorldOverlayRuntime.NotifyHpChanged(__instance, __state, hp);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"UpdateHp postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.UpdateConta))]
        internal static class UpdateContaPrefix
        {
            [HarmonyPrefix]
            private static void Prefix(ProtoActor __instance, out long __state)
            {
                __state = __instance.netSyncActorData?.conta ?? 0L;
            }
        }

        [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.UpdateConta))]
        internal static class UpdateContaPostfix
        {
            [HarmonyPostfix]
            private static void Postfix(ProtoActor __instance, long conta, long maxConta, long __state)
            {
                if (!WorldOverlayGate.DetoxIndicatorsEnabled)
                {
                    return;
                }

                try
                {
                    if (__state <= conta)
                    {
                        return;
                    }

                    WorldOverlayRuntime.NotifyContaReduced(__instance, __state, conta, maxConta);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"UpdateConta postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(ProtoActor), "ResolvePacket_HitTargetSig")]
        internal static class HitTargetSigPostfix
        {
            [HarmonyPostfix]
            private static void Postfix(ProtoActor __instance, HitTargetSig sig)
            {
                if (!WorldOverlayGate.HealthBarsEnabled && !WorldOverlayGate.DamageNumbersEnabled)
                {
                    return;
                }

                try
                {
                    ProcessHitTargets(Hub.Main, sig.targetHitInfos);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"HitTargetSig postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(GameMainBase), "OnPacket", typeof(FieldHitTargetSig))]
        internal static class FieldHitTargetSigPostfix
        {
            [HarmonyPostfix]
            private static void Postfix(GameMainBase __instance, FieldHitTargetSig sig)
            {
                if (!WorldOverlayGate.HealthBarsEnabled && !WorldOverlayGate.DamageNumbersEnabled)
                {
                    return;
                }

                try
                {
                    ProcessHitTargets(__instance, sig.targetHitInfos);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"FieldHitTargetSig postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(GameMainBase), "OnPacket", typeof(ProjectileHitTargetSig))]
        internal static class ProjectileHitTargetSigPostfix
        {
            [HarmonyPostfix]
            private static void Postfix(GameMainBase __instance, ProjectileHitTargetSig sig)
            {
                if (!WorldOverlayGate.HealthBarsEnabled && !WorldOverlayGate.DamageNumbersEnabled)
                {
                    return;
                }

                try
                {
                    ProcessHitTargets(__instance, sig.targetHitInfos);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"ProjectileHitTargetSig postfix failed — {ex.Message}");
                }
            }
        }

        private static void ProcessHitTargets(GameMainBase? main, List<TargetHitInfo>? hits)
        {
            if (main == null || hits == null)
            {
                return;
            }

            foreach (TargetHitInfo hit in hits)
            {
                if (hit.damage <= 0)
                {
                    continue;
                }

                ProtoActor? victim = main.GetActorByActorID(hit.targetID);
                if (victim == null)
                {
                    continue;
                }

                WorldOverlayRuntime.NotifyHitDamage(victim, hit.damage);
            }
        }
    }
}
