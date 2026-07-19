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

        private static bool _blockReluTelemetry;
        private static bool _blockReplayUpload;
        private static bool _blockReplayRecording;
        private static bool _blockCrashReports;
        private static bool _stripCrashReportMetadata;
        private static bool _blockKraftonGppSdk;

        internal static bool IsPrivacyEnabled => ModConfig.EnablePrivacy.Value;

        internal static bool BlocksReluTelemetry => _blockReluTelemetry;

        internal static bool ShouldBlockReluTelemetry()
        {
            if (_blockReluTelemetry)
            {
                LogOnce(ref _loggedReluTelemetry, "Relu telemetry blocked — session logs and gameplay event logs will not be sent.");
            }

            return _blockReluTelemetry;
        }

        internal static bool ShouldBlockReplayUpload()
        {
            if (_blockReplayUpload)
            {
                LogOnce(ref _loggedReplayUpload, "Replay upload blocked — replay files will not be sent to Relu storage.");
            }

            return _blockReplayUpload;
        }

        internal static bool ShouldBlockReplayRecording()
        {
            if (_blockReplayRecording)
            {
                LogOnce(ref _loggedReplayRecording, "Replay recording blocked — no replay files will be created.");
            }

            return _blockReplayRecording;
        }

        internal static bool ShouldBlockCrashReports() => _blockCrashReports;

        internal static bool ShouldStripCrashReportMetadata()
        {
            if (_stripCrashReportMetadata)
            {
                LogOnce(ref _loggedCrashMetadata, "Crash report metadata stripped — SetUserMetadata calls are ignored.");
            }

            return _stripCrashReportMetadata;
        }

        internal static bool ShouldBlockKraftonGppSdk()
        {
            if (_blockKraftonGppSdk)
            {
                LogOnce(ref _loggedGppSdk, "Krafton GPP SDK blocked — creator-code login will not run.");
            }

            return _blockKraftonGppSdk;
        }

        internal static void RefreshFromConfig()
        {
            RefreshCache();

            if (!IsPrivacyEnabled)
            {
                PrivacyCrashReportHelper.SetEnabled(true);
                ReluTelemetryGate.SyncActiveHandler();
                ResetLogFlags();
                return;
            }

            ReluTelemetryGate.SyncActiveHandler();

            if (_blockCrashReports)
            {
                PrivacyCrashReportHelper.SetEnabled(false);
                LogOnce(ref _loggedCrashReports, "Unity crash reports disabled.");
            }
            else
            {
                PrivacyCrashReportHelper.SetEnabled(true);
                _loggedCrashReports = false;
            }
        }

        internal static void RestoreOnShutdown()
        {
            PrivacyCrashReportHelper.SetEnabled(true);
            ReluTelemetryGate.RestoreVanilla();
            ResetLogFlags();
        }

        private static void RefreshCache()
        {
            PrivacyBlockFlags flags = PrivacyGateLogic.Compute(new PrivacySceneConfig(
                masterEnabled: ModConfig.IsInitialized && IsPrivacyEnabled,
                blockReluTelemetry: ModConfig.BlockReluTelemetry.Value,
                blockReplayUpload: ModConfig.BlockReplayUpload.Value,
                blockReplayRecording: ModConfig.BlockReplayRecording.Value,
                blockCrashReports: ModConfig.BlockCrashReports.Value,
                stripCrashReportMetadata: ModConfig.StripCrashReportMetadata.Value,
                blockKraftonGppSdk: ModConfig.BlockKraftonGppSdk.Value));

            _blockReluTelemetry = flags.BlockReluTelemetry;
            _blockReplayUpload = flags.BlockReplayUpload;
            _blockReplayRecording = flags.BlockReplayRecording;
            _blockCrashReports = flags.BlockCrashReports;
            _stripCrashReportMetadata = flags.StripCrashReportMetadata;
            _blockKraftonGppSdk = flags.BlockKraftonGppSdk;
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
