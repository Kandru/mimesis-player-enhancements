using System.Reflection;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>
    /// Host-side fix for vanilla waiting on inflated session counts instead of in-room load completion.
    /// Always active (not gated on <see cref="ModConfig.EnableJoinAnytime"/>).
    /// </summary>
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

        private static readonly HashSet<long> LoggedMismatchRoomIds = [];

        internal static void ResetSessionState()
        {
            LoggedMismatchRoomIds.Clear();
        }

        internal static void TryCompleteEnterHandshake(IVroom room)
        {
            if (!HostApplyGate.ShouldApplyHostOnlyFeature())
            {
                return;
            }

            if (room == null
                || StartNotifiedField?.GetValue(room) is not false
                || OnAllMemberEnteredMethod == null)
            {
                if (room != null && StartNotifiedField?.GetValue(room) is true)
                {
                    LoggedMismatchRoomIds.Remove(room.RoomID);
                }

                return;
            }

            if (!TryCountRoomMembers(room, out int expectedMembers, out int loadedMembers))
            {
                return;
            }

            PruneStaleLevelLoadIds(room);

            if (!JoinAnytimeRoomLoadingHandshakeLogic.ResolveReadyToEnter(expectedMembers, loadedMembers))
            {
                return;
            }

            int vanillaExpected = GameSessionAccess.TryGetVWorld()
                ?.GetRoomTypeMemberCount(room.Property.vRoomType) ?? -1;
            int loadCompleteIdCount = GetLevelLoadCompleteIdCount(room);
            if (vanillaExpected != loadCompleteIdCount)
            {
                LogMismatchOnce(
                    room.RoomID,
                    vanillaExpected,
                    loadCompleteIdCount,
                    expectedMembers,
                    loadedMembers);
            }

            OnAllMemberEnteredMethod.Invoke(room, null);
        }

        private static bool TryCountRoomMembers(IVroom room, out int expectedMembers, out int loadedMembers)
        {
            expectedMembers = 0;
            loadedMembers = 0;

            if (VPlayerDictField?.GetValue(room) is not VActorDict<int, VPlayer> players)
            {
                return false;
            }

            foreach (VPlayer player in players.Values)
            {
                if (player == null || player.IsDummy)
                {
                    continue;
                }

                expectedMembers++;
                if (player.LevelLoadCompleted)
                {
                    loadedMembers++;
                }
            }

            return true;
        }

        private static void PruneStaleLevelLoadIds(IVroom room)
        {
            if (LevelLoadCompleteIdsField?.GetValue(room) is not HashSet<ulong> loadCompleteIds
                || VPlayerDictField?.GetValue(room) is not VActorDict<int, VPlayer> players)
            {
                return;
            }

            HashSet<ulong> liveSteamIds = [];
            foreach (VPlayer player in players.Values)
            {
                if (player == null || player.IsDummy)
                {
                    continue;
                }

                ulong steamId = player.SteamID;
                if (steamId != 0)
                {
                    liveSteamIds.Add(steamId);
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

        private static int GetLevelLoadCompleteIdCount(IVroom room) =>
            LevelLoadCompleteIdsField?.GetValue(room) is HashSet<ulong> loadCompleteIds
                ? loadCompleteIds.Count
                : -1;

        private static void LogMismatchOnce(
            long roomId,
            int vanillaExpected,
            int loadCompleteIdCount,
            int expectedMembers,
            int loadedMembers)
        {
            if (!LoggedMismatchRoomIds.Add(roomId))
            {
                return;
            }

            ModLog.Warn(
                Feature,
                $"Loading handshake — early start room={roomId} vanillaExpected={vanillaExpected} "
                + $"loadCompleteIds={loadCompleteIdCount} roomMembers={expectedMembers} loaded={loadedMembers}");

            if (ModConfig.EnableDebugLogging.Value)
            {
                ModLog.Debug(
                    Feature,
                    $"Loading handshake — vanilla waits until loadCompleteIds == GetRoomTypeMemberCount; "
                    + $"room uses in-room LevelLoadCompleted instead (room={roomId})");
            }
        }
    }
}
