using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Privacy
{
    internal static class ReluTelemetryGate
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo CanSendRequestField =
            AccessTools.Field(typeof(APIRequestHandler), "_canSendRequest")
            ?? throw new InvalidOperationException("APIRequestHandler._canSendRequest not found");

        private static readonly PropertyInfo? ApiHandlerProperty =
            typeof(Hub).GetProperty("apihandler", InstanceFlags);

        internal static void ApplyGate(APIRequestHandler handler)
        {
            CanSendRequestField.SetValue(handler, !PrivacyRuntime.ShouldBlockReluTelemetry());
        }

        internal static void SyncActiveHandler()
        {
            if (Hub.s == null || ApiHandlerProperty?.GetValue(Hub.s) is not APIRequestHandler handler)
            {
                return;
            }

            ApplyGate(handler);
        }
    }
}
