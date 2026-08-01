using MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList;
using MimesisPlayerEnhancement.Util.Players;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class LoadingWaitPlayerListDisplayNamesTests
    {
        [Fact]
        public void Resolve_prefers_nick_name_over_registry()
        {
            const ulong steamId = 0xA1101;
            PlayerRegistry.UpdateDisplayName(steamId, "Registry Name");
            try
            {
                string result = LoadingWaitPlayerListDisplayNames.Resolve("Live Nick", steamId);

                Assert.Equal("Live Nick", result);
            }
            finally
            {
                _ = PlayerRegistry.RemoveIfNeverConnected(steamId);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Resolve_uses_registry_when_nick_name_missing(string? nickName)
        {
            const ulong steamId = 0xA1102;
            PlayerRegistry.UpdateDisplayName(steamId, "Registry Name");
            try
            {
                string result = LoadingWaitPlayerListDisplayNames.Resolve(nickName, steamId);

                Assert.Equal("Registry Name", result);
            }
            finally
            {
                _ = PlayerRegistry.RemoveIfNeverConnected(steamId);
            }
        }

        [Fact]
        public void Resolve_returns_empty_when_no_sources_available()
        {
            const ulong steamId = 0xA1103;
            _ = PlayerRegistry.RemoveIfNeverConnected(steamId);

            string result = LoadingWaitPlayerListDisplayNames.Resolve(null, steamId);

            Assert.Equal(string.Empty, result);
        }
    }
}
