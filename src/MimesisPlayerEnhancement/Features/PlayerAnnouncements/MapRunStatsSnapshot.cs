namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements
{
    internal sealed class MapRunStatsSnapshot
    {
        public long ItemCarryCount;
        public long DamageToFriend;
        public long FriendsKilled;
        public long MimicEncounterCount;
        public long TimeInStartingVolumeMs;
        public long SurvivalDeaths;
        public long SurvivalWins;
        public long SurvivalLeftBehind;
        public long Revives;
        public Dictionary<string, long> MonsterKills = [];
    }
}
