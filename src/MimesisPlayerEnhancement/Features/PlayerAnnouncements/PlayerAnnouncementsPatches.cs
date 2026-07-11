namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements
{
    internal static class PlayerAnnouncementsPatches
    {
        private const string Feature = "Announcements";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(PlayerAnnouncementsPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("OnAllMemberEntered/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "OnAllMemberEntered")),
                ("OnPlayerDeath/GameMainBase", AccessTools.Method(typeof(GameMainBase), nameof(GameMainBase.OnPlayerDeath))),
                ("OnActorEnter/DungeonRoom", AccessTools.Method(typeof(DungeonRoom), "OnActorEnter")),
            ]);
        }
    }
}
