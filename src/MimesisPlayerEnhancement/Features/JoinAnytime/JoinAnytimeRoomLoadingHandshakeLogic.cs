namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>
    /// Pure gate for host room-enter sync. See <see cref="JoinAnytimeRoomLoadingHandshake"/>.
    /// Do not compare against <c>GetSessionCount</c> / <c>GetRoomTypeMemberCount</c> here — that is
    /// the vanilla mismatch this feature avoids.
    /// </summary>
    internal static class JoinAnytimeRoomLoadingHandshakeLogic
    {
        internal static bool ResolveReadyToEnter(int expectedMembers, int loadedMembers) =>
            expectedMembers > 0 && loadedMembers == expectedMembers;
    }
}
