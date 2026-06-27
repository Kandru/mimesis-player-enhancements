using System.Collections.Generic;
using System.Linq;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Statistics;

public static class LeaderboardBuilder
{
    public static LeaderboardDocument Build(int slotId, IEnumerable<PlayerStatisticsDocument> players)
    {
        var leaderboard = new LeaderboardDocument
        {
            SaveSlotId = slotId,
            UpdatedAtUtc = System.DateTime.UtcNow,
        };

        foreach (var player in players)
        {
            if (player.SteamId == 0) continue;
            var c = player.Global.Counters;
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
                Deaths = c.Deaths,
                Revives = c.Revives,
                Kills = c.Kills,
                TotalConnectedSeconds = c.TotalConnectedSeconds,
                SessionsCompleted = player.Global.SessionsCompleted,
            });
        }

        leaderboard.Entries = leaderboard.Entries
            .OrderByDescending(e => e.CurrencyEarned)
            .ThenByDescending(e => e.MimicEncounterCount)
            .ThenByDescending(e => e.ItemCarryCount)
            .ToList();

        return leaderboard;
    }
}
