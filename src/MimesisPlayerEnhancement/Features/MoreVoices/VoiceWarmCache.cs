using System.Reflection;
using FishNet.Object.Synchronizing;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    internal static class VoiceWarmCache
    {
        private const string Feature = "MoreVoices";
        private const float CacheTtlSec = 1f;

        private static readonly FieldInfo? WarmUpDurationField =
            AccessTools.Field(typeof(SpeechEventArchive), "warmUpDuration");

        private sealed class CacheEntry
        {
            internal readonly List<SpeechEvent> WarmedEvents = [];
            internal readonly List<(string PlayerId, SpeechEvent Event)> WarmedPairs = [];
            internal readonly Dictionary<long, SpeechEvent> EventsById = [];
            internal readonly HashSet<long> EventIds = [];
            internal SyncList<SpeechEvent>.SyncListChanged? OnChangeHandler;
            internal int WarmedCount;
            internal float BuiltAtTick;
            internal bool IsDirty = true;
        }

        private static readonly Dictionary<SpeechEventArchive, CacheEntry> _entries = [];

        internal static void Attach(SpeechEventArchive archive)
        {
            if (!VoicePerformanceRuntime.IsActive || archive?.events == null)
            {
                return;
            }

            if (_entries.ContainsKey(archive))
            {
                return;
            }

            CacheEntry entry = new();
            entry.OnChangeHandler = (_, _, _, _, _) => MarkDirty(archive);
            archive.events.OnChange += entry.OnChangeHandler;
            _entries[archive] = entry;
            VoiceDissonancePlayerCache.MarkDirty();

            if (ModConfig.EnableDebugLogging.Value)
            {
                ModLog.Debug(Feature, $"Voice cache attached — {VoiceEventStats.DescribePlayerBrief(archive)}");
            }
        }

        internal static void Detach(SpeechEventArchive archive)
        {
            if (archive == null || !_entries.TryGetValue(archive, out CacheEntry? entry))
            {
                return;
            }

            try
            {
                if (entry.OnChangeHandler != null && archive.events != null)
                {
                    archive.events.OnChange -= entry.OnChangeHandler;
                }
            }
            catch
            {
                /* archive may be partially torn down */
            }

            VoiceClipCache.RemoveClipsForArchive(entry.EventIds);
            _ = _entries.Remove(archive);
            VoiceDissonancePlayerCache.MarkDirty();

            if (ModConfig.EnableDebugLogging.Value)
            {
                ModLog.Debug(Feature, "Voice cache detached — archive stopped");
            }
        }

        internal static void ClearAll()
        {
            foreach (SpeechEventArchive archive in new List<SpeechEventArchive>(_entries.Keys))
            {
                Detach(archive);
            }

            _entries.Clear();
        }

        internal static void AppendWarmedPairs(
            SpeechEventArchive archive,
            List<(string playerID, SpeechEvent evt)> destination)
        {
            if (archive == null || destination == null)
            {
                return;
            }

            if (!VoicePerformanceRuntime.IsActive || archive.events == null)
            {
                AppendWarmedPairsUncached(archive, destination);
                return;
            }

            CacheEntry entry = GetOrCreateEntry(archive);
            EnsureBuilt(archive, entry);
            for (int i = 0; i < entry.WarmedPairs.Count; i++)
            {
                (string playerId, SpeechEvent speechEvent) = entry.WarmedPairs[i];
                destination.Add((playerId, speechEvent));
            }
        }

        private static void AppendWarmedPairsUncached(
            SpeechEventArchive archive,
            List<(string playerID, SpeechEvent evt)> destination)
        {
            string playerId = archive.PlayerId;
            List<SpeechEvent> warmed = archive.events == null ? [] : BuildUncachedWarmedList(archive);
            for (int i = 0; i < warmed.Count; i++)
            {
                destination.Add((playerId, warmed[i]));
            }
        }

        internal static List<SpeechEvent> GetWarmedEvents(SpeechEventArchive archive)
        {
            if (!VoicePerformanceRuntime.IsActive || archive?.events == null)
            {
                return archive?.events == null ? [] : BuildUncachedWarmedList(archive);
            }

            CacheEntry entry = GetOrCreateEntry(archive);
            EnsureBuilt(archive, entry);
            return entry.WarmedEvents;
        }

        internal static int GetWarmedCount(SpeechEventArchive archive)
        {
            if (!VoicePerformanceRuntime.IsActive || archive?.events == null)
            {
                return archive?.events == null ? 0 : BuildUncachedWarmedList(archive).Count;
            }

            CacheEntry entry = GetOrCreateEntry(archive);
            EnsureBuilt(archive, entry);
            return entry.WarmedCount;
        }

        internal static SpeechEvent? TryGetSpeechEventById(SpeechEventArchive archive, long speechEventId)
        {
            if (archive?.events == null)
            {
                return null;
            }

            if (!VoicePerformanceRuntime.IsActive)
            {
                return archive.events.Find(e => e.Id == speechEventId);
            }

            CacheEntry entry = GetOrCreateEntry(archive);
            EnsureBuilt(archive, entry);
            return entry.EventsById.TryGetValue(speechEventId, out SpeechEvent speechEvent) ? speechEvent : null;
        }

        private static CacheEntry GetOrCreateEntry(SpeechEventArchive archive)
        {
            if (!_entries.TryGetValue(archive, out CacheEntry? entry))
            {
                Attach(archive);
                entry = _entries[archive];
            }

            return entry;
        }

        private static void MarkDirty(SpeechEventArchive archive)
        {
            if (_entries.TryGetValue(archive, out CacheEntry? entry))
            {
                entry.IsDirty = true;
            }
        }

        private static void EnsureBuilt(SpeechEventArchive archive, CacheEntry entry)
        {
            float now = GameSessionAccess.GetCurrentTickSec();
            if (!entry.IsDirty && now - entry.BuiltAtTick < CacheTtlSec)
            {
                return;
            }

            Rebuild(archive, entry, now);
        }

        private static void Rebuild(SpeechEventArchive archive, CacheEntry entry, float now)
        {
            entry.WarmedEvents.Clear();
            entry.WarmedPairs.Clear();
            entry.EventsById.Clear();
            entry.EventIds.Clear();

            string playerId = archive.PlayerId;
            float warmUpDuration = WarmUpDurationField != null
                ? (float)WarmUpDurationField.GetValue(archive)!
                : 10f;

            SyncList<SpeechEvent> events = archive.events;
            for (int i = 0; i < events.Count; i++)
            {
                SpeechEvent speechEvent = events[i];
                entry.EventsById[speechEvent.Id] = speechEvent;
                _ = entry.EventIds.Add(speechEvent.Id);

                if (now - speechEvent.RecordedTime > warmUpDuration)
                {
                    entry.WarmedEvents.Add(speechEvent);
                    entry.WarmedPairs.Add((playerId, speechEvent));
                }
            }

            entry.WarmedCount = entry.WarmedEvents.Count;
            entry.BuiltAtTick = now;
            entry.IsDirty = false;
        }

        private static List<SpeechEvent> BuildUncachedWarmedList(SpeechEventArchive archive)
        {
            float now = GameSessionAccess.GetCurrentTickSec();
            float warmUpDuration = WarmUpDurationField != null
                ? (float)WarmUpDurationField.GetValue(archive)!
                : 10f;

            List<SpeechEvent> warmed = [];
            SyncList<SpeechEvent> events = archive.events;
            for (int i = 0; i < events.Count; i++)
            {
                SpeechEvent speechEvent = events[i];
                if (now - speechEvent.RecordedTime > warmUpDuration)
                {
                    warmed.Add(speechEvent);
                }
            }

            return warmed;
        }
    }
}
