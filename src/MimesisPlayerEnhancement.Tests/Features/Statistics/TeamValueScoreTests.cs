using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class TeamValueScoreTests
    {
        [Fact]
        public void Compute_weights_train_items_and_penalizes_friend_kills()
        {
            StatCounters good = new()
            {
                TrainValueDeposited = 100,
                ItemsDeposited = 2,
                MonsterKills = { ["monster:1"] = 4 },
                Revives = 1,
            };
            StatCounters bad = new()
            {
                FriendsKilled = 1,
                DamageToFriend = 10,
                Deaths = 2,
            };

            Assert.True(TeamValueScore.Compute(good) > TeamValueScore.Compute(bad));
        }
    }
}
