namespace MimesisPlayerEnhancement.Features.MimicTuning
{
    internal static class MimicTuningPatches
    {
        private const string Feature = "MimicTuning";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(MimicTuningPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("SpawnMimicVoicEventOnce/VoiceManager",
                    AccessTools.Method(typeof(VoiceManager), nameof(VoiceManager.SpawnMimicVoicEventOnce))),
                ("SpawnMimicVoiceWithDelay/VoiceManager",
                    AccessTools.Method(typeof(VoiceManager), "SpawnMimicVoiceWithDelay")),
                ("TrySpawnMimicVoiceEventOnce/MimicVoiceSpawner",
                    AccessTools.Method(typeof(Mimic.Voice.MimicVoiceSpawner), nameof(Mimic.Voice.MimicVoiceSpawner.TrySpawnMimicVoiceEventOnce))),
                ("PickRandomInterval/MimicVoiceSpawner",
                    AccessTools.Method(typeof(Mimic.Voice.MimicVoiceSpawner), "PickRandomInterval")),
                ("TrySpawnVoiceByContext/MimicVoiceSpawner",
                    AccessTools.Method(typeof(Mimic.Voice.MimicVoiceSpawner), "TrySpawnVoiceByContext")),
                ("PickBestMatch/SpeechEventAdditionalGameData",
                    AccessTools.Method(typeof(Mimic.Voice.SpeechSystem.SpeechEventAdditionalGameData), nameof(Mimic.Voice.SpeechSystem.SpeechEventAdditionalGameData.PickBestMatch))),
                ("CopyInventory/AIController",
                    AccessTools.Method(typeof(AIController), nameof(AIController.CopyInventory))),
            ]);
        }
    }
}
