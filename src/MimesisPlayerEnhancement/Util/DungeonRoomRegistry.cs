using System;
using System.Runtime.CompilerServices;

namespace MimesisPlayerEnhancement.Util
{
    [Flags]
    internal enum DungeonRoomApplyKind
    {
        SpawnScaling = 1 << 0,
        MapPlacedEncounters = 1 << 1,
        LootScaling = 1 << 2,
        FixedLootCoordination = 1 << 3,
        DungeonTime = 1 << 4,
        Weather = 1 << 5,
        SpawnScalingSkippedOnce = 1 << 8,
        LootScalingSkippedOnce = 1 << 9,
    }

    internal static class DungeonRoomAppliedSet
    {
        private sealed class RoomApplyState
        {
            internal DungeonRoomApplyKind Flags;
        }

        private static readonly ConditionalWeakTable<DungeonRoom, RoomApplyState> States = new();

        internal static bool IsApplied(DungeonRoom room, DungeonRoomApplyKind kind)
        {
            return States.TryGetValue(room, out RoomApplyState? state)
                && (state.Flags & kind) != 0;
        }

        internal static void MarkApplied(DungeonRoom room, DungeonRoomApplyKind kind)
        {
            RoomApplyState state = GetOrCreateState(room);
            state.Flags |= kind;
        }

        internal static bool MarkSkippedOnce(DungeonRoom room, DungeonRoomApplyKind skipKind)
        {
            RoomApplyState state = GetOrCreateState(room);
            if ((state.Flags & skipKind) != 0)
            {
                return false;
            }

            state.Flags |= skipKind;
            return true;
        }

        private static RoomApplyState GetOrCreateState(DungeonRoom room)
        {
            if (!States.TryGetValue(room, out RoomApplyState? state))
            {
                state = new RoomApplyState();
                States.Add(room, state);
            }

            return state;
        }
    }

    internal sealed class DungeonRoomStateRegistry<T> where T : class
    {
        private readonly ConditionalWeakTable<DungeonRoom, T> _states = new();
        private readonly List<WeakReference<DungeonRoom>> _tracked = [];

        internal T GetOrCreate(DungeonRoom room, Func<T> factory)
        {
            if (!_states.TryGetValue(room, out T? state))
            {
                state = factory();
                _states.Add(room, state);
                Track(room);
            }

            return state;
        }

        internal bool TryGet(DungeonRoom room, out T state)
        {
            if (_states.TryGetValue(room, out T? value) && value != null)
            {
                state = value;
                return true;
            }

            state = null!;
            return false;
        }

        internal void Register(DungeonRoom room, T state)
        {
            _states.Add(room, state);
            Track(room);
        }

        internal IEnumerable<KeyValuePair<DungeonRoom, T>> EnumerateAll()
        {
            PruneDead();
            List<WeakReference<DungeonRoom>> trackedSnapshot = [.. _tracked];
            foreach (WeakReference<DungeonRoom> weak in trackedSnapshot)
            {
                if (!weak.TryGetTarget(out DungeonRoom? room))
                {
                    continue;
                }

                if (_states.TryGetValue(room, out T? state) && state != null)
                {
                    yield return new KeyValuePair<DungeonRoom, T>(room, state);
                }
            }
        }

        private void Track(DungeonRoom room)
        {
            PruneDead();
            foreach (WeakReference<DungeonRoom> weak in _tracked)
            {
                if (weak.TryGetTarget(out DungeonRoom? existing) && ReferenceEquals(existing, room))
                {
                    return;
                }
            }

            _tracked.Add(new WeakReference<DungeonRoom>(room));
        }

        private void PruneDead()
        {
            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                if (!_tracked[i].TryGetTarget(out _))
                {
                    _tracked.RemoveAt(i);
                }
            }
        }
    }
}
