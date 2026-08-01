using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.DungeonTime
{
    /// <summary>
    /// Outdoor <c>worldTime</c> follow when Dungeon Time display rate ≠ vanilla
    /// (host stretch/multiplier and modded clients with mirrored config).
    /// </summary>
    internal static class DungeonTimeClientWorldClock
    {
        private static readonly FieldInfo WorldTimeField =
            AccessTools.Field(typeof(GamePlayScene), "worldTime")
            ?? throw new InvalidOperationException("GamePlayScene.worldTime not found");

        private static bool _hasSync;
        private static double _baseTotalHours;
        private static float _syncedAtRealtime;
        private static double _rate = 1d;
        private static long _startSeconds;
        private static long _endSeconds;

        internal static void RefreshFromConfig()
        {
            if (!IsDisplayFollowActive())
            {
                Clear();
                return;
            }

            RefreshBoundsAndRate();
        }

        internal static void Clear()
        {
            _hasSync = false;
            _rate = 1d;
            _startSeconds = 0;
            _endSeconds = 0;
        }

        internal static bool IsDisplayFollowActive() =>
            SceneScopedConfigGate.DungeonTime.EnableDungeonTime;

        internal static void OnTimeSync(TimeSpan currentTime)
        {
            if (!IsDisplayFollowActive())
            {
                return;
            }

            RefreshBoundsAndRate();
            // Keep continuous hours (24 at shift end) so reverse can clamp to start without wrapping.
            _baseTotalHours = currentTime.TotalHours;
            _syncedAtRealtime = Time.realtimeSinceStartup;
            _hasSync = true;

            if (GameSessionAccess.TryGetPdata()?.main is GamePlayScene scene)
            {
                SetWorldTime(scene, ComputeHours());
            }
        }

        internal static void ApplyToSetTime(GamePlayScene scene, ref float newHours)
        {
            if (!_hasSync || !IsDisplayFollowActive() || scene == null)
            {
                return;
            }

            if (!DungeonTimeResolver.IsNonVanillaDisplayRate(_rate) && _rate > 0d)
            {
                return;
            }

            float hours = ComputeHours();
            newHours = hours;
            SetWorldTime(scene, hours);
        }

        internal static float ComputeHours()
        {
            long scaleFactor = HubGameDataAccess.Excel?.Consts.C_GameTimeScaleFactor ?? 60_000L;
            float gameTimeScale = scaleFactor / 1000f;
            float elapsed = Time.realtimeSinceStartup - _syncedAtRealtime;
            double hours = _baseTotalHours + (elapsed * _rate * gameTimeScale / 3600d);
            hours = DungeonTimeResolver.ClampDisplayHours(hours, _startSeconds, _endSeconds);
            return DungeonTimeResolver.ToClockFaceHours(hours);
        }

        private static void RefreshBoundsAndRate()
        {
            DungeonTimeSceneConfig config = SceneScopedConfigGate.DungeonTime;
            _rate = 1d;
            _startSeconds = 0;
            _endSeconds = 0;

            if (!config.EnableDungeonTime)
            {
                return;
            }

            double stretch = 1d;
            foreach (KeyValuePair<DungeonRoom, DungeonTimeRoomState> entry in DungeonTimeRuntime.RoomStates.EnumerateAll())
            {
                DungeonRoom room = entry.Key;
                _startSeconds = DungeonTimeClockResolver.GetEffectiveStartSeconds(room, config);
                _endSeconds = DungeonTimeRoomAccess.GetEndSeconds(room);
                stretch = DungeonTimeResolver.GetStretchScale(
                    entry.Value.BaseRemainingMs,
                    entry.Value.ExtendedRemainingMs);
                if (stretch < 1d)
                {
                    break;
                }
            }

            _rate = DungeonTimeResolver.GetEffectiveDisplayRate(
                stretch,
                ModConfig.TimeMultiplier.Value);
        }

        private static void SetWorldTime(GamePlayScene scene, float hours)
        {
            WorldTimeField.SetValue(scene, hours);
        }
    }
}
