using MimesisPlayerEnhancement.Features.JoinAnytime;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.JoinAnytime
{
    public sealed class JoinAnytimeRoomLoadingHandshakeLogicTests
    {
        [Theory]
        [InlineData(0, 0, false)]
        [InlineData(1, 0, false)]
        [InlineData(1, 1, true)]
        [InlineData(4, 3, false)]
        [InlineData(4, 4, true)]
        public void ResolveReadyToEnter_requires_all_in_room_members_loaded(
            int expectedMembers,
            int loadedMembers,
            bool expected)
        {
            bool actual = JoinAnytimeRoomLoadingHandshakeLogic.ResolveReadyToEnter(
                expectedMembers,
                loadedMembers);

            Assert.Equal(expected, actual);
        }
    }
}
