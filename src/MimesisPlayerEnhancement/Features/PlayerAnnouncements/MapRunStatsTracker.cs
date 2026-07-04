using System.Linq;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using Mimic.Actors;

namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements
{
    internal static class MapRunStatsTracker
    {
        private sealed class MapRunSnapshot
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

        private static readonly Dictionary<ulong, MapRunSnapshot> Baselines = [];

        internal static void ResetForDungeonEntry()
        {
            Baselines.Clear();

            if (!ModConfig.EnableStatistics.Value)
            {
                return;
            }

            ulong localSteamId = LocalPlayerHelper.TryGetLocalSteamId();
            if (localSteamId != 0)
            {
                Baselines[localSteamId] = CaptureCurrent(localSteamId);
            }

            foreach (ulong steamId in StatisticsTracker.GetConnectedSteamIds())
            {
                if (Baselines.ContainsKey(steamId))
                {
                    continue;
                }

                Baselines[steamId] = CaptureCurrent(steamId);
            }
        }

        internal static void OnLocalPlayerDeath(ProtoActor actor)
        {
            if (!ShouldShowDeathStats())
            {
                return;
            }

            ulong steamId = StatisticsTracker.TryResolveSteamId(actor);
            if (steamId == 0 || !LocalPlayerHelper.IsLocalSteamId(steamId))
            {
                return;
            }

            MapRunSnapshot baseline = Baselines.TryGetValue(steamId, out MapRunSnapshot? existing)
                ? existing
                : new MapRunSnapshot();
            MapRunSnapshot current = CaptureCurrent(steamId);
            string message = FormatMapRunStats(Subtract(current, baseline));
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            PlayerAnnouncements.ShowToast(message, localOnly: true, isEntering: false);
        }

        private static bool ShouldShowDeathStats()
        {
            return ModConfig.ShowPlayerAnnouncements.Value
                   && ModConfig.EnableStatistics.Value;
        }

        private static MapRunSnapshot CaptureCurrent(ulong steamId)
        {
            MapRunSnapshot snapshot = new();

            if (StatisticsTracker.TryGetCurrentPlayReport(steamId, out PlayReportData report))
            {
                snapshot.ItemCarryCount = report.TotalItemCarryCount;
                snapshot.DamageToFriend = report.TotalDamageToAlly;
                snapshot.MimicEncounterCount = report.TotalMimicEncounterCount;
                snapshot.TimeInStartingVolumeMs = report.TotalTimeInStartingVolume;
            }

            if (StatisticsTracker.TryGetSessionCounters(steamId, out StatCounters counters))
            {
                snapshot.SurvivalDeaths = counters.SurvivalDeaths;
                snapshot.SurvivalWins = counters.SurvivalWins;
                snapshot.SurvivalLeftBehind = counters.SurvivalLeftBehind;
                snapshot.Revives = counters.Revives;
                snapshot.FriendsKilled = counters.FriendsKilled;
                snapshot.MonsterKills = new Dictionary<string, long>(counters.MonsterKills ?? []);
            }

            return snapshot;
        }

        private static MapRunSnapshot Subtract(MapRunSnapshot current, MapRunSnapshot baseline)
        {
            return new()
            {
                ItemCarryCount = current.ItemCarryCount - baseline.ItemCarryCount,
                DamageToFriend = current.DamageToFriend - baseline.DamageToFriend,
                FriendsKilled = current.FriendsKilled - baseline.FriendsKilled,
                MimicEncounterCount = current.MimicEncounterCount - baseline.MimicEncounterCount,
                TimeInStartingVolumeMs = current.TimeInStartingVolumeMs - baseline.TimeInStartingVolumeMs,
                SurvivalDeaths = current.SurvivalDeaths - baseline.SurvivalDeaths,
                SurvivalWins = current.SurvivalWins - baseline.SurvivalWins,
                SurvivalLeftBehind = current.SurvivalLeftBehind - baseline.SurvivalLeftBehind,
                Revives = current.Revives - baseline.Revives,
                MonsterKills = SubtractDictionary(current.MonsterKills, baseline.MonsterKills),
            };
        }

        private static Dictionary<string, long> SubtractDictionary(
            Dictionary<string, long> current,
            Dictionary<string, long> baseline)
        {
            Dictionary<string, long> delta = [];
            foreach (KeyValuePair<string, long> kvp in current)
            {
                _ = baseline.TryGetValue(kvp.Key, out long baseValue);
                long diff = kvp.Value - baseValue;
                if (diff > 0)
                {
                    delta[kvp.Key] = diff;
                }
            }

            return delta;
        }

        private static string FormatMapRunStats(MapRunSnapshot stats)
        {
            List<string> parts = [];

            if (stats.SurvivalDeaths > 0)
            {
                parts.Add($"{stats.SurvivalDeaths} death{(stats.SurvivalDeaths == 1 ? "" : "s")}");
            }

            if (stats.SurvivalWins > 0)
            {
                parts.Add($"{stats.SurvivalWins} win{(stats.SurvivalWins == 1 ? "" : "s")}");
            }

            if (stats.SurvivalLeftBehind > 0)
            {
                parts.Add($"{stats.SurvivalLeftBehind} left behind");
            }

            if (stats.Revives > 0)
            {
                parts.Add($"{stats.Revives} revive{(stats.Revives == 1 ? "" : "s")}");
            }

            foreach (KeyValuePair<string, long> kvp in stats.MonsterKills.OrderByDescending(p => p.Value))
            {
                if (kvp.Value <= 0)
                {
                    continue;
                }

                parts.Add($"{kvp.Value} {kvp.Key} kill{(kvp.Value == 1 ? "" : "s")}");
            }

            if (stats.FriendsKilled > 0)
            {
                parts.Add($"{stats.FriendsKilled} friend{(stats.FriendsKilled == 1 ? "" : "s")} killed");
            }

            if (stats.ItemCarryCount > 0)
            {
                parts.Add($"{stats.ItemCarryCount} item{(stats.ItemCarryCount == 1 ? "" : "s")} carried");
            }

            if (stats.MimicEncounterCount > 0)
            {
                parts.Add($"{stats.MimicEncounterCount} mimic encounter{(stats.MimicEncounterCount == 1 ? "" : "s")}");
            }

            if (stats.DamageToFriend > 0)
            {
                parts.Add($"{stats.DamageToFriend} friend damage");
            }

            return parts.Count == 0 ? "Your run this map: no recorded activity yet." : $"Your run this map: {string.Join(", ", parts)}.";
        }
    }
}
