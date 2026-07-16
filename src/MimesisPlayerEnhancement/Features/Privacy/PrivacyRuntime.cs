namespace MimesisPlayerEnhancement.Features.Privacy
{
    internal static class PrivacyRuntime
    {
        private const string Feature = "Privacy";

        private static bool _loggedReluTelemetry;
        private static bool _loggedReplayUpload;
        private static bool _loggedReplayRecording;
        private static bool _loggedCrashReports;
        private static bool _loggedCrashMetadata;
        private static bool _loggedGppSdk;

        internal static bool IsPrivacyEnabled => ModConfig.EnablePrivacy.Value;

        internal static bool ShouldBlockReluTelemetry()
        {
            bool block = IsPrivacyEnabled && ModConfig.BlockReluTelemetry.Value;
            if (block)
            {
                LogOnce(ref _loggedReluTelemetry, "Relu telemetry blocked — session logs and gameplay event logs will not be sent.");
            }

            return block;
        }

        internal static bool ShouldBlockReplayUpload()
        {
            bool block = IsPrivacyEnabled && ModConfig.BlockReplayUpload.Value;
            if (block)
            {
                LogOnce(ref _loggedReplayUpload, "Replay upload blocked — replay files will not be sent to Relu storage.");
            }

            return block;
        }

        internal static bool ShouldBlockReplayRecording()
        {
            if (ModConfig.EnableReplays.Value)
            {
                return false;
            }

            bool block = IsPrivacyEnabled && ModConfig.BlockReplayRecording.Value;
            if (block)
            {
                LogOnce(ref _loggedReplayRecording, "Replay recording blocked — no replay files will be created.");
            }

            return block;
        }

        internal static bool ShouldBlockCrashReports() =>
            IsPrivacyEnabled && ModConfig.BlockCrashReports.Value;

        internal static bool ShouldStripCrashReportMetadata()
        {
            bool strip = IsPrivacyEnabled && ModConfig.StripCrashReportMetadata.Value;
            if (strip)
            {
                LogOnce(ref _loggedCrashMetadata, "Crash report metadata stripped — SetUserMetadata calls are ignored.");
            }

            return strip;
        }

        internal static bool ShouldBlockKraftonGppSdk()
        {
            bool block = IsPrivacyEnabled && ModConfig.BlockKraftonGppSdk.Value;
            if (block)
            {
                LogOnce(ref _loggedGppSdk, "Krafton GPP SDK blocked — creator-code login will not run.");
            }

            return block;
        }

        internal static void RefreshFromConfig()
        {
            if (!IsPrivacyEnabled)
            {
                PrivacyCrashReportHelper.SetEnabled(true);
                ReluTelemetryGate.SyncActiveHandler();
                ResetLogFlags();
                return;
            }

            ReluTelemetryGate.SyncActiveHandler();

            bool blockCrashReports = ShouldBlockCrashReports();
            PrivacyCrashReportHelper.SetEnabled(!blockCrashReports);
            if (blockCrashReports)
            {
                LogOnce(ref _loggedCrashReports, "Unity crash reports disabled.");
            }
            else
            {
                _loggedCrashReports = false;
            }
        }

        private static void ResetLogFlags()
        {
            _loggedReluTelemetry = false;
            _loggedReplayUpload = false;
            _loggedReplayRecording = false;
            _loggedCrashReports = false;
            _loggedCrashMetadata = false;
            _loggedGppSdk = false;
        }

        private static void LogOnce(ref bool flag, string message)
        {
            if (flag)
            {
                return;
            }

            flag = true;
            ModLog.Info(Feature, message);
        }
    }
}
