using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Privacy.Patches
{
    // UnityEngine.CrashReportHandler.CrashReportHandler.SetUserMetadata (CrashReportHandlerModule)
    [HarmonyPatch]
    internal static class BlockCrashReportMetadataPatch
    {
        private static MethodBase? TargetMethod() => PrivacyCrashReportHelper.SetUserMetadata;

        [HarmonyPrefix]
        private static bool Prefix()
        {
            return !PrivacyRuntime.ShouldStripCrashReportMetadata();
        }
    }
}
