namespace MimesisPlayerEnhancement.Features.PlayerTuning
{
    internal static class PlayerTuningPatches
    {
        private const string Feature = "PlayerTuning";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(PlayerTuningPatches)));

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
                ("SetAsMonster/ProtoActor", AccessTools.Method(typeof(ProtoActor), nameof(ProtoActor.SetAsMonster))),
                ("SetupMonsterCapsuleCollider/ProtoActor", AccessTools.Method(typeof(ProtoActor), "SetupMonsterCapsuleCollider")),
                ("OnActorRevive/ProtoActor", AccessTools.Method(typeof(ProtoActor), nameof(ProtoActor.OnActorRevive))),
            ]);
        }
    }
}
