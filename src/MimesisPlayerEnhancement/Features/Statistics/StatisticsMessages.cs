using System.Collections;
using MelonLoader;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsMessages
    {
        private const string Feature = "Statistics";
        private const float LocalIntroDelaySeconds = 1f;
        private const float GlobalStatsJoinDelaySeconds = 3f;
        private const float GlobalStatsDedupSeconds = 3f;

        internal const string PluginDisplayName = "Mimesis Player Enhancement";
        internal const string AuthorName = "Kandru";
        internal const string DownloadUrl = "github.com/Kandru/mimesis-player-enhancements";

        private static bool _localIntroScheduled;
        private static readonly HashSet<ulong> PendingJoinStats = [];
        private static readonly Dictionary<(ulong SteamId, bool IsJoin), DateTime> GlobalStatsShownAt = [];

        internal static void ClearRuntimeState()
        {
            _localIntroScheduled = false;
            PendingJoinStats.Clear();
            GlobalStatsShownAt.Clear();
        }

        internal static void ClearPlayerRuntimeState(ulong steamId)
        {
            if (steamId == 0)
            {
                return;
            }

            _ = PendingJoinStats.Remove(steamId);

            List<(ulong SteamId, bool IsJoin)> keysToRemove = [];
            foreach ((ulong id, bool isJoin) in GlobalStatsShownAt.Keys)
            {
                if (id == steamId)
                {
                    keysToRemove.Add((id, isJoin));
                }
            }

            foreach ((ulong id, bool isJoin) in keysToRemove)
            {
                _ = GlobalStatsShownAt.Remove((id, isJoin));
            }
        }

        internal static void OnLocalPlayerArchiveStarted()
        {
            if (!ShouldShow())
            {
                return;
            }

            if (MimesisSaveManager.IsHost())
            {
                return;
            }

            ScheduleLocalIntro(isNewSession: null, reconnectCount: 0);
        }

        internal static void OnPlayerJoinedSession(
            ulong steamId,
            string displayName,
            PlayerGlobalStats global,
            bool isNewSession,
            int reconnectCount)
        {
            if (!ShouldShow())
            {
                return;
            }

            ScheduleGlobalStatsOnJoin(steamId, displayName, global);

            if (LocalPlayerHelper.IsLocalSteamId(steamId))
            {
                ScheduleLocalIntro(isNewSession, reconnectCount);
            }
        }

        internal static void OnPlayerLeftSession(ulong steamId, string displayName, PlayerGlobalStats global)
        {
            if (!ShouldShow())
            {
                return;
            }

            TryShowGlobalStats(steamId, displayName, global, isJoin: false);
        }

        internal static void OnDungeonCompleted(int cycleNumber)
        {
            if (!ShouldShow())
            {
                return;
            }

            InGameMessageHelper.ShowModMessage(ModL10n.Get("stats.dungeon_completed", new Dictionary<string, object> { ["cycle"] = cycleNumber }));
        }

        internal static void OnGamePlayerInfoShown(string userName, bool isEntering)
        {
            if (!ShouldShow())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                return;
            }

            ulong steamId = StatisticsDisplayNameResolver.TryResolveSteamId(userName, out ulong resolved)
                ? resolved
                : 0;
            if (steamId == 0)
            {
                return;
            }

            if (!MimesisSaveManager.TryGetActiveSaveSlotId(out _))
            {
                return;
            }

            if (!PlayerRegistry.TryGetGlobal(steamId, out PlayerGlobalStats? global))
            {
                return;
            }

            if (!PlayerRegistry.UpdateDisplayName(steamId, userName))
            {
                global.DisplayName = userName;
            }

            if (isEntering)
            {
                ScheduleGlobalStatsOnJoin(steamId, userName, global);
            }
            else
            {
                TryShowGlobalStats(steamId, userName, global, isJoin: false);
            }
        }

        private static bool ShouldShow()
        {
            return ModConfig.EnableStatistics.Value && ModConfig.ShowStatisticsToasts.Value;
        }

        private static void ScheduleLocalIntro(bool? isNewSession, int reconnectCount)
        {
            if (_localIntroScheduled)
            {
                return;
            }

            _localIntroScheduled = true;
            _ = MelonCoroutines.Start(ShowLocalIntroAfterDelay(isNewSession, reconnectCount));
        }

        private static IEnumerator ShowLocalIntroAfterDelay(bool? isNewSession, int reconnectCount)
        {
            yield return new WaitForSeconds(LocalIntroDelaySeconds);

            _localIntroScheduled = false;

            InGameMessageHelper.ShowModMessage(
                FormatLocalSessionIntro(isNewSession, reconnectCount),
                localOnly: true);
            ModLog.Debug(Feature, "Local session intro shown.");
        }

        private static void ScheduleGlobalStatsOnJoin(
            ulong steamId,
            string displayName,
            PlayerGlobalStats global)
        {
            if (steamId == 0 || global == null)
            {
                return;
            }

            if (!HasAnyGlobalStats(global))
            {
                return;
            }

            if (!PendingJoinStats.Add(steamId))
            {
                return;
            }

            _ = MelonCoroutines.Start(ShowGlobalStatsOnJoinAfterDelay(steamId, displayName, global));
        }

        private static IEnumerator ShowGlobalStatsOnJoinAfterDelay(
            ulong steamId,
            string displayName,
            PlayerGlobalStats global)
        {
            yield return new WaitForSeconds(GlobalStatsJoinDelaySeconds);

            _ = PendingJoinStats.Remove(steamId);
            TryShowGlobalStats(steamId, displayName, global, isJoin: true);
        }

        private static string FormatLocalSessionIntro(bool? isNewSession, int reconnectCount)
        {
            List<string> lines =
            [
                ModL10n.Get("stats.intro_version", new Dictionary<string, object>
                {
                    ["version"] = VersionInfo.ModuleVersion,
                    ["author"] = AuthorName,
                    ["url"] = DownloadUrl,
                }),
            ];

            if (isNewSession == true)
            {
                lines.Add(ModL10n.Get("stats.session_started"));
            }
            else if (isNewSession == false)
            {
                lines.Add(reconnectCount > 0
                    ? ModL10n.Get("stats.session_resumed_reconnect", new Dictionary<string, object> { ["count"] = reconnectCount })
                    : ModL10n.Get("stats.session_resumed"));
            }

            return string.Join("\n", lines);
        }

        private static void TryShowGlobalStats(
            ulong steamId,
            string displayName,
            PlayerGlobalStats global,
            bool isJoin)
        {
            if (steamId == 0 || global == null)
            {
                return;
            }

            if (!HasAnyGlobalStats(global))
            {
                return;
            }

            (ulong steamId, bool isJoin) key = (steamId, isJoin);
            if (GlobalStatsShownAt.TryGetValue(key, out DateTime shownAt)
                && DateTime.UtcNow - shownAt < TimeSpan.FromSeconds(GlobalStatsDedupSeconds))
            {
                return;
            }

            GlobalStatsShownAt[key] = DateTime.UtcNow;
            string name = string.IsNullOrWhiteSpace(displayName) ? global.DisplayName : displayName;
            InGameMessageHelper.ShowModMessage(FormatGlobalStats(name, global));
        }

        internal static bool HasAnyGlobalStats(PlayerGlobalStats global)
        {
            if (global.SessionsCompleted > 0 || global.DungeonRunsPlayed > 0 || global.VoiceEvents > 0)
            {
                return true;
            }

            StatCounters c = global.Counters;
            return c.Deaths > 0
                   || c.SurvivalWins > 0
                   || c.SurvivalLeftBehind > 0
                   || c.DeathmatchDeaths > 0
                   || c.DeathmatchWins > 0
                   || c.Revives > 0
                   || c.TrainValueDeposited > 0
                   || c.ItemsDeposited > 0
                   || c.ItemsCarried > 0
                   || c.DamageToFriend > 0
                   || c.FriendsKilled > 0
                   || c.MimicEncounters > 0
                   || c.ConnectedSeconds > 0
                   || HasDictionaryCounts(c.MonsterKills)
                   || HasDictionaryCounts(c.DeathsByMonster)
                   || HasDictionaryCounts(c.DeathsByTrap);
        }

        private static bool HasDictionaryCounts(Dictionary<string, long>? counts)
        {
            if (counts == null)
            {
                return false;
            }

            foreach (long value in counts.Values)
            {
                if (value > 0)
                {
                    return true;
                }
            }

            return false;
        }

        internal static string FormatGlobalStats(string displayName, PlayerGlobalStats global)
        {
            StatCounters c = global.Counters;
            List<string> parts = [];
            string separator = ModL10n.Get("stats.list_separator");

            if (global.SessionsCompleted > 0)
            {
                parts.Add(ModL10n.Get("stats.sessions", new Dictionary<string, object> { ["count"] = global.SessionsCompleted }));
            }

            if (global.DungeonRunsPlayed > 0)
            {
                parts.Add(ModL10n.Get("stats.cycles", new Dictionary<string, object> { ["count"] = global.DungeonRunsPlayed }));
            }

            if (c.SurvivalWins > 0)
            {
                parts.Add(ModL10n.Get("stats.survival_wins", new Dictionary<string, object> { ["count"] = c.SurvivalWins }));
            }

            if (c.SurvivalLeftBehind > 0)
            {
                parts.Add(ModL10n.Get("stats.left_behind", new Dictionary<string, object> { ["count"] = c.SurvivalLeftBehind }));
            }

            if (c.Deaths > 0)
            {
                parts.Add(ModL10n.Get("stats.survival_deaths", new Dictionary<string, object> { ["count"] = c.Deaths }));
            }

            if (c.DeathmatchWins > 0)
            {
                parts.Add(ModL10n.Get("stats.deathmatch_wins", new Dictionary<string, object> { ["count"] = c.DeathmatchWins }));
            }

            if (c.DeathmatchDeaths > 0)
            {
                parts.Add(ModL10n.Get("stats.deathmatch_deaths", new Dictionary<string, object> { ["count"] = c.DeathmatchDeaths }));
            }

            if (c.Revives > 0)
            {
                parts.Add(ModL10n.Get("stats.revives", new Dictionary<string, object> { ["count"] = c.Revives }));
            }

            if (global.VoiceEvents > 0)
            {
                parts.Add(ModL10n.Get("stats.voices_recorded", new Dictionary<string, object> { ["count"] = global.VoiceEvents }));
            }

            if (c.TrainValueDeposited > 0)
            {
                parts.Add(ModL10n.Get("stats.currency", new Dictionary<string, object> { ["count"] = c.TrainValueDeposited }));
            }

            if (c.ConnectedSeconds > 0)
            {
                parts.Add(FormatPlaytime(c.ConnectedSeconds));
            }

            string summary = parts.Count > 0 ? string.Join(separator, parts) : ModL10n.Get("stats.no_stats");
            return displayName + ModL10n.Get("stats.summary_separator") + summary;
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
