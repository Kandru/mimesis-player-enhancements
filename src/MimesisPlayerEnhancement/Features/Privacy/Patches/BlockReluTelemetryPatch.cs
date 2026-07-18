using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Privacy.Patches
{
    [HarmonyPatch]
    internal static class BlockReluTelemetryAwakePatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(APIRequestHandler), "Awake")
            ?? throw new InvalidOperationException("APIRequestHandler.Awake not found");

        [HarmonyPostfix]
        private static void Postfix(APIRequestHandler __instance)
        {
            try
            {
                ReluTelemetryGate.ApplyGate(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn("Privacy", $"BlockReluTelemetryAwakePatch failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    internal static class BlockReluTelemetryUpdatePatch
    {
        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(APIRequestHandler), "Update")
            ?? throw new InvalidOperationException("APIRequestHandler.Update not found");

        [HarmonyPrefix]
        private static bool Prefix()
        {
            return !PrivacyRuntime.BlocksReluTelemetry;
        }
    }
}
