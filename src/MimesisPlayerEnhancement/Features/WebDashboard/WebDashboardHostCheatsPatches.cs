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
                VCreature? bypassCreature = WebDashboardHostCheatsRuntime.MoveValidationCreature;
                if (bypassCreature != null
                    && bypassCreature.VRoom == __instance
                    && WebDashboardHostCheatsRuntime.IsNoClipActive(bypassCreature))
                {
                    __result = true;
                    return false;
                }

                if (!WebDashboardHostCheatsRuntime.IsNoClipActiveInRoom(__instance))
                {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(MovementController), nameof(MovementController.DirectMoveStart))]
        internal static class DirectMoveStartPatch
        {
            private static readonly FieldInfo CreatureField =
                AccessTools.Field(typeof(MovementController), "_creature")!;

            private static void Prefix(MovementController __instance)
            {
                if (CreatureField.GetValue(__instance) is VCreature creature)
                {
                    WebDashboardHostCheatsRuntime.BeginMoveValidationBypass(creature);
                }
            }

            private static void Postfix()
            {
                WebDashboardHostCheatsRuntime.EndMoveValidationBypass();
            }

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

            private static void Prefix(MovementController __instance)
            {
                if (CreatureField.GetValue(__instance) is VCreature creature)
                {
                    WebDashboardHostCheatsRuntime.BeginMoveValidationBypass(creature);
                }
            }

            private static void Postfix()
            {
                WebDashboardHostCheatsRuntime.EndMoveValidationBypass();
            }

            private static Exception? Finalizer(Exception? __exception)
            {
                WebDashboardHostCheatsRuntime.EndMoveValidationBypass();
                return __exception;
            }
        }

        [HarmonyPatch(typeof(NetworkManagerV2), "HandleGlobalPacket")]
        internal static class NoClipSyncPacketPatch
        {
            private static void Postfix(IMsg msg, ref bool __result)
            {
                if (!__result || msg is not AdminCommandRes response)
                {
                    return;
                }

                WebDashboardHostCheatsNoClipSync.TryHandleAdminCommandRes(response);
            }
        }

        [HarmonyPatch(typeof(ProtoActor), "UpdateControl")]
        internal static class NoClipUpdateControlPatch
        {
            private static bool Prefix(ProtoActor __instance)
            {
                if (WebDashboardHostCheatsRuntime.IsRoomTransitionSuspended
                    || !WebDashboardHostCheatsNoClipMovement.ShouldReplaceControl(__instance))
                {
                    return true;
                }

                try
                {
                    WebDashboardHostCheatsNoClipMovement.Apply(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Noclip movement failed — {ex.Message}");
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.PendMoveToDungeon))]
        internal static class PendMoveToDungeonCheatsPatch
        {
            private static void Prefix() =>
                WebDashboardHostCheatsRuntime.BeginRoomTransition("tram to dungeon");
        }

        [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.PendMoveToWaitingRoom))]
        internal static class PendMoveToWaitingRoomCheatsPatch
        {
            private static void Prefix() =>
                WebDashboardHostCheatsRuntime.BeginRoomTransition("maintenance to tram");
        }

        [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.PendMoveToMaintenance))]
        internal static class PendMoveToMaintenanceCheatsPatch
        {
            private static void Prefix() =>
                WebDashboardHostCheatsRuntime.BeginRoomTransition("dungeon to maintenance");
        }

        [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.PendMoveToDeathMatch))]
        internal static class PendMoveToDeathMatchCheatsPatch
        {
            private static void Prefix() =>
                WebDashboardHostCheatsRuntime.BeginRoomTransition("dungeon to deathmatch");
        }

        [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.OnEnterDungeon))]
        internal static class OnEnterDungeonCheatsPatch
        {
            private static void Postfix() =>
                WebDashboardHostCheatsRuntime.EndRoomTransition("dungeon entered");
        }

        [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.OnEndDungeon))]
        internal static class OnEndDungeonCheatsPatch
        {
            private static void Prefix() =>
                WebDashboardHostCheatsRuntime.BeginRoomTransition("leaving dungeon");
        }

        [HarmonyPatch]
        internal static class InTramWaitingSceneReadyCheatsPatch
        {
            private static MethodBase TargetMethod() =>
                AccessTools.Method(typeof(InTramWaitingScene), "Start")
                    ?? throw new InvalidOperationException("InTramWaitingScene.Start not found");

            private static void Postfix() =>
                WebDashboardHostCheatsRuntime.EndRoomTransition("tram entered");
        }

        [HarmonyPatch(typeof(MaintenanceScene), "TryInitHostMaintenenceRoom")]
        internal static class MaintenanceSceneReadyCheatsPatch
        {
            private static void Postfix() =>
                WebDashboardHostCheatsRuntime.EndRoomTransition("maintenance entered");
        }
    }
}
