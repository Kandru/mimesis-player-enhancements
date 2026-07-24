using MelonLoader;
using MimesisPlayerEnhancement.Config.QuickSettings;

namespace MimesisPlayerEnhancement.Config.HostConfigSync
{
    internal static class HostConfigSyncCodec
    {
        internal const int ProtocolVersion = 1;
        internal const int ChunkPayloadBytes = 1024;
        private const string Feature = "HostConfigSync";

        internal static bool ShouldSyncKey(string sectionId, string key)
        {
            return ModConfigRegistry.IsSaveOverrideAllowed(sectionId, key)
                && !ModConfigEntryLocalEffect.HasLocalEffect(sectionId, key);
        }

        internal static HostConfigSyncEnvelope BuildSnapshot(int revision)
        {
            HostConfigSyncEnvelope envelope = new()
            {
                V = ProtocolVersion,
                Rev = revision,
                SlotId = SaveSlotConfigStore.ActiveSlotId,
                Profile = CloneProfile(SaveSlotConfigStore.ActiveProfile),
            };

            foreach ((string sectionId, string key) in ModConfigRegistry.EnumerateSaveOverrideableKeys())
            {
                if (!ShouldSyncKey(sectionId, key))
                {
                    continue;
                }

                if (!ModConfigRegistry.TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null)
                {
                    continue;
                }

                if (!envelope.Values.TryGetValue(sectionId, out Dictionary<string, string>? keys))
                {
                    keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    envelope.Values[sectionId] = keys;
                }

                keys[key] = ModConfigRegistry.FormatEntryValue(entry);
            }

            return envelope;
        }

        internal static string Serialize(HostConfigSyncEnvelope envelope)
        {
            return ModJson.Serialize(envelope);
        }

        internal static HostConfigSyncEnvelope? TryDeserialize(string json)
        {
            return ModJson.Deserialize<HostConfigSyncEnvelope>(json);
        }

        internal static IReadOnlyList<string> SplitIntoChunks(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return [string.Empty];
            }

            List<string> chunks = [];
            for (int offset = 0; offset < json.Length; offset += ChunkPayloadBytes)
            {
                int length = Math.Min(ChunkPayloadBytes, json.Length - offset);
                chunks.Add(json.Substring(offset, length));
            }

            return chunks;
        }

        internal static string FormatChunkArgs(int revision, int chunkIndex, int chunkCount, string payload)
        {
            return $"{ProtocolVersion}|{revision}|{chunkIndex}|{chunkCount}|{payload}";
        }

        internal static bool TryParseChunkArgs(
            string args,
            out int protocolVersion,
            out int revision,
            out int chunkIndex,
            out int chunkCount,
            out string payload)
        {
            protocolVersion = 0;
            revision = 0;
            chunkIndex = 0;
            chunkCount = 0;
            payload = string.Empty;

            if (string.IsNullOrEmpty(args))
            {
                return false;
            }

            int first = args.IndexOf('|');
            int second = first < 0 ? -1 : args.IndexOf('|', first + 1);
            int third = second < 0 ? -1 : args.IndexOf('|', second + 1);
            int fourth = third < 0 ? -1 : args.IndexOf('|', third + 1);
            if (fourth < 0)
            {
                return false;
            }

            if (!int.TryParse(args.AsSpan(0, first), out protocolVersion)
                || !int.TryParse(args.AsSpan(first + 1, second - first - 1), out revision)
                || !int.TryParse(args.AsSpan(second + 1, third - second - 1), out chunkIndex)
                || !int.TryParse(args.AsSpan(third + 1, fourth - third - 1), out chunkCount))
            {
                return false;
            }

            payload = args[(fourth + 1)..];
            return chunkCount > 0 && chunkIndex >= 0 && chunkIndex < chunkCount;
        }

        internal static bool TryReassembleChunks(
            Dictionary<int, string> chunksByIndex,
            int revision,
            int chunkCount,
            out string json)
        {
            json = string.Empty;
            if (chunksByIndex.Count != chunkCount)
            {
                return false;
            }

            System.Text.StringBuilder builder = new();
            for (int i = 0; i < chunkCount; i++)
            {
                if (!chunksByIndex.TryGetValue(i, out string? part))
                {
                    return false;
                }

                builder.Append(part);
            }

            json = builder.ToString();
            return true;
        }

        internal static bool IsCompatibleModVersion(string? modVersion)
        {
            if (string.IsNullOrWhiteSpace(modVersion))
            {
                return false;
            }

            if (string.Equals(modVersion.Trim(), VersionInfo.ModuleVersion, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            static string Major(string value)
            {
                int dot = value.IndexOf('.');
                return dot < 0 ? value : value[..dot];
            }

            return string.Equals(Major(modVersion.Trim()), Major(VersionInfo.ModuleVersion), StringComparison.Ordinal);
        }

        private static bool _protocolMismatchLogged;

        internal static void LogProtocolMismatchOnce(string modVersion)
        {
            if (_protocolMismatchLogged)
            {
                return;
            }

            _protocolMismatchLogged = true;
            ModLog.Warn(Feature, $"Ignoring peer hello — mod version mismatch (peer={modVersion}, local={VersionInfo.ModuleVersion}).");
        }

        internal static void ResetSessionDiagnostics()
        {
            _protocolMismatchLogged = false;
        }

        private static SaveConfigProfileState CloneProfile(SaveConfigProfileState profile)
        {
            return new SaveConfigProfileState
            {
                Mode = profile.Mode,
                PresetId = profile.PresetId ?? string.Empty,
                PresetRevision = profile.PresetRevision,
            };
        }
    }
}
