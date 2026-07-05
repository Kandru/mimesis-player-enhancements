namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures
{
    internal static class DeadPlayerFeaturesPatches
    {
        private const string Feature = "DeadPlayerFeatures";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(DeadPlayerFeaturesPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("OnEnterDungeon/CameraManager",
                    AccessTools.Method(typeof(CameraManager), nameof(CameraManager.OnEnterDungeon))),
                ("OnEndDungeon/CameraManager",
                    AccessTools.Method(typeof(CameraManager), nameof(CameraManager.OnEndDungeon))),
                ("HandleStartPossessing/PossessionController",
                    AccessTools.Method(typeof(PossessionController), nameof(PossessionController.HandleStartPossessing))),
                ("ClearPossessingStateInternal/PossessionController",
                    AccessTools.Method(typeof(PossessionController), "ClearPossessingStateInternal")),
                ("UpdatePossessionProgressbar/ProtoActor",
                    AccessTools.Method(typeof(ProtoActor), nameof(ProtoActor.UpdatePossessionProgressbar))),
                ("Start/UIPrefab_Spectator",
                    AccessTools.Method(typeof(UIPrefab_Spectator), "Start")),
                ("UpdatePossessionCooltime/UIPrefab_Spectator",
                    AccessTools.Method(typeof(UIPrefab_Spectator), nameof(UIPrefab_Spectator.UpdatePossessionCooltime))),
                ("OnEndPossession/ProtoActor",
                    AccessTools.Method(typeof(ProtoActor), nameof(ProtoActor.OnEndPossession))),
            ]);
        }
    }
}
