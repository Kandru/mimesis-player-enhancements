using MimesisPlayerEnhancement.Config.QuickSettings;

namespace MimesisPlayerEnhancement.Config.Models
{
    internal sealed class SaveSlotDocument
    {
        internal const int CurrentVersion = 1;

        internal int Version = CurrentVersion;

        internal SaveSlotLobbySection? Lobby;

        internal SaveConfigProfileState SettingsProfile = new();

        internal Dictionary<string, Dictionary<string, string>>? ConfigOverrides;

        internal Dictionary<string, SaveSlotPlayerEntry>? Players;
    }

    internal sealed class SaveSlotLobbySection
    {
        internal string? BaseLobbyName;

        internal bool? IsPublicLobby;
    }

    internal sealed class SaveSlotPlayerEntry
    {
        internal string DisplayName = "";

        internal string VoiceId = "";
    }
}
