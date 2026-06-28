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
    }

    internal sealed class WebDashboardSnapshot
    {
        public WebDashboardStatusDto Status = new();
        public List<WebDashboardPlayerDto> Players = [];
        public string? LeaderboardJson;
        public List<ulong> ConnectedSteamIds = [];
        public Dictionary<ulong, string> PlayerStatsJson = [];
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
}
