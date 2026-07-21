using MimesisPlayerEnhancement.Features.MorePlayers;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MorePlayers
{
    public sealed class MorePlayersPatchHelpersTests
    {
        [Fact]
        public void GetMaxPlayers_returns_vanilla_when_disabled()
        {
            Assert.Equal(MorePlayersPatchHelpers.VanillaMaxPlayers, MorePlayersPatchHelpers.GetMaxPlayers(false, 32));
        }

        [Theory]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(32)]
        public void GetMaxPlayers_returns_configured_value_when_enabled(int maxPlayers)
        {
            Assert.Equal(maxPlayers, MorePlayersPatchHelpers.GetMaxPlayers(true, maxPlayers));
        }

        [Fact]
        public void GetLobbyPlayerCountSuffix_returns_slash_prefixed_max()
        {
            Assert.Equal("/32", MorePlayersPatchHelpers.GetLobbyPlayerCountSuffix(true, 32));
        }

        [Fact]
        public void GetLobbyPlayerCountSuffix_uses_vanilla_when_disabled()
        {
            Assert.Equal("/4", MorePlayersPatchHelpers.GetLobbyPlayerCountSuffix(false, 32));
        }
    }
}
