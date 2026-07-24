namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>
    /// Pure decision for host room-enter sync when every in-room player has finished loading.
    /// </summary>
    internal static class JoinAnytimeRoomLoadingHandshakeLogic
    {
        internal static bool ResolveReadyToEnter(int expectedMembers, int loadedMembers) =>
            expectedMembers > 0 && loadedMembers == expectedMembers;
    }
}
