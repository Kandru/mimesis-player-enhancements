namespace MimesisPlayerEnhancement.Features.Persistence
{
    internal static class PersistencePatches
    {
        private const string Feature = "Persistence";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(PersistencePatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("Delete/PlatformMgr", AccessTools.Method(typeof(PlatformMgr), nameof(PlatformMgr.Delete))),
                ("SaveGameData/MaintenanceRoom", AccessTools.Method(typeof(MaintenanceRoom), nameof(MaintenanceRoom.SaveGameData))),
                ("ApplyLoadedGameData/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), nameof(GameSessionInfo.ApplyLoadedGameData))),
                ("OnStartClient/SpeechEventArchive", AccessTools.Method(typeof(SpeechEventArchive), "OnStartClient")),
                ("OnStopClient/SpeechEventArchive", AccessTools.Method(typeof(SpeechEventArchive), nameof(SpeechEventArchive.OnStopClient))),
                ("GetRandomOtherSpeechEventArchive/VoiceManager", AccessTools.Method(typeof(VoiceManager), "GetRandomOtherSpeechEventArchive")),
            ]);
        }
    }
}
