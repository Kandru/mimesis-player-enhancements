using System.Reflection;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays.Patches
{
    [HarmonyPatch]
    internal static class FieldHitTargetSigPostfix
    {
        private const string Feature = "Ui";

        internal static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(GameMainBase), "OnPacket", [typeof(FieldHitTargetSig)]);

        [HarmonyPostfix]
        private static void Postfix(GameMainBase __instance, FieldHitTargetSig sig)
        {
            if (!WorldOverlayGate.DamageHealthGlowEnabled && !WorldOverlayGate.DamageNumbersEnabled)
            {
                return;
            }

            try
            {
                WorldOverlayPatchHelpers.ProcessHitTargets(__instance, sig.targetHitInfos);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FieldHitTargetSig postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    internal static class ProjectileHitTargetSigPostfix
    {
        private const string Feature = "Ui";

        internal static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(GameMainBase), "OnPacket", [typeof(ProjectileHitTargetSig)]);

        [HarmonyPostfix]
        private static void Postfix(GameMainBase __instance, ProjectileHitTargetSig sig)
        {
            if (!WorldOverlayGate.DamageHealthGlowEnabled && !WorldOverlayGate.DamageNumbersEnabled)
            {
                return;
            }

            try
            {
                WorldOverlayPatchHelpers.ProcessHitTargets(__instance, sig.targetHitInfos);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"ProjectileHitTargetSig postfix failed — {ex.Message}");
            }
        }
    }
}
