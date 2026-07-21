using MimesisPlayerEnhancement.Features.MorePlayers;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MorePlayers
{
    public sealed class VActorDictCapacityTests
    {
        [Fact]
        public void ResolveCap_returns_vanilla_when_disabled()
        {
            Assert.Equal(10, VActorDictCapacity.ResolveCap(false, 32));
        }

        [Theory]
        [InlineData(8, 10)]
        [InlineData(10, 10)]
        [InlineData(16, 16)]
        [InlineData(32, 32)]
        public void ResolveCap_returns_max_of_vanilla_and_max_players_when_enabled(int maxPlayers, int expected)
        {
            Assert.Equal(expected, VActorDictCapacity.ResolveCap(true, maxPlayers));
        }
    }
}
