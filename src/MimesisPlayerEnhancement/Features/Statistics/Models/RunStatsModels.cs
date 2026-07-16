namespace MimesisPlayerEnhancement.Features.Statistics.Models
{
    public sealed class RunStats
    {
        public DateTime StartedAtUtc;
        public StatCounters Counters = new();
        public Dictionary<int, StatCounters> Zones = [];
    }

    public sealed class EntityCountEntry
    {
        public string Key = "";
        public string DisplayName = "";
        public string LocalizationKey = "";
        public long Count;
    }

    public sealed class LeaderboardZoneSummary
    {
        public int Zone;
        public StatCounters Totals = new();
    }
}
