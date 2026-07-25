using System.Reflection;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>
    /// Host-only workaround for vanilla <c>IVroom.OnUpdate</c> (~L808 in 0.3.1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Vanilla behavior:</b> while <c>_startNotified</c> is false and within a ~40s window, start
    /// only when <c>_levelLoadCompleteActorIDs.Count == GetRoomTypeMemberCount</c>. After the window,
    /// it periodically force-starts. <c>OnAllMemberEntered</c> sends <c>AllMemberEnterRoomSig</c> and
    /// clears the client "waiting for other survivors" loading UI.
    /// </para>
    /// <para>
    /// <b>Why we intervene:</b> that ID==session-count equality often stays false even when every
    /// player who should be in the room already has <c>LevelLoadCompleted</c> (stale load-complete
    /// Steam IDs after room recycle; JoinAnytime AwaitingClient sessions inflating
    /// <c>GetSessionCount</c> for Waiting/Game). Clients then sit on the wait UI for up to ~40s.
    /// </para>
    /// <para>
    /// <b>Our rule:</b> early-start when all non-dummy VPlayers in this room's <c>_vPlayerDict</c>
    /// are loaded <i>and</i> in-room count covers the session expectation for this room type, after
    /// subtracting JoinAnytime AwaitingClient limbo (those players have no VPlayer and are not
    /// entering this room). Always active — not gated on <c>EnableJoinAnytime</c>.
    /// </para>
    /// <para>
    /// <b>Do not</b> early-start merely because the sole arrived host is loaded while a transferring
    /// teammate is not in <c>_vPlayerDict</c> yet (dungeon→maintenance race). That desyncs vanilla
    /// clients: loading screen stuck, can move/hear, invisible to host.
    /// </para>
    /// </remarks>
    internal static class JoinAnytimeRoomLoadingHandshake
    {
        private const string Feature = "JoinAnytime";

        private static readonly FieldInfo? StartNotifiedField =
            ReflectionFieldCache.GetField(typeof(IVroom), "_startNotified");

        private static readonly FieldInfo? LevelLoadCompleteIdsField =
            ReflectionFieldCache.GetField(typeof(IVroom), "_levelLoadCompleteActorIDs");

        private static readonly FieldInfo? VPlayerDictField =
            ReflectionFieldCache.GetField(typeof(IVroom), "_vPlayerDict");

        private static readonly MethodInfo? OnAllMemberEnteredMethod =
            AccessTools.Method(typeof(IVroom), "OnAllMemberEntered");

        private static readonly HashSet<long> LoggedStartRoomIds = [];

        internal static void ResetSessionState() => LoggedStartRoomIds.Clear();

        internal static void TryCompleteEnterHandshake(IVroom room)
        {
            if (!HostApplyGate.ShouldApplyHostOnlyFeature()
                || room == null
                || OnAllMemberEnteredMethod == null)
            {
                return;
            }

            if (StartNotifiedField?.GetValue(room) is not false)
            {
                if (StartNotifiedField?.GetValue(room) is true)
                {
                    LoggedStartRoomIds.Remove(room.RoomID);
                }

                return;
            }

            if (VPlayerDictField?.GetValue(room) is not VActorDict<int, VPlayer> players)
            {
                return;
            }

            // ResetEnvironment clears _startNotified but can leave stale Steam IDs in the set.
            PruneStaleLevelLoadIds(room, players);

            CountRoomMembers(players, out int roomMembers, out int loadedMembers);

            // GetRoomTypeMemberCount → GetSessionCount: Maintenance = all sessions; Waiting/Game =
            // sessions minus players currently in a maintenance room. AwaitingClient limbo has no
            // VPlayer, so vanilla does NOT exclude it for Waiting/Game — subtract it here or the
            // 40s hang returns whenever a late joiner is mid-route.
            int sessionExpected = GameSessionAccess.TryGetVWorld()
                ?.GetRoomTypeMemberCount(room.Property.vRoomType) ?? 0;
            int adjustedExpected = JoinAnytimeRoomLoadingHandshakeLogic.AdjustSessionExpected(
                sessionExpected,
                LateJoinRouteTracker.CountAwaitingClient());

            if (!JoinAnytimeRoomLoadingHandshakeLogic.ResolveReadyToEnter(
                    roomMembers,
                    loadedMembers,
                    adjustedExpected))
            {
                return;
            }

            LogStartOnce(room.RoomID, sessionExpected, adjustedExpected, roomMembers, loadedMembers);
            OnAllMemberEnteredMethod.Invoke(room, null);
        }

        private static void CountRoomMembers(
            VActorDict<int, VPlayer> players,
            out int roomMembers,
            out int loadedMembers)
        {
            roomMembers = 0;
            loadedMembers = 0;
            foreach (VPlayer player in players.Values)
            {
                if (player == null || player.IsDummy)
                {
                    continue;
                }

                roomMembers++;
                if (player.LevelLoadCompleted)
                {
                    loadedMembers++;
                }
            }
        }

        private static void PruneStaleLevelLoadIds(IVroom room, VActorDict<int, VPlayer> players)
        {
            if (LevelLoadCompleteIdsField?.GetValue(room) is not HashSet<ulong> loadCompleteIds)
            {
                return;
            }

            HashSet<ulong> liveSteamIds = [];
            foreach (VPlayer player in players.Values)
            {
                if (player is { IsDummy: false, SteamID: not 0 })
                {
                    liveSteamIds.Add(player.SteamID);
                }
            }

            int pruned = loadCompleteIds.RemoveWhere(id => !liveSteamIds.Contains(id));
            if (pruned > 0)
            {
                ModLog.Debug(
                    Feature,
                    $"Loading handshake — pruned {pruned} stale load-complete id(s) in room={room.RoomID}");
            }
        }

        private static void LogStartOnce(
            long roomId,
            int sessionExpected,
            int adjustedExpected,
            int roomMembers,
            int loadedMembers)
        {
            if (!LoggedStartRoomIds.Add(roomId))
            {
                return;
            }

            ModLog.Info(
                Feature,
                $"Loading handshake — start room={roomId} sessionExpected={sessionExpected} "
                + $"adjustedExpected={adjustedExpected} roomMembers={roomMembers} loaded={loadedMembers}");
        }
    }
}
