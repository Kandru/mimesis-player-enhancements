namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound
{
    internal static class DungeonLandingEntryTracker
    {
        private static bool _isActive;
        private static float _closeAtUnscaledTime = float.PositiveInfinity;

        internal static bool IsActive =>
            _isActive && UnityEngine.Time.unscaledTime < _closeAtUnscaledTime;

        internal static void Begin()
        {
            _isActive = true;
            _closeAtUnscaledTime = float.PositiveInfinity;
        }

        internal static void ScheduleCloseAfterEnterGame()
        {
            _closeAtUnscaledTime = UnityEngine.Time.unscaledTime + RoundStartSoundConstants.EntryWindowCloseDelaySeconds;
        }

        internal static void End()
        {
            _isActive = false;
            _closeAtUnscaledTime = float.PositiveInfinity;
        }
    }
}
