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
                ("TrySpawnMimicReply/VoiceManager",
                    AccessTools.Method(typeof(VoiceManager), nameof(VoiceManager.TrySpawnMimicReply))),
                ("SpawnMimicVoiceAfterDelay/VoiceManager",
                    AccessTools.Method(typeof(VoiceManager), "SpawnMimicVoiceAfterDelay")),
                ("PrepareNearbyMimicReplies/MimicVoiceSpawner",
                    AccessTools.Method(typeof(Mimic.Voice.MimicVoiceSpawner), nameof(Mimic.Voice.MimicVoiceSpawner.PrepareNearbyMimicReplies))),
                ("SpawnPreparedMimicVoice/MimicVoiceSpawner",
                    AccessTools.Method(typeof(Mimic.Voice.MimicVoiceSpawner), nameof(Mimic.Voice.MimicVoiceSpawner.SpawnPreparedMimicVoice))),
                ("PickRandomInterval/MimicVoiceSpawner",
                    AccessTools.Method(typeof(Mimic.Voice.MimicVoiceSpawner), "PickRandomInterval")),
                ("PickBestMatch/SpeechEventAdditionalGameData",
                    AccessTools.Method(typeof(Mimic.Voice.SpeechSystem.SpeechEventAdditionalGameData), nameof(Mimic.Voice.SpeechSystem.SpeechEventAdditionalGameData.PickBestMatch))),
                ("ObserverRpcPlayOnActor/SpeechEventArchive",
                    AccessTools.Method(typeof(Mimic.Voice.SpeechSystem.SpeechEventArchive), "ObserverRpcPlayOnActor")),
                ("OnEnable/DLDecisionAgent",
                    AccessTools.Method(typeof(DLAgent.DLDecisionAgent), "OnEnable")),
                ("PlayMimicRandomHorn/AIController",
                    AccessTools.Method(typeof(AIController), nameof(AIController.PlayMimicRandomHorn))),
                ("OnEnterDungeon/CameraManager",
                    AccessTools.Method(typeof(CameraManager), nameof(CameraManager.OnEnterDungeon))),
                ("IsPossessable/PossessionController",
                    AccessTools.Method(typeof(PossessionController), "IsPossessable")),
                ("CopyInventory/AIController",
                    AccessTools.Method(typeof(AIController), nameof(AIController.CopyInventory))),
                ("SetPossession/ProtoActor",
                    AccessTools.Method(typeof(ProtoActor), nameof(ProtoActor.SetPossession))),
                ("PlayVoiceOnActor/ProtoActor",
                    AccessTools.Method(typeof(ProtoActor), nameof(ProtoActor.PlayVoiceOnActor))),
            ]);
        }
    }
}
