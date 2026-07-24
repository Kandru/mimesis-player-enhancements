using MimesisPlayerEnhancement.Config.HostConfigSync;
using MimesisPlayerEnhancement.Config.QuickSettings;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Config
{
    public sealed class HostConfigSyncCodecTests
    {
        [Fact]
        public void ShouldSyncKey_excludes_global_only_and_local_effect_entries()
        {
            Assert.False(HostConfigSyncCodec.ShouldSyncKey("MimesisPlayerEnhancement_Ui", "EnableFpsUi"));
            Assert.False(HostConfigSyncCodec.ShouldSyncKey("MimesisPlayerEnhancement_Privacy", "EnablePrivacy"));
            Assert.False(HostConfigSyncCodec.ShouldSyncKey("MimesisPlayerEnhancement_PlayerTuning", "DisablePlayerCollision"));
            Assert.False(HostConfigSyncCodec.ShouldSyncKey("MimesisPlayerEnhancement", "EnableDebugLogging"));
        }

        [Fact]
        public void Serialize_TryDeserialize_round_trips_envelope()
        {
            HostConfigSyncEnvelope envelope = new()
            {
                V = HostConfigSyncCodec.ProtocolVersion,
                Rev = 7,
                SlotId = 2,
                Profile = new SaveConfigProfileState
                {
                    Mode = SaveConfigProfileMode.Quick,
                    PresetId = "picnic",
                    PresetRevision = 3,
                },
            };
            envelope.Values["MimesisPlayerEnhancement_Economy"] =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["EnableEconomy"] = "true",
                };

            string json = HostConfigSyncCodec.Serialize(envelope);
            HostConfigSyncEnvelope? decoded = HostConfigSyncCodec.TryDeserialize(json);

            Assert.NotNull(decoded);
            Assert.Equal(7, decoded!.Rev);
            Assert.Equal(2, decoded.SlotId);
            Assert.Equal(SaveConfigProfileMode.Quick, decoded.Profile.Mode);
            Assert.Equal("picnic", decoded.Profile.PresetId);
            Assert.Equal("true", decoded.Values["MimesisPlayerEnhancement_Economy"]["EnableEconomy"]);
        }

        [Fact]
        public void SplitIntoChunks_and_TryReassembleChunks_round_trip()
        {
            string json = new string('x', HostConfigSyncCodec.ChunkPayloadBytes + 50);
            IReadOnlyList<string> chunks = HostConfigSyncCodec.SplitIntoChunks(json);
            Assert.Equal(2, chunks.Count);

            Dictionary<int, string> byIndex = new();
            for (int i = 0; i < chunks.Count; i++)
            {
                byIndex[i] = chunks[i];
            }

            Assert.True(HostConfigSyncCodec.TryReassembleChunks(byIndex, revision: 1, chunks.Count, out string rebuilt));
            Assert.Equal(json, rebuilt);
        }

        [Theory]
        [InlineData("1|2|0|2|abc", 1, 2, 0, 2, "abc")]
        [InlineData("1|9|1|3|payload", 1, 9, 1, 3, "payload")]
        public void TryParseChunkArgs_parses_valid_chunks(
            string args,
            int protocol,
            int revision,
            int index,
            int count,
            string payload)
        {
            Assert.True(HostConfigSyncCodec.TryParseChunkArgs(
                args,
                out int parsedProtocol,
                out int parsedRevision,
                out int parsedIndex,
                out int parsedCount,
                out string parsedPayload));
            Assert.Equal(protocol, parsedProtocol);
            Assert.Equal(revision, parsedRevision);
            Assert.Equal(index, parsedIndex);
            Assert.Equal(count, parsedCount);
            Assert.Equal(payload, parsedPayload);
        }

        [Fact]
        public void TryParseChunkArgs_rejects_oversized_chunk_count_and_payload()
        {
            string hugePayload = new string('a', HostConfigSyncCodec.MaxReceivedChunkPayloadChars + 1);
            string oversizePayloadArgs = HostConfigSyncCodec.FormatChunkArgs(1, 0, 1, hugePayload);
            Assert.False(HostConfigSyncCodec.TryParseChunkArgs(
                oversizePayloadArgs,
                out _,
                out _,
                out _,
                out _,
                out _));

            string oversizeCountArgs =
                $"{HostConfigSyncCodec.ProtocolVersion}|1|0|{HostConfigSyncCodec.MaxChunkCount + 1}|x";
            Assert.False(HostConfigSyncCodec.TryParseChunkArgs(
                oversizeCountArgs,
                out _,
                out _,
                out _,
                out _,
                out _));
        }

        [Fact]
        public void TryReassembleChunks_rejects_total_size_above_limit()
        {
            Dictionary<int, string> chunks = new()
            {
                [0] = new string('x', HostConfigSyncCodec.MaxTotalSnapshotChars),
                [1] = "y",
            };

            Assert.False(HostConfigSyncCodec.TryReassembleChunks(chunks, revision: 1, chunkCount: 2, out _));
        }

        [Fact]
        public void TryCountSnapshotEntries_rejects_too_many_keys()
        {
            HostConfigSyncEnvelope envelope = new();
            Dictionary<string, string> section = new(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < HostConfigSyncCodec.MaxSnapshotEntryCount + 1; i++)
            {
                section[$"Key{i}"] = "true";
            }

            envelope.Values["MimesisPlayerEnhancement_Economy"] = section;
            Assert.False(HostConfigSyncCodec.TryCountSnapshotEntries(envelope, out _));
        }

        [Fact]
        public void IsCompatibleModVersion_accepts_same_major()
        {
            string local = VersionInfo.ModuleVersion;
            string major = local.Contains('.') ? local[..local.IndexOf('.')] : local;
            Assert.True(HostConfigSyncCodec.IsCompatibleModVersion($"{major}.999.0"));
        }
    }
}
