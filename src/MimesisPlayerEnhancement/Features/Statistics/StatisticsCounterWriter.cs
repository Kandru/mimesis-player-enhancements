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

            StatisticsHistory.Apply(steamId, apply, CounterScope.All);

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

            StatisticsHistory.Apply(steamId, counters => counters.Add(delta), CounterScope.All);

            if (notify)
            {
                NotifyChanged();
            }
        }

        internal static void AddConnectedSeconds(ulong steamId, long seconds)
        {
            if (steamId == 0 || seconds <= 0)
            {
                return;
            }

            StatisticsHistory.Apply(
                steamId,
                counters => counters.ConnectedSeconds += seconds,
                CounterScope.All);
        }

        internal static void AddVoiceEvents(ulong steamId, long delta)
        {
            if (steamId == 0 || delta == 0)
            {
                return;
            }

            PlayerGlobalStats global = StatisticsHistory.EnsureGlobal(steamId);
            global.VoiceEvents += delta;
            StatisticsHistory.BumpRevision();
        }

        internal static void NotifyChanged()
        {
            StatisticsHistory.BumpRevision();
            PlayerRegistry.BumpRevision();
            WebDashboardSnapshotCache.MarkDirty();
        }

        private static void IncrementDictionaryValue(Dictionary<string, long> dictionary, string key)
        {
            _ = dictionary.TryGetValue(key, out long current);
            dictionary[key] = current + 1;
        }
    }
}
