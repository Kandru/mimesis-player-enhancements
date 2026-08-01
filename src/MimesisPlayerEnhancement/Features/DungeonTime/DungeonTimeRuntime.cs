namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    internal sealed class DungeonTimeRoomState
    {
        internal long BaseRemainingMs;
        internal long ExtendedRemainingMs;
        /// <summary>Floor for reversing <c>_currentTime</c> (session clock at dungeon start).</summary>
        internal long? SessionStartCurrentMs;
        internal long? VanillaStartSeconds;
        internal int LastTramClockSyncHour = -1;
        internal int LastTramClockSyncMinute = -1;
    }

    internal static class DungeonTimeRuntime
    {
        internal static readonly DungeonRoomStateRegistry<DungeonTimeRoomState> RoomStates = new();

        internal static bool TryGetState(DungeonRoom room, out DungeonTimeRoomState state) =>
            RoomStates.TryGet(room, out state);

        internal static void CaptureSessionStartCurrent(DungeonRoom room)
        {
            if (room == null)
            {
                return;
            }

            DungeonTimeRoomState state = RoomStates.GetOrCreate(room, () => new DungeonTimeRoomState());
            if (!state.SessionStartCurrentMs.HasValue)
            {
                state.SessionStartCurrentMs = DungeonRoomSessionTime.GetCurrentMilliseconds(room);
            }
        }

        internal static void ArmStretchScale(DungeonRoom room, long baseRemainingMs, long bonusMs)
        {
            if (room == null || baseRemainingMs <= 0 || bonusMs <= 0)
            {
                return;
            }

            DungeonTimeRoomState state = RoomStates.GetOrCreate(room, () => new DungeonTimeRoomState());
            state.BaseRemainingMs = baseRemainingMs;
            state.ExtendedRemainingMs = baseRemainingMs + bonusMs;
        }

        internal static double GetStretchScale(DungeonRoom room)
        {
            if (!TryGetState(room, out DungeonTimeRoomState state))
            {
                return 1d;
            }

            return DungeonTimeResolver.GetStretchScale(state.BaseRemainingMs, state.ExtendedRemainingMs);
        }

        internal static float GetTimeMultiplier()
        {
            if (!SceneScopedConfigGate.DungeonTime.EnableDungeonTime)
            {
                return 1f;
            }

            return ModConfig.TimeMultiplier.Value;
        }

        /// <summary>Display clock rate: stretch × <see cref="GetTimeMultiplier"/>.</summary>
        internal static double GetEffectiveDisplayRate(DungeonRoom? room = null)
        {
            if (!SceneScopedConfigGate.DungeonTime.EnableDungeonTime)
            {
                return 1d;
            }

            double stretch = room != null ? GetStretchScale(room) : 1d;
            return DungeonTimeResolver.GetEffectiveDisplayRate(stretch, GetTimeMultiplier());
        }

        /// <summary>
        /// Prefaces <see cref="DungeonRoom.OnUpdate"/> so the upcoming
        /// <c>_currentTime += delta</c> / <c>_elapsedTime += delta</c> apply rates.
        /// Real session clock uses <see cref="GetTimeMultiplier"/>; display uses stretch × multiplier.
        /// </summary>
        internal static bool TryPrepareClocksForRates(DungeonRoom room, long deltaMs)
        {
            if (room == null
                || deltaMs <= 0
                || !HostApplyGate.ShouldApplyHostOnlyFeature(() => SceneScopedConfigGate.DungeonTime.EnableDungeonTime))
            {
                return false;
            }

            CaptureSessionStartCurrent(room);
            float timeMultiplier = GetTimeMultiplier();
            double displayRate = GetEffectiveDisplayRate(room);
            double realRate = timeMultiplier;

            bool changed = false;

            if (DungeonTimeResolver.IsNonVanillaDisplayRate(displayRate) || displayRate <= 0d)
            {
                long elapsed = DungeonRoomSessionTime.GetElapsedMilliseconds(room);
                long adjustedElapsed = DungeonTimeResolver.GetClockBeforeAdd(
                    elapsed,
                    deltaMs,
                    displayRate,
                    minValueMs: 0);
                if (adjustedElapsed != elapsed)
                {
                    DungeonRoomSessionTime.SetElapsedMilliseconds(room, adjustedElapsed);
                    changed = true;
                }
            }

            if (DungeonTimeResolver.IsNonVanillaDisplayRate(realRate) || realRate <= 0d)
            {
                long current = DungeonRoomSessionTime.GetCurrentMilliseconds(room);
                long minCurrent = 0;
                if (TryGetState(room, out DungeonTimeRoomState state) && state.SessionStartCurrentMs.HasValue)
                {
                    minCurrent = state.SessionStartCurrentMs.Value;
                }

                long adjustedCurrent = DungeonTimeResolver.GetClockBeforeAdd(
                    current,
                    deltaMs,
                    realRate,
                    minCurrent);
                if (adjustedCurrent != current)
                {
                    DungeonRoomSessionTime.SetCurrentMilliseconds(room, adjustedCurrent);
                    changed = true;
                }
            }

            return changed;
        }
    }
}
