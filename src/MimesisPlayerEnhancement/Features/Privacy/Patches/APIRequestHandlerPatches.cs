using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Privacy.Patches
{
    // game@0.3.1 Assembly-CSharp/APIRequestHandler.cs:L41-45
    [HarmonyPatch]
    internal static class BlockReluTelemetryAwakePatch
    {
        private const string Feature = "Privacy";

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
                ModLog.Warn(Feature, $"BlockReluTelemetryAwakePatch failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/APIRequestHandler.cs:L67-77
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
