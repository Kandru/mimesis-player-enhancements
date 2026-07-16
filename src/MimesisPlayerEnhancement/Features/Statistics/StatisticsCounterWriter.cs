using MimesisPlayerEnhancement.Features.Statistics.Models;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    internal static class StatisticsCounterWriter
    {
        internal static void Modify(ulong steamId, Action<StatCounters> apply, bool notify = true)
        {
            if (steamId == 0 || apply == null)
            {
                return;
            }

            PlayerStatisticsDocument doc = PlayerRegistry.GetOrCreate(steamId).Statistics;
            EnsureDocument(doc);
            int zone = StatisticsRunTracker.GetCurrentZone();
            StatCounters zoneCounters = GetOrCreateZoneCounters(doc.CurrentRun, zone);

            apply(doc.CurrentSession!.Counters);
            apply(doc.Global.Counters);
            apply(doc.CurrentRun.Counters);
            apply(zoneCounters);

            if (notify)
            {
                NotifyChanged();
            }
        }

        internal static void ModifyDictionary(
            ulong steamId,
            Func<StatCounters, Dictionary<string, long>> selector,
            string key,
            bool notify = true)
        {
            if (steamId == 0 || string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            Modify(steamId, counters => IncrementDictionaryValue(selector(counters), key), notify);
        }

        internal static void MergeDelta(ulong steamId, StatCounters delta, bool notify = true)
        {
            if (steamId == 0 || delta == null)
            {
                return;
            }

            PlayerStatisticsDocument doc = PlayerRegistry.GetOrCreate(steamId).Statistics;
            EnsureDocument(doc);
            int zone = StatisticsRunTracker.GetCurrentZone();

            doc.CurrentSession!.Counters.Add(delta);
            doc.Global.Counters.Add(delta);
            doc.CurrentRun.Counters.Add(delta);
            GetOrCreateZoneCounters(doc.CurrentRun, zone).Add(delta);

            if (notify)
            {
                NotifyChanged();
            }
        }

        internal static void AppendLifetimeOnDeath(ulong steamId, long lifetimeMs, bool notify = true)
        {
            if (steamId == 0 || lifetimeMs <= 0)
            {
                return;
            }

            Modify(steamId, counters =>
            {
                if (counters.LifetimesOnDeathMs.Count >= StatCounters.MaxLifetimeSamples)
                {
                    counters.LifetimesOnDeathMs.RemoveAt(0);
                }

                counters.LifetimesOnDeathMs.Add(lifetimeMs);
            }, notify);
        }

        internal static void AddConnectedSeconds(ulong steamId, long seconds)
        {
            if (steamId == 0 || seconds <= 0)
            {
                return;
            }

            PlayerStatisticsDocument doc = PlayerRegistry.GetOrCreate(steamId).Statistics;
            EnsureDocument(doc);
            int zone = StatisticsRunTracker.GetCurrentZone();

            doc.CurrentSession!.Counters.TotalConnectedSeconds += seconds;
            doc.Global.Counters.TotalConnectedSeconds += seconds;
            doc.CurrentRun.Counters.TotalConnectedSeconds += seconds;
            GetOrCreateZoneCounters(doc.CurrentRun, zone).TotalConnectedSeconds += seconds;
        }

        internal static void NotifyChanged()
        {
            PlayerRegistry.BumpRevision();
            WebDashboardSnapshotCache.MarkDirty();
        }

        private static void IncrementDictionaryValue(Dictionary<string, long> dictionary, string key)
        {
            _ = dictionary.TryGetValue(key, out long current);
            dictionary[key] = current + 1;
        }

        private static void EnsureDocument(PlayerStatisticsDocument doc)
        {
            doc.CurrentSession ??= StatisticsTracker.CreateSession(DateTime.UtcNow);
            doc.CurrentSession.Counters ??= new StatCounters();
            doc.Global ??= new GlobalStats();
            doc.Global.Counters ??= new StatCounters();
            doc.CurrentRun ??= new RunStats();
            doc.CurrentRun.Counters ??= new StatCounters();
            doc.CurrentRun.Zones ??= [];
            EnsureCounterDictionaries(doc.CurrentSession.Counters);
            EnsureCounterDictionaries(doc.Global.Counters);
            EnsureCounterDictionaries(doc.CurrentRun.Counters);
        }

        private static StatCounters GetOrCreateZoneCounters(RunStats run, int zone)
        {
            if (zone <= 0)
            {
                zone = 1;
            }

            if (!run.Zones.TryGetValue(zone, out StatCounters? counters))
            {
                counters = new StatCounters();
                run.Zones[zone] = counters;
            }

            EnsureCounterDictionaries(counters);
            return counters;
        }

        private static void EnsureCounterDictionaries(StatCounters counters)
        {
            counters.MonsterKills ??= [];
            counters.DeathsByMonster ??= [];
            counters.DeathsByTrap ??= [];
            counters.LifetimesOnDeathMs ??= [];
        }
    }
}
