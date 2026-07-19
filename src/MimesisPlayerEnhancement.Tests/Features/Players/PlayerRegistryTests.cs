using MimesisPlayerEnhancement.Features.Players;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Players
{
    public sealed class PlayerRegistryTests
    {
        private const ulong DisplayNameSteamId = 0x5001;
        private const ulong VoiceIdSteamId = 0x5002;
        private const ulong ConnectionSteamId = 0x5003;
        private const ulong ConnectedListSteamId = 0x5004;
        private const ulong VoiceMappingSteamId = 0x5005;
        private const ulong RemoveOfflineSteamId = 0x5006;
        private const ulong RemoveConnectedSteamId = 0x5007;
        private const ulong GetOrCreateSteamId = 0x5008;
        private const ulong ResolvedNameSteamId = 0x5009;

        [Theory]
        [InlineData(0UL, "Alice")]
        [InlineData(DisplayNameSteamId, "")]
        [InlineData(DisplayNameSteamId, "   ")]
        public void UpdateDisplayName_returns_false_for_invalid_input(ulong steamId, string displayName)
        {
            bool changed = PlayerRegistry.UpdateDisplayName(steamId, displayName);

            Assert.False(changed);
        }

        [Fact]
        public void UpdateDisplayName_returns_false_when_name_equals_steam_id_string()
        {
            string steamIdString = DisplayNameSteamId.ToString();

            bool changed = PlayerRegistry.UpdateDisplayName(DisplayNameSteamId, steamIdString);

            Assert.False(changed);
        }

        [Fact]
        public void UpdateDisplayName_bumps_revision_for_new_name()
        {
            int before = PlayerRegistry.Revision;

            bool changed = PlayerRegistry.UpdateDisplayName(DisplayNameSteamId, "Alice");

            Assert.True(changed);
            Assert.True(PlayerRegistry.Revision > before);
            Assert.True(PlayerRegistry.TryGetRecord(DisplayNameSteamId, out PlayerRecord record));
            Assert.Equal("Alice", record.DisplayName);
            Assert.Equal("Alice", record.Statistics.DisplayName);
        }

        [Fact]
        public void UpdateDisplayName_returns_false_without_revision_bump_for_same_name()
        {
            const ulong steamId = 0x5011;
            _ = PlayerRegistry.UpdateDisplayName(steamId, "Alice");
            int afterFirst = PlayerRegistry.Revision;

            bool changed = PlayerRegistry.UpdateDisplayName(steamId, "Alice");

            Assert.False(changed);
            Assert.Equal(afterFirst, PlayerRegistry.Revision);
        }

        [Theory]
        [InlineData(0UL, "voice-1")]
        [InlineData(VoiceIdSteamId, "")]
        [InlineData(VoiceIdSteamId, "   ")]
        public void UpdateVoiceId_returns_false_for_invalid_input(ulong steamId, string voiceId)
        {
            bool changed = PlayerRegistry.UpdateVoiceId(steamId, voiceId);

            Assert.False(changed);
        }

        [Fact]
        public void UpdateVoiceId_bumps_revision_for_new_voice_id()
        {
            int before = PlayerRegistry.Revision;

            bool changed = PlayerRegistry.UpdateVoiceId(VoiceIdSteamId, "voice-abc");

            Assert.True(changed);
            Assert.True(PlayerRegistry.Revision > before);
            Assert.True(PlayerRegistry.TryGetVoiceId(VoiceIdSteamId, out string voiceId));
            Assert.Equal("voice-abc", voiceId);
        }

        [Fact]
        public void UpdateVoiceId_returns_false_without_revision_bump_for_duplicate()
        {
            const ulong steamId = 0x5021;
            _ = PlayerRegistry.UpdateVoiceId(steamId, "voice-abc");
            int afterFirst = PlayerRegistry.Revision;

            bool changed = PlayerRegistry.UpdateVoiceId(steamId, "voice-abc");

            Assert.False(changed);
            Assert.Equal(afterFirst, PlayerRegistry.Revision);
        }

        [Theory]
        [InlineData("", "0")]
        [InlineData("   ", "0")]
        [InlineData("Alice", "Alice")]
        public void ApplyResolvedDisplayName_uses_fallback_for_zero_steam_id(string fallback, string expected)
        {
            string result = PlayerRegistry.ApplyResolvedDisplayName(0, fallback);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Connection_lifecycle_tracks_online_state()
        {
            DateTime connectedAt = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

            PlayerRegistry.SetConnectedSince(ConnectionSteamId, connectedAt);

            Assert.True(PlayerRegistry.IsConnected(ConnectionSteamId));
            Assert.True(PlayerRegistry.TryGetConnectedSince(ConnectionSteamId, out DateTime since));
            Assert.Equal(connectedAt, since);

            PlayerRegistry.MarkDisconnected(ConnectionSteamId);

            Assert.False(PlayerRegistry.IsConnected(ConnectionSteamId));
            Assert.False(PlayerRegistry.TryGetConnectedSince(ConnectionSteamId, out _));
        }

        [Fact]
        public void Connected_queries_reflect_single_online_player()
        {
            PlayerRegistry.SetConnectedSince(ConnectedListSteamId, DateTime.UtcNow);

            Assert.True(PlayerRegistry.HasAnyConnected());
            Assert.Contains(ConnectedListSteamId, PlayerRegistry.GetConnectedSteamIds());

            List<ulong> seen = [];
            PlayerRegistry.ForEachConnected(steamId => seen.Add(steamId));

            Assert.Contains(ConnectedListSteamId, seen);
        }

        [Fact]
        public void GetVoiceMappings_includes_updated_voice_ids()
        {
            Assert.True(PlayerRegistry.UpdateVoiceId(VoiceMappingSteamId, "voice-map-1"));

            IReadOnlyDictionary<ulong, string> mappings = PlayerRegistry.GetVoiceMappings();

            Assert.True(mappings.TryGetValue(VoiceMappingSteamId, out string? voiceId));
            Assert.Equal("voice-map-1", voiceId);
        }

        [Fact]
        public void RemoveIfNeverConnected_removes_offline_record()
        {
            _ = PlayerRegistry.GetOrCreate(RemoveOfflineSteamId);

            bool removed = PlayerRegistry.RemoveIfNeverConnected(RemoveOfflineSteamId);

            Assert.True(removed);
            Assert.False(PlayerRegistry.TryGetRecord(RemoveOfflineSteamId, out _));
        }

        [Fact]
        public void RemoveIfNeverConnected_keeps_connected_record()
        {
            PlayerRegistry.SetConnectedSince(RemoveConnectedSteamId, DateTime.UtcNow);

            bool removed = PlayerRegistry.RemoveIfNeverConnected(RemoveConnectedSteamId);

            Assert.False(removed);
            Assert.True(PlayerRegistry.TryGetRecord(RemoveConnectedSteamId, out _));
        }

        [Fact]
        public void GetOrCreate_initializes_statistics_document()
        {
            PlayerStatisticsDocument document = PlayerRegistry.GetOrCreate(GetOrCreateSteamId).Statistics;

            Assert.Equal(GetOrCreateSteamId, document.SteamId);
            Assert.True(PlayerRegistry.TryGetStatistics(GetOrCreateSteamId, out PlayerStatisticsDocument? stored));
            Assert.Same(document, stored);
        }

        [Fact]
        public void ApplyResolvedDisplayName_for_nonzero_steam_id_falls_back_when_resolver_unavailable()
        {
            string result = PlayerRegistry.ApplyResolvedDisplayName(ResolvedNameSteamId, "Fallback");

            Assert.Equal("Fallback", result);
            Assert.True(PlayerRegistry.TryGetRecord(ResolvedNameSteamId, out PlayerRecord record));
            Assert.Equal("Fallback", record.Statistics.DisplayName);
        }
    }
}
