using System.Reflection;
using ReluReplay;

namespace MimesisPlayerEnhancement.Features.Privacy.Patches
{
    // game@0.3.1 Assembly-CSharp/ReluReplay/ReplayManager.cs:L264-279
    // game@0.3.1 Assembly-CSharp/ReluReplay/ReplayManager.cs:L44 (_requireFeedbackReplayFile)
    [HarmonyPatch]
    internal static class BlockReplayManagerSetFeedbackUploadedPatch
    {
        private const string Feature = "Privacy";

        private static readonly FieldInfo RequireFeedbackReplayFileField =
            AccessTools.Field(typeof(ReplayManager), "_requireFeedbackReplayFile")
            ?? throw new InvalidOperationException("ReplayManager._requireFeedbackReplayFile not found");

        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(ReplayManager), nameof(ReplayManager.SetFeedbackUploaded))
            ?? throw new InvalidOperationException("ReplayManager.SetFeedbackUploaded not found");

        [HarmonyPostfix]
        private static void Postfix(ReplayManager __instance)
        {
            if (!PrivacyRuntime.ShouldBlockReplayRecording())
            {
                return;
            }

            try
            {
                RequireFeedbackReplayFileField.SetValue(__instance, false);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"BlockReplayManagerSetFeedbackUploadedPatch failed — {ex.Message}");
            }
        }
    }
}
