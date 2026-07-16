using MimesisPlayerEnhancement.Config.QuickSettings;

namespace MimesisPlayerEnhancement.Config.Models
{
    // Fields must be public: the game's runtime Newtonsoft.Json does not serialize non-public fields.
    internal sealed class SaveSlotDocument
    {
        internal const int CurrentVersion = 1;

        public int Version = CurrentVersion;

        public SaveSlotLobbySection? Lobby;

        public SaveConfigProfileState SettingsProfile = new();

        public Dictionary<string, Dictionary<string, string>>? ConfigOverrides;

        public Dictionary<string, SaveSlotPlayerEntry>? Players;
    }

    internal sealed class SaveSlotLobbySection
    {
        public string? BaseLobbyName;

        public bool? IsPublicLobby;
    }

    internal sealed class SaveSlotPlayerEntry
    {
        public string DisplayName = "";

        public string VoiceId = "";
    }
}
