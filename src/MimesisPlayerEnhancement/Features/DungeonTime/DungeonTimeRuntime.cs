namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal sealed class DungeonTimeRoomState
    {
        internal long BaseRemainingMs;
        internal long ExtendedRemainingMs;
        internal long? VanillaStartSeconds;
        internal int LastTramClockSyncHour = -1;
        internal int LastTramClockSyncMinute = -1;
    }

    internal static class DungeonTimeRuntime
    {
        internal static readonly DungeonRoomStateRegistry<DungeonTimeRoomState> RoomStates = new();

        internal static bool TryGetState(DungeonRoom room, out DungeonTimeRoomState state) =>
            RoomStates.TryGet(room, out state);

        internal static void ArmDisplayScale(DungeonRoom room, long baseRemainingMs, long bonusMs)
        {
            if (room == null || baseRemainingMs <= 0 || bonusMs <= 0)
            {
                return;
            }

            DungeonTimeRoomState state = RoomStates.GetOrCreate(room, () => new DungeonTimeRoomState());
            state.BaseRemainingMs = baseRemainingMs;
            state.ExtendedRemainingMs = baseRemainingMs + bonusMs;
        }

        /// <summary>
        /// Prefaces <see cref="DungeonRoom.OnUpdate"/> so the upcoming <c>_elapsedTime += delta</c>
        /// only applies the scaled portion. <c>_currentTime</c> still advances in real time.
        /// Vanilla hourly <see cref="TimeSyncSig"/> then fires at the slower display rate — no extra syncs.
        /// </summary>
        internal static bool TryPrepareElapsedForScaledDelta(DungeonRoom room, long deltaMs)
        {
            if (room == null
                || deltaMs <= 0
                || !TryGetState(room, out DungeonTimeRoomState state))
            {
                return false;
            }

            long scaledDelta = DungeonTimeResolver.ScaleElapsedDelta(
                deltaMs,
                state.BaseRemainingMs,
                state.ExtendedRemainingMs);
            if (scaledDelta >= deltaMs)
            {
                return false;
            }

            long excess = deltaMs - scaledDelta;
            long elapsed = DungeonRoomSessionTime.GetElapsedMilliseconds(room);
            DungeonRoomSessionTime.SetElapsedMilliseconds(room, elapsed - excess);
            return true;
        }
    }
}
