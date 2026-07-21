using MimesisPlayerEnhancement.Features.PlayerAnnouncements;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.PlayerAnnouncements
{
    public sealed class MapRunStatsFormatterTests
    {
        [Fact]
        public void Subtract_computes_field_wise_delta()
        {
            MapRunStatsSnapshot current = Snapshot(
                survivalDeaths: 3,
                survivalWins: 2,
                friendsKilled: 1);
            MapRunStatsSnapshot baseline = Snapshot(
                survivalDeaths: 1,
                survivalWins: 2,
                friendsKilled: 0);

            MapRunStatsSnapshot delta = MapRunStatsFormatter.Subtract(current, baseline);

            Assert.Equal(2, delta.SurvivalDeaths);
            Assert.Equal(0, delta.SurvivalWins);
            Assert.Equal(1, delta.FriendsKilled);
        }

        [Fact]
        public void SubtractDictionary_keeps_only_positive_diffs()
        {
            Dictionary<string, long> current = new()
            {
                ["Goblin"] = 5,
                ["Slime"] = 2,
            };
            Dictionary<string, long> baseline = new()
            {
                ["Goblin"] = 3,
                ["Slime"] = 2,
                ["Orc"] = 1,
            };

            Dictionary<string, long> delta = MapRunStatsFormatter.SubtractDictionary(current, baseline);

            Assert.Single(delta);
            Assert.Equal(2, delta["Goblin"]);
        }

        [Fact]
        public void Format_returns_empty_message_for_zero_delta()
        {
            string result = MapRunStatsFormatter.Format(new MapRunStatsSnapshot());

            Assert.Equal(L10n("announce.map_run_empty"), result);
        }

        [Fact]
        public void Format_orders_deaths_before_wins_before_monster_kills()
        {
            MapRunStatsSnapshot stats = Snapshot(
                survivalDeaths: 2,
                survivalWins: 1,
                monsterKills: new Dictionary<string, long>
                {
                    ["Goblin"] = 3,
                    ["Slime"] = 1,
                });

            string result = MapRunStatsFormatter.Format(stats);

            int deathsIndex = result.IndexOf(L10n("announce.deaths_plural", 2), StringComparison.Ordinal);
            int winsIndex = result.IndexOf(L10n("announce.wins", 1), StringComparison.Ordinal);
            int goblinIndex = result.IndexOf(L10n("announce.monster_kills_plural", 3, "Goblin"), StringComparison.Ordinal);
            int slimeIndex = result.IndexOf(L10n("announce.monster_kills", 1, "Slime"), StringComparison.Ordinal);

            Assert.True(deathsIndex >= 0);
            Assert.True(winsIndex > deathsIndex);
            Assert.True(goblinIndex > winsIndex);
            Assert.True(slimeIndex > goblinIndex);
        }

        [Theory]
        [InlineData(1, "announce.deaths", "announce.deaths_plural")]
        [InlineData(2, "announce.deaths", "announce.deaths_plural")]
        public void Format_uses_singular_or_plural_death_keys(long count, string singularKey, string pluralKey)
        {
            MapRunStatsSnapshot stats = Snapshot(survivalDeaths: count);

            string result = MapRunStatsFormatter.Format(stats);

            string expectedFragment = count == 1
                ? L10n(singularKey, count)
                : L10n(pluralKey, count);
            Assert.Contains(expectedFragment, result);
        }

        private static MapRunStatsSnapshot Snapshot(
            long survivalDeaths = 0,
            long survivalWins = 0,
            long friendsKilled = 0,
            Dictionary<string, long>? monsterKills = null) =>
            new()
            {
                SurvivalDeaths = survivalDeaths,
                SurvivalWins = survivalWins,
                FriendsKilled = friendsKilled,
                MonsterKills = monsterKills ?? [],
            };

        private static string L10n(string key, long count = 0, string? name = null)
        {
            if (name == null)
            {
                return ModL10n.GetForLocale("en", key, new Dictionary<string, object> { ["count"] = count });
            }

            return ModL10n.GetForLocale("en", key, new Dictionary<string, object>
            {
                ["count"] = count,
                ["name"] = name,
            });
        }
    }
}
