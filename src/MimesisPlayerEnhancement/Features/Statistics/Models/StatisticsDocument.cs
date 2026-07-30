namespace MimesisPlayerEnhancement.Features.Statistics.Models
{
    public sealed class PlayerGlobalStats
    {
        public ulong SteamId;
        public string DisplayName = "";
        public int HighestZoneReached = 1;
        public long RunRestarts;
        public int SessionsCompleted;
        public int DungeonRunsPlayed;
        public long VoiceEvents;
        public DateTime FirstSeenUtc;
        public DateTime LastSeenUtc;
        public StatCounters Counters = new();
    }

    public enum DungeonRunOutcome
    {
        InProgress = 0,
        Success = 1,
        Failed = 2,
        Abandoned = 3,
    }

    public sealed class DungeonRunRecord
    {
        public string RunId = "";
        public int Zone;
        public int Cycle;
        public int Seed;
        public int DungeonMasterId;
        public int MapId;
        public string MapKey = "";
        public string MapName = "";
        public DateTime StartedAtUtc;
        public DateTime? EndedAtUtc;
        public DungeonRunOutcome Outcome = DungeonRunOutcome.InProgress;
        public Dictionary<ulong, StatCounters> Players = [];
    }

    public sealed class ZoneRecord
    {
        public int Zone;
        public DateTime StartedAtUtc;
        public DateTime? EndedAtUtc;
        public Dictionary<ulong, StatCounters> Players = [];
        public List<DungeonRunRecord> Runs = [];
        public int TrimmedRunCount;
    }

    public sealed class ZoneHistory
    {
        public int CurrentZone = 1;
        public List<ZoneRecord> Zones = [];
        public int TrimmedZoneCount;
    }

    public sealed class SlotStatisticsDocument
    {
        public const int CurrentVersion = 10;

        public int Version = CurrentVersion;
        public DateTime UpdatedAtUtc;
        public Dictionary<ulong, PlayerGlobalStats> Globals = [];
        public ZoneHistory History = new();
    }
}
