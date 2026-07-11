namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays.Patches
{
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
        private const string Feature = "Ui";

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
        private const string Feature = "Ui";

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

    [HarmonyPatch]
    internal static class HitTargetSigPostfix
    {
        private const string Feature = "Ui";

        internal static System.Reflection.MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(ProtoActor), "ResolvePacket_HitTargetSig");

        [HarmonyPostfix]
        private static void Postfix(ProtoActor __instance, HitTargetSig sig)
        {
            if (!WorldOverlayGate.HealthBarsEnabled && !WorldOverlayGate.DamageNumbersEnabled)
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
}
