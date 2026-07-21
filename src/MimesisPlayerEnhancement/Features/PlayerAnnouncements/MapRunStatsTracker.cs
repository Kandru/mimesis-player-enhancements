using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements
{
    internal static class MapRunStatsTracker
    {
        private static readonly Dictionary<ulong, MapRunStatsSnapshot> Baselines = [];

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
        }

        internal static void ResetForSessionEnd()
        {
            Baselines.Clear();
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

            MapRunStatsSnapshot baseline = Baselines.TryGetValue(steamId, out MapRunStatsSnapshot? existing)
                ? existing
                : new MapRunStatsSnapshot();
            MapRunStatsSnapshot current = CaptureCurrent(steamId);
            string message = MapRunStatsFormatter.Format(MapRunStatsFormatter.Subtract(current, baseline));
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

        private static MapRunStatsSnapshot CaptureCurrent(ulong steamId)
        {
            MapRunStatsSnapshot snapshot = new();

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
    }
}
