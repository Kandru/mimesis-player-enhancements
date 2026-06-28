using System.Collections.Generic;
using System.Globalization;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardJson
    {
        public static string SerializeStatus(WebDashboardStatusDto status)
        {
            return ModJson.Serialize(status);
        }

        public static string SerializePlayers(IReadOnlyList<WebDashboardPlayerDto> players)
        {
            List<PlayerApiDto> mapped = [];
            foreach (WebDashboardPlayerDto player in players)
            {
                mapped.Add(MapPlayer(player));
            }

            return ModJson.Serialize(new PlayersApiResponse { Players = mapped });
        }

        public static string SerializeLeaderboardResponse(LeaderboardDocument doc, IReadOnlyCollection<ulong> connectedSteamIds)
        {
            List<LeaderboardEntryApiDto> entries = [];
            foreach (LeaderboardEntry entry in doc.Entries)
            {
                entries.Add(MapLeaderboardEntry(entry));
            }

            List<string> connected = [];
            foreach (ulong steamId in connectedSteamIds)
            {
                connected.Add(steamId.ToString());
            }

            return ModJson.Serialize(new LeaderboardApiResponse
            {
                SaveSlotId = doc.SaveSlotId,
                UpdatedAtUtc = doc.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                ConnectedSteamIds = connected,
                Entries = entries,
            });
        }

        public static string SerializePlayerStats(PlayerStatisticsDocument doc)
        {
            return ModJson.Serialize(MapPlayerStats(doc));
        }

        public static string SerializeActionResult(WebDashboardActionResult result)
        {
            return ModJson.Serialize(result);
        }

        public static string SerializeError(int statusCode, string message)
        {
            return ModJson.Serialize(new ErrorApiResponse
            {
                Error = statusCode,
                Message = message,
            });
        }

        public static string SerializeSettings(WebDashboardSettingsDto settings)
        {
            return ModJson.Serialize(settings);
        }

        private static PlayerApiDto MapPlayer(WebDashboardPlayerDto player)
        {
            return new PlayerApiDto
            {
                SteamId = player.SteamId.ToString(),
                PlayerUid = player.PlayerUid,
                DisplayName = player.DisplayName,
                IsHost = player.IsHost,
                IsLocal = player.IsLocal,
                IsBanned = player.IsBanned,
                NetworkGrade = player.NetworkGrade,
                ConnectionRole = player.ConnectionRole,
                ConnectionAddress = player.ConnectionAddress,
                VoiceEventCount = player.VoiceEventCount,
                CurrentSession = player.CurrentSession == null ? null : MapSessionStats(player.CurrentSession),
            };
        }

        private static SessionStatsApiDto MapSessionStats(WebDashboardSessionStatsDto stats)
        {
            return new SessionStatsApiDto
            {
                CurrencyEarned = stats.CurrencyEarned,
                Kills = stats.Kills,
                Deaths = stats.Deaths,
                Revives = stats.Revives,
                MimicEncounterCount = stats.MimicEncounterCount,
                ItemCarryCount = stats.ItemCarryCount,
                VoiceEvents = stats.VoiceEvents,
                DamageToAlly = stats.DamageToAlly,
                TotalConnectedSeconds = stats.TotalConnectedSeconds,
            };
        }

        private static LeaderboardEntryApiDto MapLeaderboardEntry(LeaderboardEntry entry)
        {
            return new LeaderboardEntryApiDto
            {
                SteamId = entry.SteamId.ToString(),
                DisplayName = entry.DisplayName,
                ItemCarryCount = entry.ItemCarryCount,
                DamageToAlly = entry.DamageToAlly,
                MimicEncounterCount = entry.MimicEncounterCount,
                TimeInStartingVolumeMs = entry.TimeInStartingVolumeMs,
                CurrencyEarned = entry.CurrencyEarned,
                VoiceEvents = entry.VoiceEvents,
                Deaths = entry.Deaths,
                Revives = entry.Revives,
                Kills = entry.Kills,
                TotalConnectedSeconds = entry.TotalConnectedSeconds,
                SessionsCompleted = entry.SessionsCompleted,
            };
        }

        private static PlayerStatsApiDto MapPlayerStats(PlayerStatisticsDocument doc)
        {
            return new PlayerStatsApiDto
            {
                Version = doc.Version,
                SteamId = doc.SteamId.ToString(),
                DisplayName = doc.DisplayName,
                Global = doc.Global,
                CurrentSession = doc.CurrentSession,
                RecentSessions = doc.RecentSessions,
            };
        }

        private sealed class PlayersApiResponse
        {
            public List<PlayerApiDto> Players = [];
        }

        private sealed class PlayerApiDto
        {
            public string SteamId = "";
            public long PlayerUid;
            public string DisplayName = "";
            public bool IsHost;
            public bool IsLocal;
            public bool IsBanned;
            public int NetworkGrade = -1;
            public string ConnectionRole = "";
            public string ConnectionAddress = "";
            public int VoiceEventCount;
            public SessionStatsApiDto? CurrentSession;
        }

        private sealed class SessionStatsApiDto
        {
            public long CurrencyEarned;
            public long Kills;
            public long Deaths;
            public long Revives;
            public long MimicEncounterCount;
            public long ItemCarryCount;
            public long VoiceEvents;
            public long DamageToAlly;
            public long TotalConnectedSeconds;
        }

        private sealed class LeaderboardApiResponse
        {
            public int SaveSlotId;
            public string UpdatedAtUtc = "";
            public List<string> ConnectedSteamIds = [];
            public List<LeaderboardEntryApiDto> Entries = [];
        }

        private sealed class LeaderboardEntryApiDto
        {
            public string SteamId = "";
            public string DisplayName = "";
            public long ItemCarryCount;
            public long DamageToAlly;
            public long MimicEncounterCount;
            public long TimeInStartingVolumeMs;
            public long CurrencyEarned;
            public long VoiceEvents;
            public long Deaths;
            public long Revives;
            public long Kills;
            public long TotalConnectedSeconds;
            public int SessionsCompleted;
        }

        private sealed class PlayerStatsApiDto
        {
            public int Version;
            public string SteamId = "";
            public string DisplayName = "";
            public GlobalStats Global = new();
            public SessionStats? CurrentSession;
            public List<SessionStats> RecentSessions = [];
        }

        private sealed class ErrorApiResponse
        {
            public int Error;
            public string Message = "";
        }
    }
}
