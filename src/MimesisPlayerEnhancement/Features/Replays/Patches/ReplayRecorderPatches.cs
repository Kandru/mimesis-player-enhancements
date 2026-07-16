using ReluReplay.Data;

namespace MimesisPlayerEnhancement.Features.Replays.Patches
{
    [HarmonyPatch(typeof(ReplayData), "OnStopRecording")]
    internal static class ReplayDataOnStopRecordingPostfix
    {
        private const string Feature = "Replays";

        [HarmonyPostfix]
        private static void Postfix(ReplayData __instance, bool __result)
        {
            if (!__result || !ReplaysRuntime.ShouldKeepLocalReplays())
            {
                return;
            }

            try
            {
                ReplayLibrary.TryPreserveFromRecording(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnStopRecording patch failed — {ex.Message}");
            }
        }
    }
}
