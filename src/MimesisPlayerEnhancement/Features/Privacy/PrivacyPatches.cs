using ReluReplay;
using ReluReplay.Recorder;
using ReluReplay.Shared;

namespace MimesisPlayerEnhancement.Features.Privacy
{
    internal static class PrivacyPatches
    {
        private const string Feature = "Privacy";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(PrivacyPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("EnqueueAPI/APIRequestHandler", AccessTools.Method(typeof(APIRequestHandler), nameof(APIRequestHandler.EnqueueAPI))),
                ("SetRecordMode/ReplaySharedData", AccessTools.Method(typeof(ReplaySharedData), nameof(ReplaySharedData.SetRecordMode))),
                ("UseRecord/ReplayRecorder", AccessTools.PropertyGetter(typeof(ReplayRecorder), nameof(ReplayRecorder.UseRecord))),
                ("ReadyRecording/ReplayRecorder", AccessTools.Method(typeof(ReplayRecorder), nameof(ReplayRecorder.ReadyRecording))),
                ("ReadyRecordingForDeathMatch/ReplayRecorder", AccessTools.Method(typeof(ReplayRecorder), nameof(ReplayRecorder.ReadyRecordingForDeathMatch))),
                ("StartRecording/ReplayRecorder", AccessTools.Method(typeof(ReplayRecorder), "StartRecording")),
                ("CopyReplayToFeedbackFiles/ReplayRecorder", AccessTools.Method(typeof(ReplayRecorder), "CopyReplayToFeedbackFiles")),
                ("UploadReplayDataToStorage/ReplayRecorder", AccessTools.Method(typeof(ReplayRecorder), "UploadReplayDataToStorage")),
                ("UploadReplayDataToStorageSync/ReplayRecorder", AccessTools.Method(typeof(ReplayRecorder), "UploadReplayDataToStorageSync")),
                ("UploadReplayFiles/ReplayRecorder", AccessTools.Method(typeof(ReplayRecorder), nameof(ReplayRecorder.UploadReplayFiles))),
                ("UploadFeedbackReplayFile(string)/ReplayRecorder", AccessTools.Method(typeof(ReplayRecorder), "UploadFeedbackReplayFile", [typeof(string), typeof(string)])),
                ("UploadFeedbackReplayFile(bytes)/ReplayRecorder", AccessTools.Method(typeof(ReplayRecorder), "UploadFeedbackReplayFile", [typeof(byte[]), typeof(string)])),
                ("UploadSavedFeedbackFiles/ReplayRecorder", AccessTools.Method(typeof(ReplayRecorder), "UploadSavedFeedbackFiles")),
                ("SetFeedbackUploaded/ReplayManager", AccessTools.Method(typeof(ReplayManager), nameof(ReplayManager.SetFeedbackUploaded))),
                ("SetUserMetadata/CrashReportHandler", PrivacyCrashReportHelper.SetUserMetadata),
                ("Initialize/KOSManager", AccessTools.Method(typeof(KOSManager), nameof(KOSManager.Initialize))),
            ]);
        }
    }
}
