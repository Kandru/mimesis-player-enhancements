using MimesisPlayerEnhancement.Features.WebDashboard;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.WebDashboard
{
    public sealed class WebDashboardPlayerListMergerTests
    {
        private static WebDashboardPlayerDto Player(ulong steamId, string name, bool isHost = false) =>
            new()
            {
                SteamId = steamId,
                DisplayName = name,
                IsHost = isHost,
            };

        [Fact]
        public void MergePlayerLists_returns_empty_when_both_inputs_empty()
        {
            List<WebDashboardPlayerDto> merged = WebDashboardPlayerListMerger.MergePlayerLists([], []);

            Assert.Empty(merged);
        }

        [Fact]
        public void MergePlayerLists_returns_live_copy_when_offline_empty()
        {
            WebDashboardPlayerDto live = Player(1, "Alice");
            List<WebDashboardPlayerDto> merged = WebDashboardPlayerListMerger.MergePlayerLists([live], []);

            Assert.Single(merged);
            Assert.Same(live, merged[0]);
        }

        [Fact]
        public void MergePlayerLists_live_overrides_offline_for_same_steam_id()
        {
            WebDashboardPlayerDto offline = Player(42, "Offline");
            WebDashboardPlayerDto live = Player(42, "Live");

            List<WebDashboardPlayerDto> merged = WebDashboardPlayerListMerger.MergePlayerLists([live], [offline]);

            Assert.Single(merged);
            Assert.Equal("Live", merged[0].DisplayName);
        }

        [Fact]
        public void MergePlayerLists_sorts_host_first_then_name()
        {
            WebDashboardPlayerDto guest = Player(2, "Zed");
            WebDashboardPlayerDto host = Player(1, "Amy", isHost: true);
            WebDashboardPlayerDto other = Player(3, "Bob");
            WebDashboardPlayerDto offlinePlaceholder = Player(99, "Offline");

            List<WebDashboardPlayerDto> merged = WebDashboardPlayerListMerger.MergePlayerLists(
                [guest, host, other],
                [offlinePlaceholder]);

            Assert.Equal(4, merged.Count);
            Assert.True(merged[0].IsHost);
            Assert.Equal("Amy", merged[0].DisplayName);
            Assert.Equal("Bob", merged[1].DisplayName);
            Assert.Equal("Offline", merged[2].DisplayName);
            Assert.Equal("Zed", merged[3].DisplayName);
        }

        [Fact]
        public void ApplyLiveToMerged_updates_existing_and_reverts_disconnected_to_offline()
        {
            WebDashboardPlayerDto offlineAlice = Player(1, "Alice (offline)");
            WebDashboardPlayerDto offlineBob = Player(2, "Bob (offline)");
            WebDashboardPlayerDto liveBob = Player(2, "Bob");
            List<WebDashboardPlayerDto> merged = [offlineAlice, liveBob];
            Dictionary<ulong, int> index = WebDashboardPlayerListMerger.BuildMergedIndex(merged);

            WebDashboardPlayerDto liveAlice = Player(1, "Alice (live)");
            bool changed = WebDashboardPlayerListMerger.ApplyLiveToMerged(
                merged,
                index,
                live: [liveAlice],
                offline: [offlineAlice, offlineBob],
                previousLiveSteamIds: [1, 2]);

            Assert.True(changed);
            Assert.Equal("Alice (live)", merged[0].DisplayName);
            Assert.Equal("Bob (offline)", merged[1].DisplayName);
        }

        [Fact]
        public void ApplyLiveToMerged_adds_new_live_player_and_rebuilds_index()
        {
            WebDashboardPlayerDto offline = Player(1, "Alice");
            List<WebDashboardPlayerDto> merged = [offline];
            Dictionary<ulong, int> index = WebDashboardPlayerListMerger.BuildMergedIndex(merged);

            WebDashboardPlayerDto newcomer = Player(2, "Bob");
            bool changed = WebDashboardPlayerListMerger.ApplyLiveToMerged(
                merged,
                index,
                live: [offline, newcomer],
                offline: [offline],
                previousLiveSteamIds: [1]);

            Assert.True(changed);
            Assert.Equal(2, merged.Count);
            Assert.True(index.ContainsKey(2));
            Assert.Equal(1, index[2]);
        }

        [Fact]
        public void RebuildMergedIndex_tracks_positions_after_sort()
        {
            WebDashboardPlayerDto host = Player(1, "Host", isHost: true);
            WebDashboardPlayerDto guest = Player(2, "Guest");
            List<WebDashboardPlayerDto> merged = [guest, host];
            WebDashboardPlayerListMerger.SortPlayers(merged);
            Dictionary<ulong, int> index = [];

            WebDashboardPlayerListMerger.RebuildMergedIndex(merged, index);

            Assert.Equal(0, index[1]);
            Assert.Equal(1, index[2]);
        }
    }
}
