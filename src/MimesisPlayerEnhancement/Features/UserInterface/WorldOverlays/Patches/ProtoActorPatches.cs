using System.Reflection;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays.Patches
{
    [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.DestroyActor))]
    internal static class DestroyActorPrefix
    {
        private const string Feature = "Ui";

        [HarmonyPrefix]
        private static void Prefix(ProtoActor __instance)
        {
            try
            {
                WorldOverlayRuntime.ReleaseDamageGlowForDespawn(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"DestroyActor prefix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.OnActorDeath))]
    internal static class OnActorDeathPrefix
    {
        private const string Feature = "Ui";

        [HarmonyPrefix]
        private static void Prefix(ProtoActor __instance)
        {
            if (!WorldOverlayGate.DamageHealthGlowEnabled)
            {
                return;
            }

            try
            {
                WorldOverlayRuntime.NotifyKilled(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnActorDeath prefix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.UpdateHp))]
    internal static class UpdateHpPostfix
    {
        private const string Feature = "Ui";

        [HarmonyPrefix]
        private static void Prefix(ProtoActor __instance, out long __state)
        {
            __state = __instance.netSyncActorData?.hp ?? 0L;
        }

        [HarmonyPostfix]
        private static void Postfix(ProtoActor __instance, long hp, long maxHP, long __state)
        {
            if (!WorldOverlayGate.DamageHealthGlowEnabled || hp >= __state || maxHP <= 0)
            {
                return;
            }

            try
            {
                WorldOverlayRuntime.NotifyDamaged(__instance, hp, maxHP);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"UpdateHp postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    internal static class HitTargetSigPostfix
    {
        private const string Feature = "Ui";

        internal static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(ProtoActor), "ResolvePacket_HitTargetSig");

        [HarmonyPostfix]
        private static void Postfix(ProtoActor __instance, HitTargetSig sig)
        {
            if (!WorldOverlayGate.DamageHealthGlowEnabled && !WorldOverlayGate.DamageNumbersEnabled)
            {
                return;
            }

            try
            {
                WorldOverlayPatchHelpers.ProcessHitTargets(Hub.Main, sig.targetHitInfos);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"HitTargetSig postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    internal static class ActorDamagedSigPostfix
    {
        private const string Feature = "Ui";

        internal static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(ProtoActor), "OnPacket", [typeof(ActorDamagedSig)]);

        [HarmonyPostfix]
        private static void Postfix(ProtoActor __instance, ActorDamagedSig sig)
        {
            if (!WorldOverlayGate.DamageHealthGlowEnabled && !WorldOverlayGate.DamageNumbersEnabled)
            {
                return;
            }

            try
            {
                if (sig.amount <= 0)
                {
                    return;
                }

                if (!WorldOverlayPatchHelpers.TryConsumeHit(__instance.ActorID, sig.amount))
                {
                    return;
                }

                WorldOverlayRuntime.NotifyHitDamage(__instance, sig.amount);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"ActorDamagedSig postfix failed — {ex.Message}");
            }
        }
    }
}
