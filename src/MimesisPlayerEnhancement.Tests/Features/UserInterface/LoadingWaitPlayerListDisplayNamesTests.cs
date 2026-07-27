using MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList;
using MimesisPlayerEnhancement.Util.Players;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class LoadingWaitPlayerListDisplayNamesTests
    {
        private const ulong SteamId = 0xA11CE;

        [Fact]
        public void Resolve_prefers_nick_name_over_registry()
        {
            PlayerRegistry.UpdateDisplayName(SteamId, "Registry Name");

            string result = LoadingWaitPlayerListDisplayNames.Resolve("Live Nick", SteamId);

            Assert.Equal("Live Nick", result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Resolve_uses_registry_when_nick_name_missing(string? nickName)
        {
            PlayerRegistry.UpdateDisplayName(SteamId, "Registry Name");

            string result = LoadingWaitPlayerListDisplayNames.Resolve(nickName, SteamId);

            Assert.Equal("Registry Name", result);
        }

        [Fact]
        public void Resolve_returns_empty_when_no_sources_available()
        {
            string result = LoadingWaitPlayerListDisplayNames.Resolve(null, SteamId);

            Assert.Equal(string.Empty, result);
        }
    }
}
