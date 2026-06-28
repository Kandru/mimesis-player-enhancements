using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsJson
    {
        public static string SerializePlayer(PlayerStatisticsDocument doc)
        {
            return ModJson.Serialize(doc);
        }

        public static PlayerStatisticsDocument? DeserializePlayer(string json)
        {
            return ModJson.Deserialize<PlayerStatisticsDocument>(json);
        }

        public static string SerializeLeaderboard(LeaderboardDocument doc)
        {
            return ModJson.Serialize(doc);
        }

        public static LeaderboardDocument? DeserializeLeaderboard(string json)
        {
            return ModJson.Deserialize<LeaderboardDocument>(json);
        }
    }
}
