using MimesisPlayerEnhancement.Config.QuickSettings;

namespace MimesisPlayerEnhancement.Config.HostConfigSync
{
    /// <summary>
    /// Client-side in-memory mirror of the host save-scoped config. Never writes disk or sidecars.
    /// </summary>
    internal static class HostConfigMirror
    {
        private const string Feature = "HostConfigSync";

        private static bool _isApplyingRemoteMirror;

        internal static bool IsActive { get; private set; }

        internal static bool IsApplyingRemoteMirror => _isApplyingRemoteMirror;

        internal static int MirroredSlotId { get; private set; } = -1;

        internal static SaveConfigProfileState MirroredProfile { get; private set; } = new();

        internal static int Revision { get; private set; }

        internal static bool ApplySnapshot(HostConfigSyncEnvelope envelope)
        {
            if (envelope == null)
            {
                return false;
            }

            if (envelope.V != HostConfigSyncCodec.ProtocolVersion)
            {
                ModLog.Warn(Feature, $"Ignoring config snapshot — protocol v{envelope.V} (expected {HostConfigSyncCodec.ProtocolVersion}).");
                return false;
            }

            if (!HostConfigSyncCodec.TryCountSnapshotEntries(envelope, out _))
            {
                ModLog.Warn(Feature, "Ignoring config snapshot — too many entries.");
                return false;
            }

            if (envelope.Rev < Revision && IsActive)
            {
                ModLog.Debug(Feature, $"Ignoring stale config snapshot — rev={envelope.Rev}, active={Revision}.");
                return false;
            }

            int appliedKeys = 0;
            int skippedKeys = 0;

            ModConfigChangeTracker.BeginBatch();
            _isApplyingRemoteMirror = true;
            try
            {
                MirroredSlotId = envelope.SlotId;
                MirroredProfile = envelope.Profile ?? new SaveConfigProfileState();
                Revision = envelope.Rev;
                IsActive = true;

                foreach (KeyValuePair<string, Dictionary<string, string>> section in envelope.Values)
                {
                    foreach (KeyValuePair<string, string> pair in section.Value)
                    {
                        if (!HostConfigSyncCodec.ShouldSyncKey(section.Key, pair.Key))
                        {
                            continue;
                        }

                        if (!ModConfigRegistry.TryNormalizeRawValue(
                                section.Key,
                                pair.Key,
                                pair.Value,
                                out string normalized,
                                out _))
                        {
                            skippedKeys++;
                            ModLog.Debug(Feature, $"Skipped unknown synced key — {section.Key}/{pair.Key}.");
                            continue;
                        }

                        if (!ModConfigRegistry.TryApplyNormalizedEntry(
                                section.Key,
                                pair.Key,
                                normalized,
                                out _,
                                out string? error))
                        {
                            skippedKeys++;
                            ModLog.Debug(Feature, $"Failed applying synced key — {section.Key}/{pair.Key}: {error}");
                            continue;
                        }

                        appliedKeys++;
                    }
                }

                ModConfig.SanitizeFloatEntries();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Config mirror apply failed — {ex.Message}");
                return false;
            }
            finally
            {
                _isApplyingRemoteMirror = false;
                ModConfigChangeTracker.EndBatch();
            }

            WebDashboardSnapshotCache.MarkDirty();
            ModLog.Info(Feature,
                $"Applied host config mirror — slot={MirroredSlotId}, rev={Revision}, profile={MirroredProfile.Mode}.");
            ModLog.Debug(Feature, $"Mirror apply summary — applied={appliedKeys}, skipped={skippedKeys}.");
            HostConfigSyncRuntime.OnMirrorApplied();
            return true;
        }

        internal static void Clear()
        {
            if (!IsActive && Revision == 0)
            {
                return;
            }

            IsActive = false;
            MirroredSlotId = -1;
            MirroredProfile = new SaveConfigProfileState();
            Revision = 0;

            if (!ModConfig.IsInitialized)
            {
                return;
            }

            ModConfigChangeTracker.BeginBatch();
            try
            {
                ModConfig.ReloadGlobalFromFile();
            }
            finally
            {
                ModConfigChangeTracker.CancelBatch();
            }

            ModConfigChangeTracker.NotifyFullReload();
            WebDashboardSnapshotCache.MarkDirty();
            ModLog.Info(Feature, "Cleared host config mirror — restored local global config.");
        }
    }
}
