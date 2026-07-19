namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>
    /// Helpers for SetLobbyPublicCoroutine IL patches and public-room writes.
    /// </summary>
    internal static class JoinAnytimePublicLobbyTools
    {
        /// <summary>
        /// When the host requested a public lobby, do not let a stale ESC toggle downgrade PublicRoom.
        /// </summary>
        internal static bool CoercePublicRoomWriteFlag(bool isPublicRequested, bool toggleOrFallbackFlag) =>
            CoercePublicRoomWriteFlag(
                ModConfig.EnableJoinAnytime.Value,
                JoinAnytimeLobbyController.HostWantsPublicMatchmaking(),
                isPublicRequested,
                toggleOrFallbackFlag);

        internal static bool CoercePublicRoomWriteFlag(
            bool featureEnabled,
            bool hostWantsPublic,
            bool isPublicRequested,
            bool toggleOrFallbackFlag)
        {
            if (!featureEnabled)
            {
                return toggleOrFallbackFlag;
            }

            if (isPublicRequested || hostWantsPublic)
            {
                return true;
            }

            return toggleOrFallbackFlag;
        }
    }
}
