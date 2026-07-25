namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>
    /// Pure gate for host room-enter sync. See <see cref="JoinAnytimeRoomLoadingHandshake"/>.
    /// </summary>
    internal static class JoinAnytimeRoomLoadingHandshakeLogic
    {
        /// <summary>
        /// Session players that inflate <c>GetRoomTypeMemberCount</c> but will not enter this room
        /// (JoinAnytime <see cref="LateJoinRoutePhase.AwaitingClient"/> limbo).
        /// </summary>
        internal static int AdjustSessionExpected(int sessionExpected, int nonBlockingSessions)
        {
            if (sessionExpected <= 0)
            {
                return 0;
            }

            return System.Math.Max(0, sessionExpected - System.Math.Max(0, nonBlockingSessions));
        }

        /// <summary>
        /// Ready when every VPlayer already in the room has finished loading, and the in-room
        /// count covers the <b>adjusted</b> session expectation for this room type.
        /// </summary>
        /// <remarks>
        /// Two failure modes this gate balances:
        /// <list type="bullet">
        /// <item>
        /// <b>Vanilla ~40s hang (must still avoid):</b> everyone who belongs in the room is loaded,
        /// but <c>_levelLoadCompleteActorIDs.Count != GetRoomTypeMemberCount</c> (stale IDs, or
        /// session count inflated by JoinAnytime limbo). Early-start once in-room membership covers
        /// the adjusted expectation — do not wait for the vanilla ID equality.
        /// </item>
        /// <item>
        /// <b>Transfer race (must wait):</b> host arrives first after dungeon→maintenance (or similar);
        /// <c>roomMembers</c> is briefly 1 while session still expects 2. Early-starting here sends
        /// <c>AllMemberEnterRoomSig</c> without the teammate and leaves vanilla clients stuck on the
        /// loading screen (can move/hear, invisible to host). Require
        /// <c>roomMembers &gt;= adjustedSessionExpected</c>.
        /// </item>
        /// </list>
        /// Do not treat brief no-VPlayer mid-snapshot transfers as "non-blocking" — only known
        /// JoinAnytime AwaitingClient limbo is subtracted when building
        /// <paramref name="adjustedSessionExpected"/>.
        /// </remarks>
        internal static bool ResolveReadyToEnter(
            int roomMembers,
            int loadedMembers,
            int adjustedSessionExpected)
        {
            if (roomMembers <= 0 || loadedMembers != roomMembers)
            {
                return false;
            }

            if (adjustedSessionExpected <= 0)
            {
                return false;
            }

            return roomMembers >= adjustedSessionExpected;
        }
    }
}
