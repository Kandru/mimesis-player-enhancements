namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal static class DungeonTimePatches
    {
        private const string Feature = "DungeonTime";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(DungeonTimePatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            // game@0.3.1 Assembly-CSharp/DungeonRoom.cs:L1018-1021
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("OnAllMemberEntered/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "OnAllMemberEntered")),
            ]);
        }
    }
}
