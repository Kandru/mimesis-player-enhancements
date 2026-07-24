using System.Reflection;

namespace MimesisPlayerEnhancement.Features.UserInterface.WorldOverlays.Patches
{
    // game@0.3.1 Assembly-CSharp/GameMainBase.cs:L3984-4032
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

    // game@0.3.1 Assembly-CSharp/GameMainBase.cs:L4035-4089
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
