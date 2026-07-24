using FishNet.Object.Synchronizing;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    /// <summary>
    /// Unified voice pool eviction when indoor/outdoor caps are merged.
    /// Deathmatch keeps its own cap; all other events share one bucket.
    /// Ranking matches vanilla EvaluateValue (game@0.3.1 …/SpeechEventArchive.cs:L312-315).
    /// </summary>
    internal static class SpeechEventArchiveUnifiedEviction
    {
        private static readonly List<SpeechEvent> _deathMatchEvents = [];
        private static readonly List<SpeechEvent> _sharedEvents = [];
        private static readonly List<long> _removedIds = [];
        private static readonly HashSet<long> _removedIdSet = [];

        internal static List<long> TryEvict(SpeechEventArchive archive)
        {
            _removedIds.Clear();
            if (archive == null)
            {
                return CopyRemovedIds();
            }

            SyncList<SpeechEvent>? events = archive.events;
            if (events == null || events.Count == 0)
            {
                return CopyRemovedIds();
            }

            _deathMatchEvents.Clear();
            _sharedEvents.Clear();
            for (int i = 0; i < events.Count; i++)
            {
                SpeechEvent speechEvent = events[i];
                SpeechEventAdditionalGameData? gameData = speechEvent.GameData;
                if (gameData != null && VoiceEventContext.IsDeathMatch(gameData.Area))
                {
                    _deathMatchEvents.Add(speechEvent);
                }
                else
                {
                    _sharedEvents.Add(speechEvent);
                }
            }

            int deathMatchCap = ModConfig.MaxDeathMatchVoiceEvents.Value;
            int sharedCap = ModConfig.MaxIndoorVoiceEvents.Value + ModConfig.MaxOutdoorVoiceEvents.Value;

            CollectRemovals(_sharedEvents, sharedCap, _removedIds);
            CollectRemovals(_deathMatchEvents, deathMatchCap, _removedIds);

            if (_removedIds.Count > 0)
            {
                _removedIdSet.Clear();
                for (int i = 0; i < _removedIds.Count; i++)
                {
                    _ = _removedIdSet.Add(_removedIds[i]);
                }

                for (int i = events.Count - 1; i >= 0; i--)
                {
                    if (_removedIdSet.Contains(events[i].Id))
                    {
                        events.RemoveAt(i);
                    }
                }
            }

            return CopyRemovedIds();
        }

        /// <summary>
        /// Keeps the highest-value events (vanilla: least played) and appends overflow ids.
        /// </summary>
        internal static void CollectRemovals(List<SpeechEvent> bucket, int cap, List<long> removedIds)
        {
            if (bucket.Count <= cap)
            {
                return;
            }

            int removeCount = bucket.Count - cap;
            bucket.Sort(CompareValueDescending);
            for (int i = bucket.Count - removeCount; i < bucket.Count; i++)
            {
                removedIds.Add(bucket[i].Id);
            }
        }

        /// <summary>Vanilla EvaluateValue: higher (less negative) means more valuable.</summary>
        internal static float EvaluateValue(SpeechEvent speechEvent) => -speechEvent.AudioPlayedCount;

        private static int CompareValueDescending(SpeechEvent left, SpeechEvent right) =>
            EvaluateValue(right).CompareTo(EvaluateValue(left));

        private static List<long> CopyRemovedIds() => [.. _removedIds];
    }
}
