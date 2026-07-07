namespace MimesisPlayerEnhancement.Features.WebDashboard.Models
{
    internal sealed class WebDashboardStatusDto
    {
        public bool IsConnected;
        public bool IsHost;
        public int SaveSlotId = -1;
        public string LobbyName = "";
        public string ModVersion = "";
        public string ListenUrl = "";
        public int SnapshotVersion;
        public int ConfigVersion;
        public int JoinAnytimeRoutingCount;
        public string Locale = "en";
    }

    internal sealed class WebDashboardSessionStatsDto
    {
        public long CurrencyEarned;
        public long SurvivalDeaths;
        public long SurvivalWins;
        public long SurvivalLeftBehind;
        public long DeathmatchDeaths;
        public long DeathmatchWins;
        public long Revives;
        public long MimicEncounterCount;
        public long ItemCarryCount;
        public long DamageToFriend;
        public long FriendsKilled;
        public long TotalConnectedSeconds;
        public Dictionary<string, long> MonsterKills = [];
        public Dictionary<string, long> DeathsByMonster = [];
        public Dictionary<string, long> DeathsByTrap = [];
    }

    internal sealed class WebDashboardPlayerDto
    {
        public ulong SteamId;
        public long PlayerUid;
        public string DisplayName = "";
        public bool IsHost;
        public bool IsLocal;
        public bool IsBanned;
        public bool IsAlive = true;
        public int NetworkGrade = -1;
        public string ConnectionRole = "";
        public string ConnectionAddress = "";
        public int VoiceLineCount;
        public WebDashboardSessionStatsDto? CurrentSession;
        public long? Health;
        public long? MaxHealth;
        public double? ToxicPercent;
        public string LateJoinPhase = "";
        public string LateJoinLabel = "";
        public float? LateJoinStuckSeconds;
        public int LateJoinAttemptCount;
        public bool GodMode;
        public bool NoClip;
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

    internal sealed class WebDashboardMinimapConnectionPointDto
    {
        public float X;
        public float Z;
        public float DirX;
        public float DirZ;
        public string FromTileId = "";
        public string ToTileId = "";
        public string TargetAreaId = "";
        public bool CrossArea;
    }

    internal sealed class WebDashboardMinimapAreaDto
    {
        public string Id = "";
        public string Label = "";
        public string Kind = "";
        public WebDashboardMinimapBoundsDto Bounds = new();
        public List<WebDashboardMinimapTileDto> Tiles = [];
        public List<WebDashboardMinimapConnectionPointDto> ConnectionPoints = [];
    }

    internal sealed class WebDashboardMinimapMarkerDto
    {
        public ulong SteamId;
        public string DisplayName = "";
        public float X;
        public float Z;
        public float Yaw;
        public string RoomName = "";
        public string AreaId = "";
        public string TileId = "";
        public bool IsAlive = true;
        public bool IsHost;
        public bool IsLocal;
    }

    internal sealed class WebDashboardMinimapTrainDto
    {
        public float X;
        public float Z;
        public float Yaw;
        public string AreaId = "";
    }

    internal sealed class WebDashboardMinimapLayoutDto
    {
        public int LayoutVersion;
        public string LayoutKind = "none";
        public string DisplayMode = "hidden";
        public string SceneLabel = "";
        public string DefaultAreaId = "";
        public WebDashboardMinimapBoundsDto Bounds = new();
        public List<WebDashboardMinimapAreaDto> Areas = [];
        public List<WebDashboardMinimapTileDto> Tiles = [];
        public List<WebDashboardMinimapConnectionDto> Connections = [];
    }

    internal sealed class WebDashboardSnapshot
    {
        public WebDashboardStatusDto Status = new();
        public List<WebDashboardPlayerDto> Players = [];
        public string? LeaderboardJson;
        public WebDashboardMinimapLayoutDto MinimapLayout = new();
        public List<WebDashboardMinimapMarkerDto> MinimapMarkers = [];
        public WebDashboardMinimapTrainDto? MinimapTrain;
    }

    internal enum WebDashboardActionType
    {
        Kick,
        Ban,
        Unban,
        Respawn,
        Heal,
        ToggleGodMode,
        ToggleNoClip,
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
        public int SaveSlotId = -1;
        public string Scope = "";
        public List<WebDashboardConfigSectionDto> Sections = [];
        public WebDashboardSaveProfileDto? Profile;
    }

    internal sealed class WebDashboardSaveProfileDto
    {
        public string Mode = "global";
        public string PresetId = "";
        public string Label = "";
    }

    internal sealed class WebDashboardQuickPresetDto
    {
        public string Id = "";
        public string Name = "";
        public string? Description;
        public bool IsBuiltin;
        public int Revision;
        public string? CreatedUtc;
        public string? UpdatedUtc;
    }

    internal sealed class WebDashboardQuickPresetsListDto
    {
        public List<WebDashboardQuickPresetDto> Presets = [];
    }

    internal sealed class WebDashboardSaveProfileRequest
    {
        public string Mode = "";
        public string PresetId = "";
    }

    internal sealed class WebDashboardSaveProfileResponseDto
    {
        public bool Success = true;
        public WebDashboardSaveProfileDto Profile = new();
        public int ConfigVersion;
        public string Message = "";
    }

    internal sealed class WebDashboardQuickPresetSaveRequest
    {
        public string PresetId = "";
        public string Name = "";
        public bool OverwriteExisting = false;
        public bool FromCurrentSave = true;
        public Dictionary<string, Dictionary<string, string>>? Values = null;
    }

    internal sealed class WebDashboardQuickPresetImportRequest
    {
        public string ShareString = "";
        public string? Name = null;
        public string PresetId = "";
        public bool OverwriteExisting = false;
        public bool SaveOnly = true;
    }

    internal sealed class WebDashboardQuickPresetShareDto
    {
        public string ShareString = "";
        public string Name = "";
    }

    internal sealed class WebDashboardQuickPresetImportResultDto
    {
        public bool Success;
        public string Message = "";
        public WebDashboardQuickPresetDto? Preset;
        public string ShareString = "";
    }

    internal sealed class WebDashboardConfigSectionDto
    {
        public string Id = "";
        public string Title = "";
        public WebDashboardConfigEntryDto? FeatureToggle;
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
        public string GlobalValue = "";
        public bool IsOverridden;
        public bool IsHidden;
        public string? MinValue;
        public string? MaxValue;
        public string InputKind = "Default";
        public string EntryGroup = "";
        public string? DependsOnKey;
        public string? DependsOnValue;
        public List<WebDashboardConfigSelectOptionDto> SelectOptions = [];
    }

    internal sealed class WebDashboardConfigSelectOptionDto
    {
        public string Value = "";
        public string Label = "";
    }

    internal sealed class WebDashboardDungeonOptionDto
    {
        public string Id = "";
        public string Label = "";
    }

    internal sealed class WebDashboardDungeonsApiResponse
    {
        public List<WebDashboardDungeonOptionDto> Dungeons = [];
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
        public int ConfigVersion;
        public string SectionId = "";
        public string Key = "";
        public string Value = "";
        public string Type = "";
        public bool IsOverridden;
    }

    internal sealed class WebDashboardItemOptionDto
    {
        public string Id = "";
        public string Label = "";
        public string Type = "";
        public int? MasterId;
        public int? SellPriceMin;
        public int? SellPriceMax;
        public List<WebDashboardItemVariantDto>? Variants;
    }

    internal sealed class WebDashboardItemVariantDto
    {
        public int Percent;
        public int MasterId;
        public int? SellPriceMin;
        public int? SellPriceMax;
    }

    internal sealed class WebDashboardItemsApiResponse
    {
        public List<WebDashboardItemOptionDto> Items = [];
    }

    internal sealed class WebDashboardSpawnItemRequest
    {
        public string ItemId = "";
        public int? Percent = null;
    }

    internal sealed class WebDashboardSpawnItemResult
    {
        public bool Success;
        public string Message = "";
        public string Location = "";
    }

    internal sealed class WebDashboardHostCheatsDto
    {
        public bool Success = true;
        public string Message = "";
        public bool GodMode;
        public bool NoClip;
    }

    internal sealed class WebDashboardHostCheatsUpdateRequest
    {
        public bool? DisableAll { get; set; }
    }
}
