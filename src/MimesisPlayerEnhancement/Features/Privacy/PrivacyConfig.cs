using MelonLoader;

namespace MimesisPlayerEnhancement.Features.Privacy
{
    internal static class PrivacyConfig
    {
        internal const string SectionId = "MimesisPlayerEnhancement_Privacy";

        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory(SectionId);
        }

        internal static void CreateEntries()
        {
            ModConfig.EnablePrivacy = ModConfig.CreateTrackedEntry(_category,
                "EnablePrivacy",
                false);

            ModConfig.BlockReluTelemetry = ModConfig.CreateTrackedEntry(_category,
                "BlockReluTelemetry",
                true);

            ModConfig.BlockReplayUpload = ModConfig.CreateTrackedEntry(_category,
                "BlockReplayUpload",
                true);

            ModConfig.BlockReplayRecording = ModConfig.CreateTrackedEntry(_category,
                "BlockReplayRecording",
                true);

            ModConfig.BlockCrashReports = ModConfig.CreateTrackedEntry(_category,
                "BlockCrashReports",
                true);

            ModConfig.StripCrashReportMetadata = ModConfig.CreateTrackedEntry(_category,
                "StripCrashReportMetadata",
                true);

            ModConfig.BlockKraftonGppSdk = ModConfig.CreateTrackedEntry(_category,
                "BlockKraftonGppSdk",
                true);
        }

        internal static void WireValidation()
        {
            ModConfig.EnablePrivacy.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnablePrivacy));
            ModConfig.BlockReluTelemetry.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.BlockReluTelemetry));
            ModConfig.BlockReplayUpload.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.BlockReplayUpload));
            ModConfig.BlockReplayRecording.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.BlockReplayRecording));
            ModConfig.BlockCrashReports.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.BlockCrashReports));
            ModConfig.StripCrashReportMetadata.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.StripCrashReportMetadata));
            ModConfig.BlockKraftonGppSdk.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.BlockKraftonGppSdk));
        }
    }
}
