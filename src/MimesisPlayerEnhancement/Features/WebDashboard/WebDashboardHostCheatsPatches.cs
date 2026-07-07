using System.Reflection;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardHostCheatsPatches
    {
        private const string Feature = "WebDashboard";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = HarmonyPatchHelper.ApplyPatchTypes(harmony, Feature, HarmonyPatchHelper.GetNestedPatchTypes(typeof(WebDashboardHostCheatsPatches)));
        }

        [HarmonyPatch(typeof(VCreature), nameof(VCreature.ForcedDying))]
        internal static class BlockForcedDyingPatch
        {
            private static bool Prefix(VCreature __instance)
            {
                return __instance is not VPlayer player || !WebDashboardHostCheatsRuntime.IsGodModeActive(player);
            }
        }

        [HarmonyPatch(typeof(MovementController), nameof(MovementController.CheckFallDamage))]
        internal static class CheckFallDamagePatch
        {
            private static readonly FieldInfo CreatureField =
                AccessTools.Field(typeof(MovementController), "_creature")!;

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

        [HarmonyPatch]
        internal static class ValidateMoveSpeedPatch
        {
            private static readonly FieldInfo OwnerField =
                AccessTools.Field("MovementController+IMoveContext:m_Owner")
                    ?? throw new InvalidOperationException("IMoveContext.m_Owner not found");

            private static MethodBase TargetMethod()
            {
                return AccessTools.Method("MovementController+DirectMoveContext:ValidateMoveSpped")
                    ?? throw new InvalidOperationException("ValidateMoveSpped not found");
            }

            private static bool Prefix(object __instance, ref bool __result)
            {
                VCreature? owner = OwnerField.GetValue(__instance) as VCreature;
                if (owner != null && WebDashboardHostCheatsRuntime.IsNoClipActive(owner))
                {
                    __result = true;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch]
        internal static class DirectMoveStartMovePatch
        {
            private static readonly FieldInfo OwnerField =
                AccessTools.Field("MovementController+IMoveContext:m_Owner")
                    ?? throw new InvalidOperationException("IMoveContext.m_Owner not found");

            private static MethodBase TargetMethod()
            {
                return AccessTools.Method("MovementController+DirectMoveContext:StartMove")
                    ?? throw new InvalidOperationException("DirectMoveContext.StartMove not found");
            }

            private static void Prefix(object __instance, ref PosWithRot prevPos, ref PosWithRot currPos)
            {
                if (OwnerField.GetValue(__instance) is not VCreature owner
                    || !WebDashboardHostCheatsRuntime.IsNoClipActive(owner))
                {
                    return;
                }

                prevPos = currPos.Clone();
            }
        }

        [HarmonyPatch]
        internal static class DirectMoveStopMovePatch
        {
            private static readonly FieldInfo OwnerField =
                AccessTools.Field("MovementController+IMoveContext:m_Owner")
                    ?? throw new InvalidOperationException("IMoveContext.m_Owner not found");

            private static MethodBase TargetMethod()
            {
                return AccessTools.Method("MovementController+DirectMoveContext:OnMoveStopReq")
                    ?? throw new InvalidOperationException("DirectMoveContext.OnMoveStopReq not found");
            }

            private static void Prefix(object __instance, ref PosWithRot prevPos, ref PosWithRot currPos)
            {
                if (OwnerField.GetValue(__instance) is not VCreature owner
                    || !WebDashboardHostCheatsRuntime.IsNoClipActive(owner))
                {
                    return;
                }

                prevPos = currPos.Clone();
            }
        }

        [HarmonyPatch(typeof(IVroom), nameof(IVroom.ValidPosition))]
        internal static class ValidPositionPatch
        {
            private static bool Prefix(IVroom __instance, ref bool __result)
            {
                if (!WebDashboardHostCheatsRuntime.NoClipEnabled
                    || !WebDashboardHostCheatsRuntime.TryGetHostVPlayer(out VPlayer? player)
                    || player == null
                    || player.VRoom != __instance)
                {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(ProtoActor), "UpdateControl")]
        internal static class NoClipUpdateControlPatch
        {
            private static bool Prefix(ProtoActor __instance)
            {
                if (!WebDashboardHostCheatsNoClipMovement.ShouldReplaceControl(__instance))
                {
                    return true;
                }

                WebDashboardHostCheatsNoClipMovement.Apply(__instance);
                return false;
            }
        }
    }
}
