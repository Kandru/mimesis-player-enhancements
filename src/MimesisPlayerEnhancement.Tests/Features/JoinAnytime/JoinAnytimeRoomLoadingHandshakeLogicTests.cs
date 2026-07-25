using MimesisPlayerEnhancement.Features.JoinAnytime;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.JoinAnytime
{
    public sealed class JoinAnytimeRoomLoadingHandshakeLogicTests
    {
        [Theory]
        [InlineData(2, 0, 2)]
        [InlineData(3, 1, 2)]
        [InlineData(2, 2, 0)]
        [InlineData(0, 1, 0)]
        [InlineData(-1, 0, 0)]
        public void AdjustSessionExpected_subtracts_non_blocking_limbo(
            int sessionExpected,
            int nonBlocking,
            int expected)
        {
            Assert.Equal(
                expected,
                JoinAnytimeRoomLoadingHandshakeLogic.AdjustSessionExpected(sessionExpected, nonBlocking));
        }

        [Theory]
        [InlineData(0, 0, 1, false)]
        [InlineData(1, 0, 1, false)]
        [InlineData(1, 1, 1, true)]
        [InlineData(2, 2, 2, true)]
        [InlineData(4, 3, 4, false)]
        [InlineData(4, 4, 4, true)]
        // Transfer race: host alone while teammate still entering — must wait (not the 40s hang case).
        [InlineData(1, 1, 2, false)]
        [InlineData(2, 2, 3, false)]
        [InlineData(1, 1, 0, false)]
        [InlineData(3, 3, 2, true)]
        public void ResolveReadyToEnter_balances_transfer_wait_and_early_start(
            int roomMembers,
            int loadedMembers,
            int adjustedSessionExpected,
            bool expected)
        {
            bool actual = JoinAnytimeRoomLoadingHandshakeLogic.ResolveReadyToEnter(
                roomMembers,
                loadedMembers,
                adjustedSessionExpected);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void End_to_end_late_join_limbo_does_not_block_dungeon_party()
        {
            // Raw sessionExpected=3 (2 in dungeon + 1 AwaitingClient); adjusted=2.
            int adjusted = JoinAnytimeRoomLoadingHandshakeLogic.AdjustSessionExpected(
                sessionExpected: 3,
                nonBlockingSessions: 1);

            Assert.True(JoinAnytimeRoomLoadingHandshakeLogic.ResolveReadyToEnter(
                roomMembers: 2,
                loadedMembers: 2,
                adjustedSessionExpected: adjusted));
        }

        [Fact]
        public void End_to_end_maintenance_return_waits_for_transferring_teammate()
        {
            // Raw sessionExpected=2, no limbo; host alone in room — do not start.
            int adjusted = JoinAnytimeRoomLoadingHandshakeLogic.AdjustSessionExpected(
                sessionExpected: 2,
                nonBlockingSessions: 0);

            Assert.False(JoinAnytimeRoomLoadingHandshakeLogic.ResolveReadyToEnter(
                roomMembers: 1,
                loadedMembers: 1,
                adjustedSessionExpected: adjusted));
        }
    }
}
