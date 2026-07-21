using MimesisPlayerEnhancement;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Logging
{
    public sealed class PlayerLifecycleContributionTests
    {
        [Fact]
        public void FormatContributions_returns_empty_for_no_contributions()
        {
            string result = PlayerLifecycleCoordinator.FormatContributions([]);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void FormatContributions_joins_features_with_semicolon_separator()
        {
            PlayerLifecycleContribution[] contributions =
            [
                new PlayerLifecycleContribution("Persistence", "restored 12 voice events"),
                new PlayerLifecycleContribution("Statistics", "new session"),
            ];

            string result = PlayerLifecycleCoordinator.FormatContributions(contributions);

            Assert.Equal(
                "Persistence: restored 12 voice events; Statistics: new session",
                result);
        }

        [Fact]
        public void FormatContributions_skips_contributions_with_empty_detail()
        {
            PlayerLifecycleContribution[] contributions =
            [
                new PlayerLifecycleContribution("Persistence", "restored 12 voice events"),
                new PlayerLifecycleContribution("Statistics", string.Empty),
            ];

            string result = PlayerLifecycleCoordinator.FormatContributions(contributions);

            Assert.Equal("Persistence: restored 12 voice events", result);
        }

        [Fact]
        public void OrderedContributions_uses_default_persistence_then_statistics_order()
        {
            Dictionary<string, PlayerLifecycleContribution> contributions = new()
            {
                ["Statistics"] = new PlayerLifecycleContribution("Statistics", "reconnect #2"),
                ["Persistence"] = new PlayerLifecycleContribution("Persistence", "restored voices"),
            };

            List<PlayerLifecycleContribution> ordered =
                PlayerLifecycleCoordinator.OrderedContributions(contributions);

            Assert.Equal(2, ordered.Count);
            Assert.Equal("Persistence", ordered[0].Feature);
            Assert.Equal("Statistics", ordered[1].Feature);
        }

        [Fact]
        public void OrderedContributions_respects_custom_connecting_order()
        {
            Dictionary<string, PlayerLifecycleContribution> contributions = new()
            {
                ["Statistics"] = new PlayerLifecycleContribution("Statistics", "reconnect #2"),
                ["Persistence"] = new PlayerLifecycleContribution("Persistence", "connecting"),
            };

            List<PlayerLifecycleContribution> ordered = PlayerLifecycleCoordinator.OrderedContributions(
                contributions,
                order: ["Persistence"]);

            Assert.Single(ordered);
            Assert.Equal("Persistence", ordered[0].Feature);
        }
    }
}
