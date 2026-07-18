using System.Reflection;
using Cysharp.Threading.Tasks;
using ReluReplay.Recorder;

namespace MimesisPlayerEnhancement.Features.Privacy.Patches
{
    internal static class ReplayRecorderUploadBlockHelper
    {
        internal static bool TryBlockUpload(ref UniTask result)
        {
            if (!PrivacyRuntime.ShouldBlockReplayUpload())
            {
                return false;
            }

            result = UniTask.CompletedTask;
            return true;
        }
    }

    [HarmonyPatch]
    internal static class BlockReplayRecorderUseRecordPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.PropertyGetter(typeof(ReplayRecorder), nameof(ReplayRecorder.UseRecord))
            ?? throw new InvalidOperationException("ReplayRecorder.UseRecord getter not found");

        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (PrivacyRuntime.ShouldBlockReplayRecording())
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch]
    internal static class BlockReplayRecorderReadyRecordingPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(ReplayRecorder), nameof(ReplayRecorder.ReadyRecording))
            ?? throw new InvalidOperationException("ReplayRecorder.ReadyRecording not found");

        [HarmonyPrefix]
        private static bool Prefix()
        {
            return !PrivacyRuntime.ShouldBlockReplayRecording();
        }
    }

    [HarmonyPatch]
    internal static class BlockReplayRecorderReadyRecordingForDeathMatchPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(ReplayRecorder), nameof(ReplayRecorder.ReadyRecordingForDeathMatch))
            ?? throw new InvalidOperationException("ReplayRecorder.ReadyRecordingForDeathMatch not found");

        [HarmonyPrefix]
        private static bool Prefix()
        {
            return !PrivacyRuntime.ShouldBlockReplayRecording();
        }
    }

    [HarmonyPatch]
    internal static class BlockReplayRecorderStartRecordingPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(ReplayRecorder), "StartRecording")
            ?? throw new InvalidOperationException("ReplayRecorder.StartRecording not found");

        [HarmonyPrefix]
        private static bool Prefix()
        {
            return !PrivacyRuntime.ShouldBlockReplayRecording();
        }
    }

    [HarmonyPatch]
    internal static class BlockReplayRecorderCopyReplayToFeedbackFilesPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(ReplayRecorder), "CopyReplayToFeedbackFiles")
            ?? throw new InvalidOperationException("ReplayRecorder.CopyReplayToFeedbackFiles not found");

        [HarmonyPrefix]
        private static bool Prefix()
        {
            return !PrivacyRuntime.ShouldBlockReplayRecording();
        }
    }

    [HarmonyPatch]
    internal static class BlockReplayRecorderUploadReplayDataToStoragePatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(ReplayRecorder), "UploadReplayDataToStorage")
            ?? throw new InvalidOperationException("ReplayRecorder.UploadReplayDataToStorage not found");

        [HarmonyPrefix]
        private static bool Prefix(ref UniTask __result) =>
            !ReplayRecorderUploadBlockHelper.TryBlockUpload(ref __result);
    }

    [HarmonyPatch]
    internal static class BlockReplayRecorderUploadReplayDataToStorageSyncPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(ReplayRecorder), "UploadReplayDataToStorageSync")
            ?? throw new InvalidOperationException("ReplayRecorder.UploadReplayDataToStorageSync not found");

        [HarmonyPrefix]
        private static bool Prefix()
        {
            return !PrivacyRuntime.ShouldBlockReplayUpload();
        }
    }

    [HarmonyPatch]
    internal static class BlockReplayRecorderUploadReplayFilesPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(ReplayRecorder), nameof(ReplayRecorder.UploadReplayFiles))
            ?? throw new InvalidOperationException("ReplayRecorder.UploadReplayFiles not found");

        [HarmonyPrefix]
        private static bool Prefix(ref UniTask __result) =>
            !ReplayRecorderUploadBlockHelper.TryBlockUpload(ref __result);
    }

    [HarmonyPatch]
    internal static class BlockReplayRecorderUploadSavedFeedbackFilesPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(ReplayRecorder), "UploadSavedFeedbackFiles")
            ?? throw new InvalidOperationException("ReplayRecorder.UploadSavedFeedbackFiles not found");

        [HarmonyPrefix]
        private static bool Prefix(ref UniTask __result) =>
            !ReplayRecorderUploadBlockHelper.TryBlockUpload(ref __result);
    }

    [HarmonyPatch]
    internal static class BlockReplayRecorderUploadFeedbackReplayFilePathPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(ReplayRecorder), "UploadFeedbackReplayFile", [typeof(string), typeof(string)])
            ?? throw new InvalidOperationException("ReplayRecorder.UploadFeedbackReplayFile(string,string) not found");

        [HarmonyPrefix]
        private static bool Prefix(ref UniTask __result) =>
            !ReplayRecorderUploadBlockHelper.TryBlockUpload(ref __result);
    }

    [HarmonyPatch]
    internal static class BlockReplayRecorderUploadFeedbackReplayFileBytesPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(ReplayRecorder), "UploadFeedbackReplayFile", [typeof(byte[]), typeof(string)])
            ?? throw new InvalidOperationException("ReplayRecorder.UploadFeedbackReplayFile(byte[],string) not found");

        [HarmonyPrefix]
        private static bool Prefix(ref UniTask __result) =>
            !ReplayRecorderUploadBlockHelper.TryBlockUpload(ref __result);
    }
}
