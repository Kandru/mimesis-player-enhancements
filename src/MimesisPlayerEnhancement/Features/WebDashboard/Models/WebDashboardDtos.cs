using System.Collections.Generic;

namespace MimesisPlayerEnhancement.Features.WebDashboard.Models
{
    internal sealed class WebDashboardStatusDto
    {
        public bool InSession;
        public bool IsHost;
        public int SaveSlotId = -1;
        public string ModVersion = "";
        public string ListenUrl = "";
        public int SnapshotVersion;
        public int ConfigVersion;
    }

    internal sealed class WebDashboardSessionStatsDto
    {
        public long CurrencyEarned;
        public long Kills;
        public long Deaths;
        public long Revives;
        public long MimicEncounterCount;
        public long ItemCarryCount;
        public long VoiceEvents;
        public long DamageToAlly;
        public long TotalConnectedSeconds;
    }

    internal sealed class WebDashboardPlayerDto
    {
        public ulong SteamId;
        public long PlayerUid;
        public string DisplayName = "";
        public bool IsHost;
        public bool IsLocal;
        public bool IsBanned;
        public int NetworkGrade = -1;
        public string ConnectionRole = "";
        public string ConnectionAddress = "";
        public int VoiceEventCount;
        public WebDashboardSessionStatsDto? CurrentSession;
    }

    internal sealed class WebDashboardMinimapBoundsDto
    {
        public float MinX;
        public float MinZ;
        public float MaxX;
        public float MaxZ;
    }

    internal sealed class WebDashboardMinimapTileDto
    {
        public string Id = "";
        public string Label = "";
        public float X;
        public float Z;
        public float W;
        public float H;
        public bool IsMainPath;
    }

    internal sealed class WebDashboardMinimapConnectionDto
    {
        public string From = "";
        public string To = "";
    }

    internal sealed class WebDashboardMinimapMarkerDto
    {
        public ulong SteamId;
        public string DisplayName = "";
        public float X;
        public float Z;
        public float Yaw;
        public string RoomName = "";
        public bool IsAlive = true;
        public bool IsHost;
        public bool IsLocal;
    }

    internal sealed class WebDashboardMinimapTrainDto
    {
        public float X;
        public float Z;
        public float Yaw;
    }

    internal sealed class WebDashboardMinimapLayoutDto
    {
        public int LayoutVersion;
        public string LayoutKind = "none";
        public string SceneLabel = "";
        public WebDashboardMinimapBoundsDto Bounds = new();
        public List<WebDashboardMinimapTileDto> Tiles = [];
        public List<WebDashboardMinimapConnectionDto> Connections = [];
        public WebDashboardMinimapTrainDto? Train;
    }

    internal sealed class WebDashboardSnapshot
    {
        public WebDashboardStatusDto Status = new();
        public List<WebDashboardPlayerDto> Players = [];
        public string? LeaderboardJson;
        public List<ulong> ConnectedSteamIds = [];
        public Dictionary<ulong, string> PlayerStatsJson = [];
        public WebDashboardMinimapLayoutDto MinimapLayout = new();
        public List<WebDashboardMinimapMarkerDto> MinimapMarkers = [];
        public WebDashboardMinimapTrainDto? MinimapTrain;
    }

    internal enum WebDashboardActionType
    {
        Kick,
        Ban,
        Unban,
    }

    internal sealed class WebDashboardPendingAction
    {
        public WebDashboardActionType Type;
        public ulong SteamId;
        public long PlayerUid;
    }

    internal sealed class WebDashboardActionResult
    {
        public bool Success;
        public string Message = "";
    }

    internal sealed class WebDashboardSettingsDto
    {
        public string ConfigPath = "";
        public int ConfigVersion;
        public List<WebDashboardConfigSectionDto> Sections = [];
    }

    internal sealed class WebDashboardConfigSectionDto
    {
        public string Id = "";
        public string Title = "";
        public List<WebDashboardConfigEntryDto> Entries = [];
    }

    internal sealed class WebDashboardConfigEntryDto
    {
        public string Key = "";
        public string Title = "";
        public string Description = "";
        public string Type = "";
        public string Value = "";
        public string DefaultValue = "";
        public bool IsHidden;
    }

    internal sealed class WebDashboardConfigUpdateRequest
    {
        public string SectionId = "";
        public string Key = "";
        public string Value = "";
    }

    internal sealed class WebDashboardConfigUpdateResult
    {
        public bool Success;
        public string Message = "";
        public string SectionId = "";
        public string Key = "";
        public string Value = "";
        public string Type = "";
    }
}
