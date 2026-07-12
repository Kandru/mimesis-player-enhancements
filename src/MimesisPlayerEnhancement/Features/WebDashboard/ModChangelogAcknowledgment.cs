using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class ModChangelogAcknowledgment
    {
        private const string Feature = "WebDashboard";
        private const string SectionId = "MimesisPlayerEnhancement";
        private const string Key = "LastSeenModVersion";

        internal static bool IsPending()
        {
            return !VersionsMatch(ModConfig.LastSeenModVersion.Value, VersionInfo.ModuleVersion);
        }

        internal static WebDashboardChangelogAcknowledgeResult Acknowledge()
        {
            if (!ModConfig.IsInitialized)
            {
                return Fail("Configuration is not initialized.");
            }

            string modVersion = VersionInfo.ModuleVersion;
            string lastSeen = ModConfig.LastSeenModVersion.Value ?? string.Empty;
            if (!IsPending())
            {
                return new WebDashboardChangelogAcknowledgeResult
                {
                    Success = true,
                    Message = "Changelog already acknowledged.",
                    ModVersion = modVersion,
                    LastSeenModVersion = lastSeen,
                };
            }

            ModConfigChangeTracker.BeginBatch();
            try
            {
                if (!ModConfigRegistry.TryApplyNormalizedEntry(
                        SectionId,
                        Key,
                        modVersion,
                        out string effectiveValue,
                        out string? error))
                {
                    return Fail(error ?? "Failed to apply changelog acknowledgment.");
                }

                if (!GlobalConfigStore.TryWriteValue(
                        SectionId,
                        Key,
                        effectiveValue,
                        out error,
                        waitForCompletion: true))
                {
                    return Fail(error ?? "Failed to save changelog acknowledgment.");
                }

                ModLog.Debug(Feature, $"Changelog acknowledged — version={effectiveValue}");
                return new WebDashboardChangelogAcknowledgeResult
                {
                    Success = true,
                    Message = "Changelog acknowledged.",
                    ModVersion = modVersion,
                    LastSeenModVersion = effectiveValue,
                };
            }
            finally
            {
                ModConfigChangeTracker.EndBatch();
            }
        }

        private static WebDashboardChangelogAcknowledgeResult Fail(string message)
        {
            ModLog.Warn(Feature, $"Changelog acknowledgment failed — {message}");
            return new WebDashboardChangelogAcknowledgeResult
            {
                Success = false,
                Message = message,
                ModVersion = VersionInfo.ModuleVersion,
                LastSeenModVersion = ModConfig.LastSeenModVersion.Value ?? string.Empty,
            };
        }

        private static bool VersionsMatch(string? lastSeen, string current)
        {
            string normalizedLast = Normalize(lastSeen);
            string normalizedCurrent = Normalize(current);
            if (string.IsNullOrEmpty(normalizedCurrent))
            {
                return true;
            }

            return string.Equals(normalizedLast, normalizedCurrent, StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
