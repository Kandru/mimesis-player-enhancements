namespace MimesisPlayerEnhancement
{
    using System.Collections.Generic;
    using MimesisPlayerEnhancement.Features.Privacy;
    using MimesisPlayerEnhancement.Features.UserInterface;

    /// <summary>
    /// Marks config entries whose global value only affects the local game client
    /// (guests may edit these from the web dashboard while connected).
    /// </summary>
    internal static class ModConfigEntryLocalEffect
    {
        private const string MainSectionId = "MimesisPlayerEnhancement";
        private const string PlayerTuningSectionId = "MimesisPlayerEnhancement_PlayerTuning";

        private static readonly HashSet<(string SectionId, string Key)> LocalEntries =
            new(EntryKeyComparer.Instance)
            {
                (MainSectionId, "EnableDebugLogging"),
                (MainSectionId, "LastSeenModVersion"),
                (UiConfig.SectionId, "ModToastDurationSeconds"),
                (UiConfig.SectionId, "EnableExtendedSaveSlots"),
                (UiConfig.SectionId, "EnableExtendedSpectatorPlayerList"),
                (UiConfig.SectionId, "EnableExtendedInGameMenuPlayerList"),
                (UiConfig.SectionId, "EnableDamageHealthGlow"),
                (UiConfig.SectionId, "EnableFloatingDamageNumbers"),
                (UiConfig.SectionId, "FloatingDamageDurationSeconds"),
                (UiConfig.SectionId, "EnableFpsUi"),
                (UiConfig.SectionId, "EnableFpsUiInventoryNetWorth"),
                (PrivacyConfig.SectionId, "EnablePrivacy"),
                (PrivacyConfig.SectionId, "BlockReluTelemetry"),
                (PrivacyConfig.SectionId, "BlockReplayUpload"),
                (PrivacyConfig.SectionId, "BlockReplayRecording"),
                (PrivacyConfig.SectionId, "BlockCrashReports"),
                (PrivacyConfig.SectionId, "StripCrashReportMetadata"),
                (PrivacyConfig.SectionId, "BlockKraftonGppSdk"),
                (PlayerTuningSectionId, "DisablePlayerCollision"),
            };

        internal static bool HasLocalEffect(string sectionId, string key)
        {
            return LocalEntries.Contains((sectionId, key));
        }

        private sealed class EntryKeyComparer : IEqualityComparer<(string SectionId, string Key)>
        {
            internal static readonly EntryKeyComparer Instance = new();

            public bool Equals((string SectionId, string Key) x, (string SectionId, string Key) y)
            {
                return string.Equals(x.SectionId, y.SectionId, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.Key, y.Key, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode((string SectionId, string Key) obj)
            {
                return HashCode.Combine(
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.SectionId),
                    StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Key));
            }
        }
    }
}
