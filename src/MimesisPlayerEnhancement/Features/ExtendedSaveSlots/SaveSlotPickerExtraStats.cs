using System.IO;
using MimesisPlayerEnhancement.Config.QuickSettings;
using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class SaveSlotPickerExtraStats
    {
        private static readonly Dictionary<int, string> Line3Cache = new();

        internal static void ClearCache() => Line3Cache.Clear();

        internal static void InvalidateSlot(int slotId) => Line3Cache.Remove(slotId);

        internal static void PopulateLine3Text(IReadOnlyList<SaveSlotEntry> entries)
        {
            foreach (SaveSlotEntry entry in entries)
            {
                entry.Line3Text = FormatLine3(entry.SlotId);
            }
        }

        internal static string FormatLine3(int slotId)
        {
            if (Line3Cache.TryGetValue(slotId, out string? cached))
            {
                return cached;
            }

            string text = BuildLine3(slotId);
            Line3Cache[slotId] = text;
            return text;
        }

        internal static float ComputeRowHeight()
        {
            const float verticalPadding = 10f;
            const float line1Height = 24f;
            const float line2Height = 22f;
            const float line3Height = 18f;

            return verticalPadding + line1Height + line2Height + line3Height + verticalPadding;
        }

        private static string BuildLine3(int slotId)
        {
            List<string> parts = [];

            SaveConfigProfileState profile = TryReadProfileIfPresent(slotId);
            parts.Add(SaveSlotConfigProfile.GetDisplayLabel(profile));

            LeaderboardDocument? leaderboard = LoadLeaderboard(slotId);
            if (leaderboard?.Entries is { Count: > 0 } entries)
            {
                AppendLeaderboardSummary(parts, entries);
            }

            if (SpeechEventFileStore.HasSpeechEventsFile(slotId))
            {
                int voiceEvents = SpeechEventFileStore.TryGetSavedSpeechEventCount(slotId);
                if (voiceEvents > 0)
                {
                    parts.Add(ModL10n.Get("saveslots.voice_events", new Dictionary<string, object> { ["count"] = voiceEvents }));
                }
            }

            return parts.Count == 0
                ? ModL10n.Get("saveslots.no_statistics")
                : string.Join(" · ", parts);
        }

        private static SaveConfigProfileState TryReadProfileIfPresent(int slotId)
        {
            string? path = SaveSidecarPaths.GetSlotDocumentPath(slotId);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return new SaveConfigProfileState();
            }

            return SaveSlotConfigProfile.TryReadProfileForSlot(slotId);
        }

        private static void AppendLeaderboardSummary(List<string> parts, List<LeaderboardEntry> entries)
        {
            parts.Add(entries.Count == 1
                ? ModL10n.Get("saveslots.players", new Dictionary<string, object> { ["count"] = entries.Count })
                : ModL10n.Get("saveslots.players_plural", new Dictionary<string, object> { ["count"] = entries.Count }));

            long sessions = 0;
            long survivalWins = 0;
            long survivalDeaths = 0;
            long revives = 0;
            long playSeconds = 0;

            foreach (LeaderboardEntry entry in entries)
            {
                sessions += entry.SessionsCompleted;
                survivalWins += entry.SurvivalWins;
                survivalDeaths += entry.SurvivalDeaths;
                revives += entry.Revives;
                playSeconds += entry.TotalConnectedSeconds;
            }

            if (sessions > 0)
            {
                parts.Add(sessions == 1
                    ? ModL10n.Get("saveslots.sessions", new Dictionary<string, object> { ["count"] = sessions })
                    : ModL10n.Get("saveslots.sessions_plural", new Dictionary<string, object> { ["count"] = sessions }));
            }

            if (survivalWins > 0)
            {
                parts.Add(ModL10n.Get("saveslots.survival_wins", new Dictionary<string, object> { ["count"] = survivalWins }));
            }

            if (survivalDeaths > 0)
            {
                parts.Add(ModL10n.Get("saveslots.survival_deaths", new Dictionary<string, object> { ["count"] = survivalDeaths }));
            }

            if (revives > 0)
            {
                parts.Add(ModL10n.Get("saveslots.revives", new Dictionary<string, object> { ["count"] = revives }));
            }

            if (playSeconds >= 60)
            {
                parts.Add(ModL10n.Get("saveslots.playtime", new Dictionary<string, object> { ["time"] = FormatPlaytime(playSeconds) }));
            }
        }

        private static LeaderboardDocument? LoadLeaderboard(int slotId)
        {
            string? path = SaveSidecarPaths.GetStatisticsPath(slotId);
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            Dictionary<ulong, PlayerStatisticsDocument> players = [];
            StatisticsStore.LoadAllPlayersForSlot(slotId, players);
            return players.Count == 0 ? null : LeaderboardBuilder.Build(slotId, players.Values);
        }

        private static string FormatPlaytime(long totalSeconds)
        {
            if (totalSeconds < 60)
            {
                return ModL10n.Get("stats.playtime_seconds", new Dictionary<string, object> { ["seconds"] = totalSeconds });
            }

            long hours = totalSeconds / 3600;
            long minutes = (totalSeconds % 3600) / 60;
            return hours > 0
                ? minutes > 0
                    ? ModL10n.Get("stats.playtime_hours_minutes", new Dictionary<string, object> { ["hours"] = hours, ["minutes"] = minutes })
                    : ModL10n.Get("stats.playtime_hours", new Dictionary<string, object> { ["hours"] = hours })
                : ModL10n.Get("stats.playtime_minutes", new Dictionary<string, object> { ["minutes"] = minutes });
        }
    }
}
