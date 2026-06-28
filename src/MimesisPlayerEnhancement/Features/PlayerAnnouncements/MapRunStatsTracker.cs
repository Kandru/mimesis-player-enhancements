using System.Collections.Generic;
using Mimic.Actors;
using MimesisPlayerEnhancement.Features.Statistics;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements;

internal static class MapRunStatsTracker
{
    private sealed class MapRunSnapshot
    {
        public long ItemCarryCount;
        public long DamageToAlly;
        public long MimicEncounterCount;
        public long TimeInStartingVolumeMs;
        public long Kills;
        public long Deaths;
        public long Revives;
    }

    private static readonly Dictionary<ulong, MapRunSnapshot> Baselines = new();

    internal static void ResetForDungeonEntry()
    {
        Baselines.Clear();

        if (!ModConfig.EnableStatistics.Value)
            return;

        ulong localSteamId = LocalPlayerHelper.TryGetLocalSteamId();
        if (localSteamId != 0)
            Baselines[localSteamId] = CaptureCurrent(localSteamId);

        foreach (ulong steamId in StatisticsTracker.GetConnectedSteamIds())
        {
            if (Baselines.ContainsKey(steamId))
                continue;

            Baselines[steamId] = CaptureCurrent(steamId);
        }
    }

    internal static void OnLocalPlayerDeath(ProtoActor actor)
    {
        if (!ShouldShowDeathStats())
            return;

        ulong steamId = StatisticsTracker.TryResolveSteamId(actor);
        if (steamId == 0 || !LocalPlayerHelper.IsLocalSteamId(steamId))
            return;

        MapRunSnapshot baseline = Baselines.TryGetValue(steamId, out MapRunSnapshot? existing)
            ? existing
            : new MapRunSnapshot();
        MapRunSnapshot current = CaptureCurrent(steamId);
        string message = FormatMapRunStats(Subtract(current, baseline));
        if (string.IsNullOrWhiteSpace(message))
            return;

        PlayerAnnouncements.ShowToast(message, localOnly: true, isEntering: false);
    }

    private static bool ShouldShowDeathStats() =>
        ModConfig.ShowPlayerAnnouncements.Value
        && ModConfig.EnableStatistics.Value;

    private static MapRunSnapshot CaptureCurrent(ulong steamId)
    {
        var snapshot = new MapRunSnapshot();

        if (StatisticsTracker.TryGetCurrentPlayReport(steamId, out PlayReportData report))
        {
            snapshot.ItemCarryCount = report.TotalItemCarryCount;
            snapshot.DamageToAlly = report.TotalDamageToAlly;
            snapshot.MimicEncounterCount = report.TotalMimicEncounterCount;
            snapshot.TimeInStartingVolumeMs = report.TotalTimeInStartingVolume;
        }
        if (StatisticsTracker.TryGetSessionCounters(steamId, out StatCounters counters))
        {
            snapshot.Kills = counters.Kills;
            snapshot.Deaths = counters.Deaths;
            snapshot.Revives = counters.Revives;
        }

        return snapshot;
    }

    private static MapRunSnapshot Subtract(MapRunSnapshot current, MapRunSnapshot baseline) => new()
    {
        ItemCarryCount = current.ItemCarryCount - baseline.ItemCarryCount,
        DamageToAlly = current.DamageToAlly - baseline.DamageToAlly,
        MimicEncounterCount = current.MimicEncounterCount - baseline.MimicEncounterCount,
        TimeInStartingVolumeMs = current.TimeInStartingVolumeMs - baseline.TimeInStartingVolumeMs,
        Kills = current.Kills - baseline.Kills,
        Deaths = current.Deaths - baseline.Deaths,
        Revives = current.Revives - baseline.Revives,
    };

    private static string FormatMapRunStats(MapRunSnapshot stats)
    {
        var parts = new List<string>();

        if (stats.Kills > 0)
            parts.Add($"{stats.Kills} kill{(stats.Kills == 1 ? "" : "s")}");
        if (stats.Deaths > 0)
            parts.Add($"{stats.Deaths} death{(stats.Deaths == 1 ? "" : "s")}");
        if (stats.Revives > 0)
            parts.Add($"{stats.Revives} revive{(stats.Revives == 1 ? "" : "s")}");
        if (stats.ItemCarryCount > 0)
            parts.Add($"{stats.ItemCarryCount} item{(stats.ItemCarryCount == 1 ? "" : "s")} carried");
        if (stats.MimicEncounterCount > 0)
            parts.Add($"{stats.MimicEncounterCount} mimic encounter{(stats.MimicEncounterCount == 1 ? "" : "s")}");
        if (stats.DamageToAlly > 0)
            parts.Add($"{stats.DamageToAlly} ally damage");

        if (parts.Count == 0)
            return "Your run this map: no recorded activity yet.";

        return $"Your run this map: {string.Join(", ", parts)}.";
    }
}
