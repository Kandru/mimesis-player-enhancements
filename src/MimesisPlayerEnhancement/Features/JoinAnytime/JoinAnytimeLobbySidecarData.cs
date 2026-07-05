namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    internal sealed class JoinAnytimeLobbySidecarData
    {
        internal const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public string? BaseLobbyName;
        public bool? IsPublicLobby;
        public Dictionary<string, string>? Custom;
    }
}
