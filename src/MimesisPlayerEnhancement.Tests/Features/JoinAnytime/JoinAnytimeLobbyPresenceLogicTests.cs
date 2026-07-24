using MimesisPlayerEnhancement.Features.JoinAnytime;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.JoinAnytime
{
    public sealed class JoinAnytimeLobbyPresenceLogicTests
    {
        [Fact]
        public void Resolve_returns_none_when_not_public()
        {
            JoinAnytimePublicPresenceKind actual = JoinAnytimeLobbyPresenceLogic.Resolve(
                wantsPublic: false,
                JoinAnytimeSessionPhase.Maintenance,
                sessionCount: 1,
                waitingThreshold: 4);

            Assert.Equal(JoinAnytimePublicPresenceKind.None, actual);
        }

        [Fact]
        public void Resolve_uses_waiting_presence_in_maintenance_below_threshold()
        {
            JoinAnytimePublicPresenceKind actual = JoinAnytimeLobbyPresenceLogic.Resolve(
                wantsPublic: true,
                JoinAnytimeSessionPhase.Maintenance,
                sessionCount: 2,
                waitingThreshold: 4);

            Assert.Equal(JoinAnytimePublicPresenceKind.InLobbyPublicWaiting, actual);
        }

        [Fact]
        public void Resolve_uses_public_presence_in_maintenance_at_threshold()
        {
            JoinAnytimePublicPresenceKind actual = JoinAnytimeLobbyPresenceLogic.Resolve(
                wantsPublic: true,
                JoinAnytimeSessionPhase.Maintenance,
                sessionCount: 4,
                waitingThreshold: 4);

            Assert.Equal(JoinAnytimePublicPresenceKind.InLobbyPublic, actual);
        }

        [Fact]
        public void Resolve_uses_public_presence_outside_maintenance()
        {
            Assert.Equal(
                JoinAnytimePublicPresenceKind.InLobbyPublic,
                JoinAnytimeLobbyPresenceLogic.Resolve(
                    wantsPublic: true,
                    JoinAnytimeSessionPhase.Tram,
                    sessionCount: 1,
                    waitingThreshold: 4));

            Assert.Equal(
                JoinAnytimePublicPresenceKind.InLobbyPublic,
                JoinAnytimeLobbyPresenceLogic.Resolve(
                    wantsPublic: true,
                    JoinAnytimeSessionPhase.Dungeon,
                    sessionCount: 1,
                    waitingThreshold: 4));
        }
    }
}
