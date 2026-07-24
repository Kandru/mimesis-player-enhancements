using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MimicTuning.MimicHorn
{
    internal static class MimicHornResolver
    {
        internal const string SectionId = "MimesisPlayerEnhancement_MimicTuning";

        internal const float VanillaMaxRecordSeconds = 5f;
        internal const float VanillaRecordingGapSeconds = 1f;
        internal const int VanillaMaxStoredRecords = 10;

        private static bool _cachedMasterEnabled;
        private static bool _cachedCustom;
        private static bool _cachedAllowHornImitation = true;
        private static float _cachedMaxRecordSeconds = VanillaMaxRecordSeconds;
        private static float _cachedRecordingGapSeconds = VanillaRecordingGapSeconds;
        private static int _cachedMaxStoredRecords = VanillaMaxStoredRecords;

        internal static bool ShouldApplyCustom =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() => _cachedMasterEnabled)
            && _cachedCustom;

        internal static bool AllowHornImitation => _cachedAllowHornImitation;
        internal static float MaxRecordSeconds => _cachedMaxRecordSeconds;
        internal static float RecordingGapSeconds => _cachedRecordingGapSeconds;
        internal static int MaxStoredRecords => _cachedMaxStoredRecords;

        internal static void RefreshConfigCache()
        {
            if (ModConfig.EnableMimicTuning == null || ModConfig.HornImitationMode == null)
            {
                return;
            }

            _cachedMasterEnabled = ModConfig.EnableMimicTuning.Value;
            _cachedCustom = string.Equals(
                ModConfig.HornImitationMode.Value,
                "Custom",
                StringComparison.OrdinalIgnoreCase);
            _cachedAllowHornImitation = ModConfig.AllowHornImitation?.Value ?? true;
            _cachedMaxRecordSeconds = Mathf.Clamp(
                ModConfig.HornMaxRecordSeconds?.Value ?? VanillaMaxRecordSeconds,
                0.1f,
                60f);
            _cachedRecordingGapSeconds = Mathf.Clamp(
                ModConfig.HornRecordingGapSeconds?.Value ?? VanillaRecordingGapSeconds,
                0.1f,
                30f);
            _cachedMaxStoredRecords = Mathf.Clamp(
                ModConfig.HornMaxStoredRecords?.Value ?? VanillaMaxStoredRecords,
                1,
                100);
        }
    }
}
