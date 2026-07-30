namespace MimesisPlayerEnhancement.Features.Statistics.Models
{
    public sealed class EntityCountEntry
    {
        public string Key = "";
        public string DisplayName = "";
        public string LocalizationKey = "";
        public long Count;
    }

    public sealed class LeaderboardEntry
    {
        public ulong SteamId;
        public string DisplayName = "";
        public double Score;
        public double AllTimeScore;
        public int HighestZoneReached;
        public int SessionsCompleted;
        public long RunRestarts;
        public int DungeonRunsPlayed;
        public StatCounters Global = new();
        public StatCounters CurrentZone = new();
    }

    public sealed class LeaderboardDocument
    {
        public int SaveSlotId;
        public int CurrentZone;
        public int HistoryRevision;
        public DateTime UpdatedAtUtc;
        public StatCounters ServerGlobalTotals = new();
        public StatCounters ServerZoneTotals = new();
        public List<LeaderboardEntry> Entries = [];
    }

    public sealed class StatisticsHistoryDocument
    {
        public int SaveSlotId;
        public int CurrentZone;
        public int HistoryRevision;
        public DateTime UpdatedAtUtc;
        public int TrimmedZoneCount;
        public List<StatisticsHistoryZone> Zones = [];
    }

    public sealed class StatisticsHistoryZone
    {
        public int Zone;
        public bool IsCurrent;
        public DateTime StartedAtUtc;
        public DateTime? EndedAtUtc;
        public int TrimmedRunCount;
        public StatCounters Totals = new();
        public List<StatisticsHistoryPlayerRow> Players = [];
        public List<StatisticsHistoryRun> Runs = [];
    }

    public sealed class StatisticsHistoryPlayerRow
    {
        public ulong SteamId;
        public string DisplayName = "";
        public StatCounters Counters = new();
    }

    public sealed class StatisticsHistoryRun
    {
        public string RunId = "";
        public int Zone;
        public int Cycle;
        public int Seed;
        public int MapId;
        public string MapKey = "";
        public string MapName = "";
        public int DungeonMasterId;
        public DateTime StartedAtUtc;
        public DateTime? EndedAtUtc;
        public long? DurationSeconds;
        public DungeonRunOutcome Outcome;
        public StatCounters Totals = new();
        public List<StatisticsHistoryPlayerRow> Players = [];
    }
}
