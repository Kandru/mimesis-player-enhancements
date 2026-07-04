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
            string separator = ModL10n.Get("stats.list_separator");

            if (stats.SurvivalDeaths > 0)
            {
                parts.Add(Count("announce.deaths", "announce.deaths_plural", stats.SurvivalDeaths));
            }

            if (stats.SurvivalWins > 0)
            {
                parts.Add(Count("announce.wins", "announce.wins_plural", stats.SurvivalWins));
            }

            if (stats.SurvivalLeftBehind > 0)
            {
                parts.Add(ModL10n.Get("announce.left_behind", new Dictionary<string, object> { ["count"] = stats.SurvivalLeftBehind }));
            }

            if (stats.Revives > 0)
            {
                parts.Add(Count("announce.revives", "announce.revives_plural", stats.Revives));
            }

            foreach (KeyValuePair<string, long> kvp in stats.MonsterKills.OrderByDescending(p => p.Value))
            {
                if (kvp.Value <= 0)
                {
                    continue;
                }

                parts.Add(kvp.Value == 1
                    ? ModL10n.Get("announce.monster_kills", new Dictionary<string, object> { ["count"] = kvp.Value, ["name"] = kvp.Key })
                    : ModL10n.Get("announce.monster_kills_plural", new Dictionary<string, object> { ["count"] = kvp.Value, ["name"] = kvp.Key }));
            }

            if (stats.FriendsKilled > 0)
            {
                parts.Add(Count("announce.friends_killed", "announce.friends_killed_plural", stats.FriendsKilled));
            }

            if (stats.ItemCarryCount > 0)
            {
                parts.Add(Count("announce.items_carried", "announce.items_carried_plural", stats.ItemCarryCount));
            }

            if (stats.MimicEncounterCount > 0)
            {
                parts.Add(Count("announce.mimic_encounters", "announce.mimic_encounters_plural", stats.MimicEncounterCount));
            }

            if (stats.DamageToFriend > 0)
            {
                parts.Add(ModL10n.Get("announce.friend_damage", new Dictionary<string, object> { ["count"] = stats.DamageToFriend }));
            }

            return parts.Count == 0
                ? ModL10n.Get("announce.map_run_empty")
                : ModL10n.Get("announce.map_run_prefix", new Dictionary<string, object> { ["summary"] = string.Join(separator, parts) });
        }

        private static string Count(string singularKey, string pluralKey, long count)
        {
            return count == 1
                ? ModL10n.Get(singularKey, new Dictionary<string, object> { ["count"] = count })
                : ModL10n.Get(pluralKey, new Dictionary<string, object> { ["count"] = count });
        }
    }
}
