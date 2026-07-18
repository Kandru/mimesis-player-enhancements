using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Privacy
{
    internal static class ReluTelemetryGate
    {
        private const string Feature = "Privacy";

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo CanSendRequestField =
            AccessTools.Field(typeof(APIRequestHandler), "_canSendRequest")
            ?? throw new InvalidOperationException("APIRequestHandler._canSendRequest not found");

        private static readonly PropertyInfo? ApiHandlerProperty =
            typeof(Hub).GetProperty("apihandler", InstanceFlags);

        internal static void ApplyGate(APIRequestHandler handler)
        {
            try
            {
                CanSendRequestField.SetValue(handler, !PrivacyRuntime.BlocksReluTelemetry);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"ApplyGate failed — {ex.Message}");
            }
        }

        internal static void SyncActiveHandler()
        {
            if (Hub.s == null || ApiHandlerProperty?.GetValue(Hub.s) is not APIRequestHandler handler)
            {
                return;
            }

            ApplyGate(handler);
        }

        internal static void RestoreVanilla()
        {
            if (Hub.s == null || ApiHandlerProperty?.GetValue(Hub.s) is not APIRequestHandler handler)
            {
                return;
            }

            try
            {
                CanSendRequestField.SetValue(handler, true);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"RestoreVanilla failed — {ex.Message}");
            }
        }
    }
}
