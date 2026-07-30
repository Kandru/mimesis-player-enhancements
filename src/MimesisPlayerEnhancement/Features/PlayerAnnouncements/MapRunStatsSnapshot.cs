namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements
{
    internal sealed class MapRunStatsSnapshot
    {
        public long ItemCarryCount;
        public long DamageToFriend;
        public long FriendsKilled;
        public long MimicEncounterCount;
        public long TimeInStartingVolumeMs;
        public long Deaths;
        public long SurvivalWins;
        public long SurvivalLeftBehind;
        public long Revives;
        public long TrainValueDeposited;
        public long ItemsDeposited;
        public Dictionary<string, long> MonsterKills = [];
    }
}
