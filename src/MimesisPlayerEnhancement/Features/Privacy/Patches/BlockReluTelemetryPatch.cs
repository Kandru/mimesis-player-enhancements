using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Privacy.Patches
{
    [HarmonyPatch]
    internal static class BlockReluTelemetryPatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(APIRequestHandler), nameof(APIRequestHandler.EnqueueAPI))
            ?? throw new InvalidOperationException("APIRequestHandler.EnqueueAPI not found");

        [HarmonyPrefix]
        private static bool Prefix()
        {
            if (!PrivacyRuntime.ShouldBlockReluTelemetry())
            {
                return true;
            }

            return false;
        }
    }
}
