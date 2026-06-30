using System;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    public static class DungeonTimePatches
    {
        private const string Feature = "DungeonTime";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(DungeonTimePatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("OnAllMemberEntered/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "OnAllMemberEntered")),
            ]);
        }

        [HarmonyPatch(typeof(DungeonRoom), "OnAllMemberEntered")]
        public static class DungeonRoomOnAllMemberEnteredPatch
        {
            [HarmonyPostfix]
            public static void Postfix(DungeonRoom __instance)
            {
                try
                {
                    DungeonTimeApplier.EnsureApplied(__instance);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"OnAllMemberEntered postfix failed — {ex.Message}");
                }
            }
        }
    }
}
