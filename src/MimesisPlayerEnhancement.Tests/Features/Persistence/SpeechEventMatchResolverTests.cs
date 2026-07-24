using MimesisPlayerEnhancement.Features.Persistence;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Persistence
{
    public sealed class SpeechEventMatchResolverTests
    {
        [Theory]
        [InlineData(true, true, false, true)]
        [InlineData(false, true, false, false)]
        [InlineData(true, false, false, false)]
        [InlineData(true, true, true, false)]
        public void ShouldCacheDisconnectEvents_requires_enabled_host_remote(
            bool enabled,
            bool isHost,
            bool isLocal,
            bool expected)
        {
            Assert.Equal(
                expected,
                SpeechEventMatchResolver.ShouldCacheDisconnectEvents(enabled, isHost, isLocal));
        }

        [Fact]
        public void ResolveMatchedPlayerNames_adds_direct_pool_voice_id()
        {
            HashSet<string> matched = SpeechEventMatchResolver.ResolveMatchedPlayerNames(
                playerId: "voice-a",
                steamId: 0,
                registryMappings: new Dictionary<ulong, string>(),
                disconnectedMappings: new Dictionary<ulong, string>(),
                poolVoiceIds: ["voice-a", "voice-b"],
                useDisconnectedMapping: false);

            Assert.Equal(["voice-a"], matched.OrderBy(x => x));
        }

        [Fact]
        public void ResolveMatchedPlayerNames_registry_requires_pool_match_on_claim()
        {
            HashSet<string> matched = SpeechEventMatchResolver.ResolveMatchedPlayerNames(
                playerId: null,
                steamId: 42,
                registryMappings: new Dictionary<ulong, string> { [42] = "missing-voice" },
                disconnectedMappings: new Dictionary<ulong, string>(),
                poolVoiceIds: ["voice-a"],
                useDisconnectedMapping: false);

            Assert.Empty(matched);
        }

        [Fact]
        public void ResolveMatchedPlayerNames_registry_skips_pool_match_on_disconnect_reclaim()
        {
            HashSet<string> matched = SpeechEventMatchResolver.ResolveMatchedPlayerNames(
                playerId: null,
                steamId: 42,
                registryMappings: new Dictionary<ulong, string> { [42] = "missing-voice" },
                disconnectedMappings: new Dictionary<ulong, string>(),
                poolVoiceIds: ["voice-a"],
                useDisconnectedMapping: true);

            Assert.Equal(["missing-voice"], matched.OrderBy(x => x));
        }

        [Fact]
        public void ResolveDominantVoiceId_picks_highest_event_count()
        {
            string? dominant = SpeechEventMatchResolver.ResolveDominantVoiceId(
                ["a", "b", "c"],
                new Dictionary<string, int> { ["a"] = 1, ["b"] = 5, ["c"] = 3 });

            Assert.Equal("b", dominant);
        }

        [Fact]
        public void ResolveDominantPlayerName_picks_most_common_name()
        {
            string? dominant = SpeechEventMatchResolver.ResolveDominantPlayerName(
                ["a", "b", "a", "c", "a", "b"]);

            Assert.Equal("a", dominant);
        }

        [Fact]
        public void InferUnmappedPoolVoiceIds_solo_save_claims_all_unmapped()
        {
            HashSet<string> matched = [];

            SpeechEventMatchResolver.InferUnmappedPoolVoiceIds(
                matched,
                steamId: 7,
                isKnownSavePlayer: true,
                isSoloSaveForSteam: true,
                poolVoiceIds: ["v1", "v2"],
                mappedToOtherSteamIds: new HashSet<string>(StringComparer.Ordinal) { "mapped-elsewhere" },
                eventCountsByVoiceId: new Dictionary<string, int> { ["v1"] = 1, ["v2"] = 9 });

            Assert.Equal(["v1", "v2"], matched.OrderBy(x => x));
        }

        [Fact]
        public void InferUnmappedPoolVoiceIds_multi_save_claims_dominant_unmapped()
        {
            HashSet<string> matched = [];

            SpeechEventMatchResolver.InferUnmappedPoolVoiceIds(
                matched,
                steamId: 7,
                isKnownSavePlayer: true,
                isSoloSaveForSteam: false,
                poolVoiceIds: ["v1", "v2"],
                mappedToOtherSteamIds: new HashSet<string>(StringComparer.Ordinal),
                eventCountsByVoiceId: new Dictionary<string, int> { ["v1"] = 1, ["v2"] = 9 });

            Assert.Equal(["v2"], matched.OrderBy(x => x));
        }

        [Fact]
        public void InferUnmappedPoolVoiceIds_skips_unknown_players()
        {
            HashSet<string> matched = [];

            SpeechEventMatchResolver.InferUnmappedPoolVoiceIds(
                matched,
                steamId: 7,
                isKnownSavePlayer: false,
                isSoloSaveForSteam: true,
                poolVoiceIds: ["v1"],
                mappedToOtherSteamIds: new HashSet<string>(StringComparer.Ordinal),
                eventCountsByVoiceId: new Dictionary<string, int> { ["v1"] = 3 });

            Assert.Empty(matched);
        }
    }
}
