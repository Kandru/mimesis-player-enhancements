namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    internal static class DungeonRandomizerPatches
    {
        private const string Feature = "DungeonRandomizer";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(DungeonRandomizerPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("PickDungeon/ExcelDataManager", AccessTools.Method(typeof(ExcelDataManager), nameof(ExcelDataManager.PickDungeon))),
                ("RollDiceDungeon/VWaitingRoom", AccessTools.Method(typeof(VWaitingRoom), nameof(VWaitingRoom.RollDiceDungeon))),
                ("GetRandomDungenName/DungeonMasterInfo", AccessTools.Method(typeof(DungeonMasterInfo), nameof(DungeonMasterInfo.GetRandomDungenName))),
                ("PickMapID/DungeonMasterInfo", AccessTools.Method(typeof(DungeonMasterInfo), nameof(DungeonMasterInfo.PickMapID))),
                ("SetNextDungeonMasterID/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), nameof(GameSessionInfo.SetNextDungeonMasterID))),
            ]);
        }
    }
}
