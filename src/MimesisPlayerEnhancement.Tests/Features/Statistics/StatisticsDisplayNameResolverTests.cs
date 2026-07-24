using MimesisPlayerEnhancement.Features.Statistics;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class StatisticsDisplayNameResolverTests
    {
        [Fact]
        public void ResolveFromSources_prefers_cache_name()
        {
            var cache = new Dictionary<ulong, string> { [100] = "Cached" };

            string name = StatisticsDisplayNameResolver.ResolveFromSources(
                100,
                cache,
                localNick: "Local",
                localSteamId: 100,
                fallback: "Fallback");

            Assert.Equal("Cached", name);
        }

        [Fact]
        public void ResolveFromSources_uses_local_nick_when_cache_miss_and_local_steam_matches()
        {
            string name = StatisticsDisplayNameResolver.ResolveFromSources(
                42,
                cache: new Dictionary<ulong, string>(),
                localNick: "HostNick",
                localSteamId: 42,
                fallback: "Fallback");

            Assert.Equal("HostNick", name);
        }

        [Fact]
        public void ResolveFromSources_uses_fallback_then_steam_id_string()
        {
            Assert.Equal(
                "Fallback",
                StatisticsDisplayNameResolver.ResolveFromSources(7, null, null, 0, "Fallback"));
            Assert.Equal(
                "7",
                StatisticsDisplayNameResolver.ResolveFromSources(7, null, null, 0, "  "));
        }

        [Fact]
        public void TryFindSteamIdByDisplayName_matches_case_insensitively()
        {
            var cache = new Dictionary<ulong, string>
            {
                [1] = "Alice",
                [2] = "Bob",
            };

            Assert.True(StatisticsDisplayNameResolver.TryFindSteamIdByDisplayName(cache, "alice", out ulong steamId));
            Assert.Equal(1ul, steamId);
        }

        [Fact]
        public void TryFindSteamIdByDisplayName_returns_false_for_unknown_or_blank()
        {
            var cache = new Dictionary<ulong, string> { [1] = "Alice" };

            Assert.False(StatisticsDisplayNameResolver.TryFindSteamIdByDisplayName(cache, "Charlie", out _));
            Assert.False(StatisticsDisplayNameResolver.TryFindSteamIdByDisplayName(cache, "  ", out _));
        }
    }
}
