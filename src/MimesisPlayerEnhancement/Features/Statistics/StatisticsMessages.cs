using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Util;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsMessages
    {
        private const string Feature = "Statistics";
        private const float LocalIntroDelaySeconds = 1f;
        private const float GlobalStatsJoinDelaySeconds = 3f;
        private const float GlobalStatsDedupSeconds = 3f;

        private const BindingFlags InstanceMemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

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
            PlayerStatisticsDocument doc,
            bool isNewSession,
            int reconnectCount)
        {
            if (!ShouldShow())
            {
                return;
            }

            ScheduleGlobalStatsOnJoin(steamId, displayName, doc);

            if (LocalPlayerHelper.IsLocalSteamId(steamId))
            {
                ScheduleLocalIntro(isNewSession, reconnectCount);
            }
        }

        internal static void OnPlayerLeftSession(ulong steamId, string displayName, PlayerStatisticsDocument doc)
        {
            if (!ShouldShow())
            {
                return;
            }

            TryShowGlobalStats(steamId, displayName, doc, isJoin: false);
        }

        internal static void OnDungeonCompleted(int cycleNumber)
        {
            if (!ShouldShow())
            {
                return;
            }

            InGameMessageHelper.ShowModMessage($"Dungeon run recorded (cycle {cycleNumber}).");
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

            ulong steamId = ResolveSteamIdFromDisplayName(userName);
            if (steamId == 0)
            {
                return;
            }

            if (!MimesisSaveManager.TryGetActiveSaveSlotId(out _))
            {
                return;
            }

            if (StatisticsTracker.TryGetPlayerDocument(steamId) is not PlayerStatisticsDocument doc)
            {
                return;
            }

            doc.DisplayName = userName;
            if (isEntering)
            {
                ScheduleGlobalStatsOnJoin(steamId, userName, doc);
            }
            else
            {
                TryShowGlobalStats(steamId, userName, doc, isJoin: false);
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
            PlayerStatisticsDocument doc)
        {
            if (steamId == 0 || doc.Global == null)
            {
                return;
            }

            if (!HasAnyGlobalStats(doc.Global))
            {
                return;
            }

            if (!PendingJoinStats.Add(steamId))
            {
                return;
            }

            _ = MelonCoroutines.Start(ShowGlobalStatsOnJoinAfterDelay(steamId, displayName, doc));
        }

        private static IEnumerator ShowGlobalStatsOnJoinAfterDelay(
            ulong steamId,
            string displayName,
            PlayerStatisticsDocument doc)
        {
            yield return new WaitForSeconds(GlobalStatsJoinDelaySeconds);

            _ = PendingJoinStats.Remove(steamId);
            TryShowGlobalStats(steamId, displayName, doc, isJoin: true);
        }

        private static string FormatLocalSessionIntro(bool? isNewSession, int reconnectCount)
        {
            List<string> lines =
            [
                $"v{VersionInfo.ModuleVersion}, developed by {AuthorName}, downloadable via {DownloadUrl}",
            ];

            if (isNewSession == true)
            {
                lines.Add("A new stats session has been started.");
            }
            else if (isNewSession == false)
            {
                lines.Add(reconnectCount > 0
                    ? $"Stats session resumed (reconnect {reconnectCount})."
                    : "Stats session resumed.");
            }

            return string.Join("\n", lines);
        }

        private static void TryShowGlobalStats(
            ulong steamId,
            string displayName,
            PlayerStatisticsDocument doc,
            bool isJoin)
        {
            if (steamId == 0 || doc.Global == null)
            {
                return;
            }

            if (!HasAnyGlobalStats(doc.Global))
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
            string name = string.IsNullOrWhiteSpace(displayName) ? doc.DisplayName : displayName;
            InGameMessageHelper.ShowModMessage(FormatGlobalStats(name, doc.Global));
        }

        internal static bool HasAnyGlobalStats(GlobalStats global)
        {
            if (global.SessionsCompleted > 0)
            {
                return true;
            }

            StatCounters c = global.Counters;
            return c.CyclesCompleted > 0
                   || c.SurvivalDeaths > 0
                   || c.SurvivalWins > 0
                   || c.SurvivalLeftBehind > 0
                   || c.DeathmatchDeaths > 0
                   || c.DeathmatchWins > 0
                   || c.Revives > 0
                   || c.VoiceEvents > 0
                   || c.CurrencyEarned > 0
                   || c.ItemCarryCount > 0
                   || c.DamageToAlly > 0
                   || c.MimicEncounterCount > 0
                   || c.TimeInStartingVolumeMs > 0
                   || c.TotalConnectedSeconds > 0
                   || HasDictionaryCounts(c.MonsterKillsByMasterId)
                   || HasDictionaryCounts(c.DeathsByTrapType);
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

        internal static string FormatGlobalStats(string displayName, GlobalStats global)
        {
            StatCounters c = global.Counters;
            List<string> parts = [];

            if (global.SessionsCompleted > 0)
            {
                parts.Add($"{global.SessionsCompleted} sessions");
            }

            if (c.CyclesCompleted > 0)
            {
                parts.Add($"{c.CyclesCompleted} cycles");
            }

            if (c.SurvivalWins > 0)
            {
                parts.Add($"{c.SurvivalWins} survival wins");
            }

            if (c.SurvivalLeftBehind > 0)
            {
                parts.Add($"{c.SurvivalLeftBehind} left behind");
            }

            if (c.SurvivalDeaths > 0)
            {
                parts.Add($"{c.SurvivalDeaths} survival deaths");
            }

            if (c.DeathmatchWins > 0)
            {
                parts.Add($"{c.DeathmatchWins} deathmatch wins");
            }

            if (c.DeathmatchDeaths > 0)
            {
                parts.Add($"{c.DeathmatchDeaths} deathmatch deaths");
            }

            if (c.Revives > 0)
            {
                parts.Add($"{c.Revives} revives");
            }

            if (c.VoiceEvents > 0)
            {
                parts.Add($"{c.VoiceEvents} voices recorded");
            }

            if (c.CurrencyEarned > 0)
            {
                parts.Add($"{c.CurrencyEarned} currency");
            }

            if (c.TotalConnectedSeconds > 0)
            {
                parts.Add(FormatPlaytime(c.TotalConnectedSeconds));
            }

            string summary = parts.Count > 0 ? string.Join(", ", parts) : "no recorded stats yet";
            return $"{displayName} — {summary}";
        }

        private static string FormatPlaytime(long totalSeconds)
        {
            if (totalSeconds < 60)
            {
                return $"{totalSeconds}s played";
            }

            long hours = totalSeconds / 3600;
            long minutes = (totalSeconds % 3600) / 60;
            return hours > 0 ? minutes > 0 ? $"{hours}h {minutes}m played" : $"{hours}h played" : $"{minutes}m played";
        }

        private static ulong ResolveSteamIdFromDisplayName(string displayName)
        {
            try
            {
                if (Hub.s == null)
                {
                    return 0;
                }

                object? pdata = typeof(Hub).GetField("pdata", InstanceMemberFlags)?.GetValue(Hub.s);
                object? main = pdata?.GetType().GetField("main", InstanceMemberFlags)?.GetValue(pdata);
                if (main == null)
                {
                    return 0;
                }

                FieldInfo cacheField = main.GetType().GetField("steamIDToNameCache", InstanceMemberFlags);
                if (cacheField?.GetValue(main) is not Dictionary<ulong, string> cache)
                {
                    return 0;
                }

                foreach (KeyValuePair<ulong, string> kvp in cache)
                {
                    if (string.Equals(kvp.Value, displayName, StringComparison.OrdinalIgnoreCase))
                    {
                        return kvp.Key;
                    }
                }
            }
            catch
            {
                /* ignore */
            }

            return 0;
        }
    }
}
