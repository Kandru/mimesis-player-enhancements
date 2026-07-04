using System.Linq;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    public static class LeaderboardBuilder
    {
        public static LeaderboardDocument Build(int slotId, IEnumerable<PlayerStatisticsDocument> players)
        {
            LeaderboardDocument leaderboard = new()
            {
                SaveSlotId = slotId,
                UpdatedAtUtc = System.DateTime.UtcNow,
            };

            foreach (PlayerStatisticsDocument player in players)
            {
                if (player.SteamId == 0)
                {
                    continue;
                }

                StatCounters c = player.Global.Counters;
                leaderboard.Entries.Add(new LeaderboardEntry
                {
                    SteamId = player.SteamId,
                    DisplayName = player.DisplayName,
                    ItemCarryCount = c.ItemCarryCount,
                    DamageToAlly = c.DamageToAlly,
                    MimicEncounterCount = c.MimicEncounterCount,
                    TimeInStartingVolumeMs = c.TimeInStartingVolumeMs,
                    CurrencyEarned = c.CurrencyEarned,
                    VoiceEvents = c.VoiceEvents,
                    SurvivalDeaths = c.SurvivalDeaths,
                    SurvivalWins = c.SurvivalWins,
                    SurvivalLeftBehind = c.SurvivalLeftBehind,
                    DeathmatchDeaths = c.DeathmatchDeaths,
                    DeathmatchWins = c.DeathmatchWins,
                    Revives = c.Revives,
                    TotalConnectedSeconds = c.TotalConnectedSeconds,
                    SessionsCompleted = player.Global.SessionsCompleted,
                });
            }

            leaderboard.Entries = [.. leaderboard.Entries
                .OrderByDescending(e => e.CurrencyEarned)
                .ThenByDescending(e => e.MimicEncounterCount)
                .ThenByDescending(e => e.ItemCarryCount)];

            return leaderboard;
        }
    }
}
