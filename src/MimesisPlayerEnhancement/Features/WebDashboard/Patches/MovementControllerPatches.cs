using System.Reflection;

namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    [HarmonyPatch(typeof(MovementController), nameof(MovementController.CheckFallDamage))]
    internal static class CheckFallDamagePatch
    {
        private static readonly FieldInfo CreatureField =
            AccessTools.Field(typeof(MovementController), "_creature")!;

        [HarmonyPrefix]
        private static bool Prefix(MovementController __instance, ref float __result)
        {
            if (CreatureField.GetValue(__instance) is VCreature creature
                && WebDashboardHostCheatsRuntime.IsNoClipActive(creature))
            {
                __result = 0f;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(MovementController), nameof(MovementController.DirectMoveStart))]
    internal static class DirectMoveStartPatch
    {
        private static readonly FieldInfo CreatureField =
            AccessTools.Field(typeof(MovementController), "_creature")!;

        [HarmonyPrefix]
        private static void Prefix(MovementController __instance)
        {
            if (CreatureField.GetValue(__instance) is VCreature creature)
            {
                WebDashboardHostCheatsRuntime.BeginMoveValidationBypass(creature);
            }
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            WebDashboardHostCheatsRuntime.EndMoveValidationBypass();
        }

        [HarmonyFinalizer]
        private static Exception? Finalizer(Exception? __exception)
        {
            WebDashboardHostCheatsRuntime.EndMoveValidationBypass();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(MovementController), nameof(MovementController.DirectMoveStop))]
    internal static class DirectMoveStopPatch
    {
        private static readonly FieldInfo CreatureField =
            AccessTools.Field(typeof(MovementController), "_creature")!;

        [HarmonyPrefix]
        private static void Prefix(MovementController __instance)
        {
            if (CreatureField.GetValue(__instance) is VCreature creature)
            {
                WebDashboardHostCheatsRuntime.BeginMoveValidationBypass(creature);
            }
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            WebDashboardHostCheatsRuntime.EndMoveValidationBypass();
        }

        [HarmonyFinalizer]
        private static Exception? Finalizer(Exception? __exception)
        {
            WebDashboardHostCheatsRuntime.EndMoveValidationBypass();
            return __exception;
        }
    }
}
