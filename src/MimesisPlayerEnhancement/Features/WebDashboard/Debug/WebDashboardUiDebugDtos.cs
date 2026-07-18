namespace MimesisPlayerEnhancement.Features.WebDashboard.Debug
{
    internal sealed class WebDashboardUiDebugStatusDto
    {
        public bool Ingame;
        public bool Alive;
        public int MaxPlayers;
        public bool Spectator;
        public bool LoadingWait;
        public bool EscMenu;
        public bool SurvivalResult;
    }

    internal sealed class WebDashboardUiDebugToggleRequest
    {
        public string Id = "";
    }

    internal sealed class WebDashboardUiDebugToggleResult
    {
        public bool Success;
        public string Id = "";
        public bool Active;
        public string Message = "";
    }
}
