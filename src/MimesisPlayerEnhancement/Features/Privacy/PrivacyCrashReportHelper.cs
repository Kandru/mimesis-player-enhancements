using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Privacy
{
    internal static class PrivacyCrashReportHelper
    {
        private static readonly Type? CrashReportHandlerType =
            AccessTools.TypeByName("UnityEngine.CrashReportHandler.CrashReportHandler");

        private static readonly PropertyInfo? EnableProperty =
            CrashReportHandlerType?.GetProperty("enable", BindingFlags.Public | BindingFlags.Static);

        private static readonly MethodInfo? SetUserMetadataMethod =
            CrashReportHandlerType?.GetMethod(
                "SetUserMetadata",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(string), typeof(string)],
                modifiers: null);

        internal static bool IsAvailable => CrashReportHandlerType != null;

        internal static void SetEnabled(bool enabled)
        {
            if (EnableProperty == null)
            {
                return;
            }

            EnableProperty.SetValue(null, enabled);
        }

        internal static MethodInfo? SetUserMetadata => SetUserMetadataMethod;
    }
}
