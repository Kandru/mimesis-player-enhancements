using System.Reflection;

namespace MimesisPlayerEnhancement.Features.WebDashboard.Patches
{
    // game@0.3.1 Assembly-CSharp/MovementController.cs:L53-63
    [HarmonyPatch]
    internal static class ValidateMoveSpeedPatch
    {
        private static readonly FieldInfo OwnerField =
            AccessTools.Field("MovementController+IMoveContext:m_Owner")
                ?? throw new InvalidOperationException("IMoveContext.m_Owner not found");

        private static MethodBase TargetMethod() =>
            AccessTools.Method("MovementController+DirectMoveContext:ValidateMoveSpped")
                ?? throw new InvalidOperationException("ValidateMoveSpped not found");

        [HarmonyPrefix]
        private static bool Prefix(object __instance, ref bool __result)
        {
            if (!WebDashboardHostCheatsRuntime.HasActiveNoClip)
            {
                return true;
            }

            VCreature? owner = OwnerField.GetValue(__instance) as VCreature;
            if (owner != null && WebDashboardHostCheatsRuntime.IsNoClipActive(owner))
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    // game@0.3.1 Assembly-CSharp/MovementController.cs:L106-173
    [HarmonyPatch]
    internal static class DirectMoveStartMovePatch
    {
        private static readonly FieldInfo OwnerField =
            AccessTools.Field("MovementController+IMoveContext:m_Owner")
                ?? throw new InvalidOperationException("IMoveContext.m_Owner not found");

        private static MethodBase TargetMethod() =>
            AccessTools.Method("MovementController+DirectMoveContext:StartMove")
                ?? throw new InvalidOperationException("DirectMoveContext.StartMove not found");

        [HarmonyPrefix]
        private static void Prefix(object __instance, ref PosWithRot prevPos, ref PosWithRot currPos)
        {
            if (!WebDashboardHostCheatsRuntime.HasActiveNoClip)
            {
                return;
            }

            if (OwnerField.GetValue(__instance) is not VCreature owner
                || !WebDashboardHostCheatsRuntime.IsNoClipActive(owner))
            {
                return;
            }

            prevPos = currPos.Clone();
        }
    }

    // game@0.3.1 Assembly-CSharp/MovementController.cs:L76-104
    [HarmonyPatch]
    internal static class DirectMoveStopMovePatch
    {
        private static readonly FieldInfo OwnerField =
            AccessTools.Field("MovementController+IMoveContext:m_Owner")
                ?? throw new InvalidOperationException("IMoveContext.m_Owner not found");

        private static MethodBase TargetMethod() =>
            AccessTools.Method("MovementController+DirectMoveContext:OnMoveStopReq")
                ?? throw new InvalidOperationException("DirectMoveContext.OnMoveStopReq not found");

        [HarmonyPrefix]
        private static void Prefix(object __instance, ref PosWithRot prevPos, ref PosWithRot currPos)
        {
            if (!WebDashboardHostCheatsRuntime.HasActiveNoClip)
            {
                return;
            }

            if (OwnerField.GetValue(__instance) is not VCreature owner
                || !WebDashboardHostCheatsRuntime.IsNoClipActive(owner))
            {
                return;
            }

            prevPos = currPos.Clone();
        }
    }
}
