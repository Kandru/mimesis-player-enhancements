namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>
    /// Pure join/open and tram-departure gate decisions (no Unity/Steam).
    /// VGameSessionState: Ready=1, WaitStartSession=2, PreGame=3, OnPlaying=4,
    /// AfterGame=5, EndGame=6, DeathMatch=7.
    /// </summary>
    internal static class JoinAnytimeRoomGateLogic
    {
        private const int OnPlaying = 4;
        private const int AfterGame = 5;
        private const int DeathMatch = 7;

        internal static bool ResolveJoinsOpen(
            bool isHost,
            bool inJoinableHostScene,
            bool hasVRoomManager,
            int sessionStateValue)
        {
            if (!isHost || !inJoinableHostScene)
            {
                return false;
            }

            if (!hasVRoomManager)
            {
                return true;
            }

            return sessionStateValue is not (OnPlaying or DeathMatch or AfterGame);
        }

        internal static WaitingRoomBlockReason ResolveWaitingRoomBlockReason(
            bool hasPendingConnecting,
            bool hasVRoomManager,
            int sessionStateValue,
            bool occupiedDungeonBlocksDeparture,
            int sessionPlayers,
            int waitingPlayers)
        {
            if (hasPendingConnecting)
            {
                return WaitingRoomBlockReason.PlayersConnecting;
            }

            if (!hasVRoomManager)
            {
                return WaitingRoomBlockReason.None;
            }

            if (sessionStateValue is OnPlaying or AfterGame)
            {
                return WaitingRoomBlockReason.ActiveDungeon;
            }

            if (sessionStateValue is DeathMatch)
            {
                return WaitingRoomBlockReason.None;
            }

            if (occupiedDungeonBlocksDeparture)
            {
                return WaitingRoomBlockReason.ActiveDungeon;
            }

            if (sessionPlayers <= 0)
            {
                return WaitingRoomBlockReason.None;
            }

            if (waitingPlayers < sessionPlayers)
            {
                return WaitingRoomBlockReason.PlayersSplit;
            }

            return WaitingRoomBlockReason.None;
        }
    }
}
