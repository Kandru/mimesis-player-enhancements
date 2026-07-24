using System.Reflection;
using ReluReplay.Shared;

namespace MimesisPlayerEnhancement.Features.Privacy.Patches
{
    // game@0.3.1 Assembly-CSharp/ReluReplay.Shared/ReplaySharedData.cs:L388-391
    [HarmonyPatch]
    internal static class BlockReplaySharedDataSetRecordModePatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(ReplaySharedData), nameof(ReplaySharedData.SetRecordMode))
            ?? throw new InvalidOperationException("ReplaySharedData.SetRecordMode not found");

        [HarmonyPrefix]
        private static bool Prefix()
        {
            return !PrivacyRuntime.ShouldBlockReplayRecording();
        }
    }
}
