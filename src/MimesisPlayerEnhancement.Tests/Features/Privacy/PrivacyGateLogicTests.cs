using MimesisPlayerEnhancement.Features.Privacy;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Privacy
{
    public sealed class PrivacyGateLogicTests
    {
        private static PrivacyConfigSnapshot Config(
            bool masterEnabled = true,
            bool blockReluTelemetry = true,
            bool blockReplayUpload = true,
            bool blockReplayRecording = true,
            bool blockCrashReports = true,
            bool stripCrashReportMetadata = true,
            bool blockKraftonGppSdk = true) =>
            new(
                masterEnabled,
                blockReluTelemetry,
                blockReplayUpload,
                blockReplayRecording,
                blockCrashReports,
                stripCrashReportMetadata,
                blockKraftonGppSdk);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Compute_returns_all_false_when_master_disabled(bool subFlag)
        {
            PrivacyBlockFlags flags = PrivacyGateLogic.Compute(Config(
                masterEnabled: false,
                blockReluTelemetry: subFlag,
                blockReplayUpload: subFlag,
                blockReplayRecording: subFlag,
                blockCrashReports: subFlag,
                stripCrashReportMetadata: subFlag,
                blockKraftonGppSdk: subFlag));

            Assert.False(flags.BlockReluTelemetry);
            Assert.False(flags.BlockReplayUpload);
            Assert.False(flags.BlockReplayRecording);
            Assert.False(flags.BlockCrashReports);
            Assert.False(flags.StripCrashReportMetadata);
            Assert.False(flags.BlockKraftonGppSdk);
        }

        [Fact]
        public void Compute_returns_all_true_when_master_and_all_sub_flags_enabled()
        {
            PrivacyBlockFlags flags = PrivacyGateLogic.Compute(Config());

            Assert.True(flags.BlockReluTelemetry);
            Assert.True(flags.BlockReplayUpload);
            Assert.True(flags.BlockReplayRecording);
            Assert.True(flags.BlockCrashReports);
            Assert.True(flags.StripCrashReportMetadata);
            Assert.True(flags.BlockKraftonGppSdk);
        }

        [Fact]
        public void Compute_returns_all_false_when_master_enabled_but_all_sub_flags_disabled()
        {
            PrivacyBlockFlags flags = PrivacyGateLogic.Compute(Config(
                masterEnabled: true,
                blockReluTelemetry: false,
                blockReplayUpload: false,
                blockReplayRecording: false,
                blockCrashReports: false,
                stripCrashReportMetadata: false,
                blockKraftonGppSdk: false));

            Assert.False(flags.BlockReluTelemetry);
            Assert.False(flags.BlockReplayUpload);
            Assert.False(flags.BlockReplayRecording);
            Assert.False(flags.BlockCrashReports);
            Assert.False(flags.StripCrashReportMetadata);
            Assert.False(flags.BlockKraftonGppSdk);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void Compute_blockReluTelemetry_follows_master_and_sub_flag(bool masterEnabled, bool subFlag)
        {
            PrivacyBlockFlags flags = PrivacyGateLogic.Compute(Config(
                masterEnabled: masterEnabled,
                blockReluTelemetry: subFlag,
                blockReplayUpload: false,
                blockReplayRecording: false,
                blockCrashReports: false,
                stripCrashReportMetadata: false,
                blockKraftonGppSdk: false));

            Assert.Equal(masterEnabled && subFlag, flags.BlockReluTelemetry);
            Assert.False(flags.BlockReplayUpload);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public void Compute_blockReplayUpload_follows_master_and_sub_flag(bool masterEnabled, bool subFlag)
        {
            PrivacyBlockFlags flags = PrivacyGateLogic.Compute(Config(
                masterEnabled: masterEnabled,
                blockReluTelemetry: false,
                blockReplayUpload: subFlag,
                blockReplayRecording: false,
                blockCrashReports: false,
                stripCrashReportMetadata: false,
                blockKraftonGppSdk: false));

            Assert.Equal(masterEnabled && subFlag, flags.BlockReplayUpload);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public void Compute_blockReplayRecording_follows_master_and_sub_flag(bool masterEnabled, bool subFlag)
        {
            PrivacyBlockFlags flags = PrivacyGateLogic.Compute(Config(
                masterEnabled: masterEnabled,
                blockReluTelemetry: false,
                blockReplayUpload: false,
                blockReplayRecording: subFlag,
                blockCrashReports: false,
                stripCrashReportMetadata: false,
                blockKraftonGppSdk: false));

            Assert.Equal(masterEnabled && subFlag, flags.BlockReplayRecording);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public void Compute_blockCrashReports_follows_master_and_sub_flag(bool masterEnabled, bool subFlag)
        {
            PrivacyBlockFlags flags = PrivacyGateLogic.Compute(Config(
                masterEnabled: masterEnabled,
                blockReluTelemetry: false,
                blockReplayUpload: false,
                blockReplayRecording: false,
                blockCrashReports: subFlag,
                stripCrashReportMetadata: false,
                blockKraftonGppSdk: false));

            Assert.Equal(masterEnabled && subFlag, flags.BlockCrashReports);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public void Compute_stripCrashReportMetadata_follows_master_and_sub_flag(bool masterEnabled, bool subFlag)
        {
            PrivacyBlockFlags flags = PrivacyGateLogic.Compute(Config(
                masterEnabled: masterEnabled,
                blockReluTelemetry: false,
                blockReplayUpload: false,
                blockReplayRecording: false,
                blockCrashReports: false,
                stripCrashReportMetadata: subFlag,
                blockKraftonGppSdk: false));

            Assert.Equal(masterEnabled && subFlag, flags.StripCrashReportMetadata);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        public void Compute_blockKraftonGppSdk_follows_master_and_sub_flag(bool masterEnabled, bool subFlag)
        {
            PrivacyBlockFlags flags = PrivacyGateLogic.Compute(Config(
                masterEnabled: masterEnabled,
                blockReluTelemetry: false,
                blockReplayUpload: false,
                blockReplayRecording: false,
                blockCrashReports: false,
                stripCrashReportMetadata: false,
                blockKraftonGppSdk: subFlag));

            Assert.Equal(masterEnabled && subFlag, flags.BlockKraftonGppSdk);
        }
    }
}
