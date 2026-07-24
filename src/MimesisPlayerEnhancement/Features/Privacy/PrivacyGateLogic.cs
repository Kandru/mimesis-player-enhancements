namespace MimesisPlayerEnhancement.Features.Privacy
{
    internal readonly struct PrivacyConfigSnapshot
    {
        internal PrivacyConfigSnapshot(
            bool masterEnabled,
            bool blockReluTelemetry,
            bool blockReplayUpload,
            bool blockReplayRecording,
            bool blockCrashReports,
            bool stripCrashReportMetadata,
            bool blockKraftonGppSdk)
        {
            MasterEnabled = masterEnabled;
            BlockReluTelemetry = blockReluTelemetry;
            BlockReplayUpload = blockReplayUpload;
            BlockReplayRecording = blockReplayRecording;
            BlockCrashReports = blockCrashReports;
            StripCrashReportMetadata = stripCrashReportMetadata;
            BlockKraftonGppSdk = blockKraftonGppSdk;
        }

        internal bool MasterEnabled { get; }

        internal bool BlockReluTelemetry { get; }

        internal bool BlockReplayUpload { get; }

        internal bool BlockReplayRecording { get; }

        internal bool BlockCrashReports { get; }

        internal bool StripCrashReportMetadata { get; }

        internal bool BlockKraftonGppSdk { get; }
    }

    internal readonly struct PrivacyBlockFlags
    {
        internal PrivacyBlockFlags(
            bool blockReluTelemetry,
            bool blockReplayUpload,
            bool blockReplayRecording,
            bool blockCrashReports,
            bool stripCrashReportMetadata,
            bool blockKraftonGppSdk)
        {
            BlockReluTelemetry = blockReluTelemetry;
            BlockReplayUpload = blockReplayUpload;
            BlockReplayRecording = blockReplayRecording;
            BlockCrashReports = blockCrashReports;
            StripCrashReportMetadata = stripCrashReportMetadata;
            BlockKraftonGppSdk = blockKraftonGppSdk;
        }

        internal bool BlockReluTelemetry { get; }

        internal bool BlockReplayUpload { get; }

        internal bool BlockReplayRecording { get; }

        internal bool BlockCrashReports { get; }

        internal bool StripCrashReportMetadata { get; }

        internal bool BlockKraftonGppSdk { get; }
    }

    internal static class PrivacyGateLogic
    {
        internal static PrivacyBlockFlags Compute(PrivacyConfigSnapshot config) =>
            new(
                blockReluTelemetry: config.MasterEnabled && config.BlockReluTelemetry,
                blockReplayUpload: config.MasterEnabled && config.BlockReplayUpload,
                blockReplayRecording: config.MasterEnabled && config.BlockReplayRecording,
                blockCrashReports: config.MasterEnabled && config.BlockCrashReports,
                stripCrashReportMetadata: config.MasterEnabled && config.StripCrashReportMetadata,
                blockKraftonGppSdk: config.MasterEnabled && config.BlockKraftonGppSdk);
    }
}
