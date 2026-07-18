using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Privacy.Patches
{
    [HarmonyPatch]
    internal static class BlockKraftonGppSdkPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(KOSManager), nameof(KOSManager.Initialize))
            ?? throw new InvalidOperationException("KOSManager.Initialize not found");

        [HarmonyPrefix]
        private static bool Prefix()
        {
            return !PrivacyRuntime.ShouldBlockKraftonGppSdk();
        }
    }
}
