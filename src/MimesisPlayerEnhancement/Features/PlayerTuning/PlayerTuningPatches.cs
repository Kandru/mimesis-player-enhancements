using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.PlayerTuning
{
    public static class PlayerTuningPatches
    {
        private const string Feature = "PlayerTuning";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(PlayerTuningPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("LoadBaseStats/MappedStats", AccessTools.Method(typeof(MappedStats), nameof(MappedStats.LoadBaseStats))),
                ("OnChangeInventory/InventoryController", AccessTools.Method(typeof(InventoryController), nameof(InventoryController.OnChangeInventory))),
                ("SetControlMode/ProtoActor", AccessTools.Method(typeof(ProtoActor), "SetControlMode")),
                ("SetAsOtherPlayer/ProtoActor", AccessTools.Method(typeof(ProtoActor), nameof(ProtoActor.SetAsOtherPlayer))),
                ("OnActorRevive/ProtoActor", AccessTools.Method(typeof(ProtoActor), nameof(ProtoActor.OnActorRevive))),
            ]);
        }

        [HarmonyPatch(typeof(MappedStats), nameof(MappedStats.LoadBaseStats))]
        public static class MappedStatsLoadBaseStatsPatch
        {
            [HarmonyPostfix]
            public static void Postfix(MappedStats __instance, ActorType type, bool __result)
            {
                try
                {
                    if (!ModConfig.EnablePlayerTuning.Value || !__result || type != ActorType.Player)
                    {
                        return;
                    }

                    PlayerTuningApplier.ApplyMappedPlayerStats(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"LoadBaseStats postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(InventoryController), nameof(InventoryController.OnChangeInventory))]
        public static class InventoryControllerOnChangeInventoryPatch
        {
            [HarmonyPostfix]
            public static void Postfix(InventoryController __instance)
            {
                try
                {
                    if (!ModConfig.EnablePlayerTuning.Value)
                    {
                        return;
                    }

                    PlayerTuningApplier.ApplyInventoryWeightPenalty(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"OnChangeInventory postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(ProtoActor), "SetControlMode")]
        public static class ProtoActorSetControlModePatch
        {
            [HarmonyPostfix]
            public static void Postfix(ProtoActor __instance)
            {
                try
                {
                    PlayerTuningCollision.OnRemotePlayerConfigured(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"SetControlMode postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.SetAsOtherPlayer))]
        public static class ProtoActorSetAsOtherPlayerPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ProtoActor __instance)
            {
                try
                {
                    PlayerTuningCollision.OnRemotePlayerConfigured(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"SetAsOtherPlayer postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.OnActorRevive))]
        public static class ProtoActorOnActorRevivePatch
        {
            [HarmonyPostfix]
            public static void Postfix(ProtoActor __instance)
            {
                try
                {
                    PlayerTuningCollision.OnRemotePlayerConfigured(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"OnActorRevive postfix failed — {ex.Message}");
                }
            }
        }
    }
}
