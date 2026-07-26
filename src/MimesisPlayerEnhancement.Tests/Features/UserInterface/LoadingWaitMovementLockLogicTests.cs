using MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitMovementLock;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class LoadingWaitMovementLockLogicTests
    {
        [Theory]
        [InlineData(true, true, 2, true)]
        [InlineData(true, true, 4, true)]
        [InlineData(true, true, 1, false)]
        [InlineData(true, false, 2, false)]
        [InlineData(false, true, 2, false)]
        [InlineData(false, false, 2, false)]
        public void ShouldDeferInputUnlock_requires_gameplay_loading_and_multiplayer(
            bool isGamePlayScene,
            bool loadingVisible,
            int playerCount,
            bool expected)
        {
            Assert.Equal(
                expected,
                LoadingWaitMovementLockLogic.ShouldDeferInputUnlock(
                    isGamePlayScene,
                    loadingVisible,
                    playerCount));
        }
    }
}
