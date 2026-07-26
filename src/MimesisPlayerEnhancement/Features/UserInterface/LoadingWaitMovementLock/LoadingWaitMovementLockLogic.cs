namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitMovementLock
{
    internal static class LoadingWaitMovementLockLogic
    {
        internal const float MaxDeferSeconds = 40f;

        internal static bool ShouldDeferInputUnlock(
            bool isGamePlayScene,
            bool loadingVisible,
            int playerCount) =>
            isGamePlayScene && loadingVisible && playerCount > 1;
    }
}
