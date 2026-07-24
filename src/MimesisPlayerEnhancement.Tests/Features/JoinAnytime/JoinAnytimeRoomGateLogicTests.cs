using MimesisPlayerEnhancement.Features.JoinAnytime;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.JoinAnytime
{
    public sealed class JoinAnytimeRoomGateLogicTests
    {
        [Theory]
        [InlineData(false, true, true, 3, false)]
        [InlineData(true, false, true, 3, false)]
        [InlineData(true, true, false, 0, true)]
        [InlineData(true, true, true, 3, true)]
        [InlineData(true, true, true, 4, false)] // OnPlaying
        [InlineData(true, true, true, 5, false)] // AfterGame
        [InlineData(true, true, true, 7, false)] // DeathMatch
        public void ResolveJoinsOpen_matches_host_scene_and_session_state(
            bool isHost,
            bool inJoinableHostScene,
            bool hasVRoomManager,
            int sessionState,
            bool expected)
        {
            bool actual = JoinAnytimeRoomGateLogic.ResolveJoinsOpen(
                isHost,
                inJoinableHostScene,
                hasVRoomManager,
                sessionState);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ResolveWaitingRoomBlockReason_pending_connecting_wins()
        {
            WaitingRoomBlockReason actual = JoinAnytimeRoomGateLogic.ResolveWaitingRoomBlockReason(
                hasPendingConnecting: true,
                hasVRoomManager: true,
                sessionStateValue: 3,
                occupiedDungeonBlocksDeparture: false,
                sessionPlayers: 2,
                waitingPlayers: 2);

            Assert.Equal(WaitingRoomBlockReason.PlayersConnecting, actual);
        }

        [Theory]
        [InlineData(4)] // OnPlaying
        [InlineData(5)] // AfterGame
        public void ResolveWaitingRoomBlockReason_active_session_states_block(int sessionState)
        {
            WaitingRoomBlockReason actual = JoinAnytimeRoomGateLogic.ResolveWaitingRoomBlockReason(
                hasPendingConnecting: false,
                hasVRoomManager: true,
                sessionStateValue: sessionState,
                occupiedDungeonBlocksDeparture: false,
                sessionPlayers: 2,
                waitingPlayers: 2);

            Assert.Equal(WaitingRoomBlockReason.ActiveDungeon, actual);
        }

        [Fact]
        public void ResolveWaitingRoomBlockReason_deathmatch_does_not_block()
        {
            WaitingRoomBlockReason actual = JoinAnytimeRoomGateLogic.ResolveWaitingRoomBlockReason(
                hasPendingConnecting: false,
                hasVRoomManager: true,
                sessionStateValue: 7,
                occupiedDungeonBlocksDeparture: true,
                sessionPlayers: 2,
                waitingPlayers: 1);

            Assert.Equal(WaitingRoomBlockReason.None, actual);
        }

        [Fact]
        public void ResolveWaitingRoomBlockReason_occupied_dungeon_blocks()
        {
            WaitingRoomBlockReason actual = JoinAnytimeRoomGateLogic.ResolveWaitingRoomBlockReason(
                hasPendingConnecting: false,
                hasVRoomManager: true,
                sessionStateValue: 3,
                occupiedDungeonBlocksDeparture: true,
                sessionPlayers: 3,
                waitingPlayers: 1);

            Assert.Equal(WaitingRoomBlockReason.ActiveDungeon, actual);
        }

        [Fact]
        public void ResolveWaitingRoomBlockReason_split_party_blocks()
        {
            WaitingRoomBlockReason actual = JoinAnytimeRoomGateLogic.ResolveWaitingRoomBlockReason(
                hasPendingConnecting: false,
                hasVRoomManager: true,
                sessionStateValue: 3,
                occupiedDungeonBlocksDeparture: false,
                sessionPlayers: 3,
                waitingPlayers: 1);

            Assert.Equal(WaitingRoomBlockReason.PlayersSplit, actual);
        }

        [Fact]
        public void ResolveWaitingRoomBlockReason_all_in_waiting_allows()
        {
            WaitingRoomBlockReason actual = JoinAnytimeRoomGateLogic.ResolveWaitingRoomBlockReason(
                hasPendingConnecting: false,
                hasVRoomManager: true,
                sessionStateValue: 3,
                occupiedDungeonBlocksDeparture: false,
                sessionPlayers: 3,
                waitingPlayers: 3);

            Assert.Equal(WaitingRoomBlockReason.None, actual);
        }
    }
}
