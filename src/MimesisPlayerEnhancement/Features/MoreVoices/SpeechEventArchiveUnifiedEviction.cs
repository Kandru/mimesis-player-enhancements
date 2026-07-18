using System.Reflection;
using FishNet.Object.Synchronizing;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    /// <summary>
    /// Unified voice pool eviction when indoor/outdoor caps are merged.
    /// Deathmatch keeps its own cap; all other events share one bucket.
    /// </summary>
    internal static class SpeechEventArchiveUnifiedEviction
    {
        private static readonly MethodInfo? EvaluateValueMethod =
            AccessTools.Method(typeof(SpeechEventArchive), "EvaluateValue");

        private static readonly List<SpeechEvent> _deathMatchEvents = [];
        private static readonly List<SpeechEvent> _sharedEvents = [];
        private static readonly List<long> _removedIds = [];
        private static readonly HashSet<long> _removedIdSet = [];

        internal static bool IsAvailable => EvaluateValueMethod != null;

        internal static List<long> TryEvict(SpeechEventArchive archive)
        {
            _removedIds.Clear();
            if (archive == null || !IsAvailable)
            {
                return _removedIds;
            }

            SyncList<SpeechEvent>? events = archive.events;
            if (events == null || events.Count == 0)
            {
                return _removedIds;
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

            CollectRemovals(archive, _sharedEvents, sharedCap);
            CollectRemovals(archive, _deathMatchEvents, deathMatchCap);

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

            return _removedIds;
        }

        private static void CollectRemovals(SpeechEventArchive archive, List<SpeechEvent> bucket, int cap)
        {
            if (bucket.Count <= cap)
            {
                return;
            }

            int removeCount = bucket.Count - cap;
            bucket.Sort((a, b) => CompareValue(archive, b, a));
            for (int i = bucket.Count - removeCount; i < bucket.Count; i++)
            {
                _removedIds.Add(bucket[i].Id);
            }
        }

        private static int CompareValue(SpeechEventArchive archive, SpeechEvent left, SpeechEvent right)
        {
            float leftValue = Evaluate(archive, left);
            float rightValue = Evaluate(archive, right);
            return leftValue.CompareTo(rightValue);
        }

        private static float Evaluate(SpeechEventArchive archive, SpeechEvent speechEvent)
        {
            try
            {
                object?[] args = [speechEvent];
                return (float)EvaluateValueMethod!.Invoke(archive, args)!;
            }
            catch
            {
                return 0f;
            }
        }
    }
}
