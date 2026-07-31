namespace MimesisPlayerEnhancement.Features.SavegamePreparation
{
    /// <summary>
    /// Armed when the host starts a new save from the main menu; disarmed after the first save file is written.
    /// </summary>
    internal static class SavegamePreparationNewGameGate
    {
        private static int _depth;

        internal static bool IsArmed => _depth > 0;

        internal static void Arm()
        {
            _depth++;
        }

        internal static void Disarm()
        {
            if (_depth > 0)
            {
                _depth--;
            }
        }

        internal static void Reset()
        {
            _depth = 0;
        }
    }
}
