namespace MimesisPlayerEnhancement.Config
{
    internal static class SaveSlotSidecarPersistencePatches
    {
        private const string Feature = "SaveSlotSidecar";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(SaveSlotSidecarPersistencePatches)));

            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("ApplyLoadedGameData/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), nameof(GameSessionInfo.ApplyLoadedGameData))),
                ("SaveGameData/MaintenanceRoom", AccessTools.Method(typeof(MaintenanceRoom), nameof(MaintenanceRoom.SaveGameData))),
            ]);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }
    }
}
