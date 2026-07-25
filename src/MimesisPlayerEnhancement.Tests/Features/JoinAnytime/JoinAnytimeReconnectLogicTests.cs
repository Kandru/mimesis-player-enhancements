using MimesisPlayerEnhancement.Features.JoinAnytime;
using ReluProtocol.Enum;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.JoinAnytime
{
    public sealed class JoinAnytimeReconnectLogicTests
    {
        [Theory]
        [InlineData(DisconnectReason.ByServer, true)]
        [InlineData(DisconnectReason.ConnectionError, true)]
        [InlineData(DisconnectReason.Undefined, true)]
        [InlineData(DisconnectReason.PacketError, true)]
        [InlineData(DisconnectReason.TransientDrop, true)]
        [InlineData(DisconnectReason.ByClient, false)]
        [InlineData(DisconnectReason.KickByServer, false)]
        [InlineData(DisconnectReason.DuplicateLogin, false)]
        [InlineData(DisconnectReason.KickByHost, false)]
        public void IsGraceEligible_matches_vanilla_disconnect_reasons(DisconnectReason reason, bool expected)
        {
            bool actual = JoinAnytimeReconnectLogic.IsGraceEligible(
                isVirtualAcceptSession: false,
                isDummy: false,
                steamId: 123,
                playerUid: 456,
                hasPlayer: true,
                hasPlayerSnapshot: true,
                reason);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0UL, 1L)]
        [InlineData(1UL, 0L)]
        public void IsGraceEligible_returns_false_when_steam_or_uid_missing(ulong steamId, long playerUid)
        {
            Assert.False(JoinAnytimeReconnectLogic.IsGraceEligible(
                isVirtualAcceptSession: false,
                isDummy: false,
                steamId,
                playerUid,
                hasPlayer: true,
                hasPlayerSnapshot: true,
                DisconnectReason.ConnectionError));
        }

        [Fact]
        public void IsGraceEligible_returns_false_for_dummy_session()
        {
            Assert.False(JoinAnytimeReconnectLogic.IsGraceEligible(
                isVirtualAcceptSession: false,
                isDummy: true,
                steamId: 1,
                playerUid: 2,
                hasPlayer: true,
                hasPlayerSnapshot: true,
                DisconnectReason.ConnectionError));
        }

        [Fact]
        public void IsGraceEligible_returns_false_for_virtual_accept_session()
        {
            Assert.False(JoinAnytimeReconnectLogic.IsGraceEligible(
                isVirtualAcceptSession: true,
                isDummy: false,
                steamId: 1,
                playerUid: 2,
                hasPlayer: true,
                hasPlayerSnapshot: true,
                DisconnectReason.ConnectionError));
        }

        [Fact]
        public void IsGraceEligible_returns_false_without_player_or_snapshot()
        {
            Assert.False(JoinAnytimeReconnectLogic.IsGraceEligible(
                isVirtualAcceptSession: false,
                isDummy: false,
                steamId: 1,
                playerUid: 2,
                hasPlayer: false,
                hasPlayerSnapshot: false,
                DisconnectReason.ConnectionError));
        }

        [Fact]
        public void IsGraceEligible_returns_true_with_snapshot_only()
        {
            Assert.True(JoinAnytimeReconnectLogic.IsGraceEligible(
                isVirtualAcceptSession: false,
                isDummy: false,
                steamId: 1,
                playerUid: 2,
                hasPlayer: false,
                hasPlayerSnapshot: true,
                DisconnectReason.TransientDrop));
        }
    }
}
