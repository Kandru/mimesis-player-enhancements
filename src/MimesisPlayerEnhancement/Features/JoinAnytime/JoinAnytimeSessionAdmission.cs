namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal static class JoinAnytimeSessionAdmission
    {
        // VGameSessionState values from game metadata (Ready=1, WaitStartSession=2, PreGame=3,
        // OnPlaying=4, AfterGame=5, EndGame=6, DeathMatch=7).
        internal static bool ResolveCanEnter(int stateValue, bool joinsOpen) =>
            stateValue switch
            {
                1 or 2 or 6 => true,
                4 or 5 or 7 => false,
                _ => joinsOpen,
            };
    }
}
