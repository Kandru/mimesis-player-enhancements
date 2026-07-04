using FishNet.Object.Synchronizing;
using Mimic.Voice.SpeechSystem;

namespace MimesisPlayerEnhancement.Features.Persistence
{
    internal static class SpeechEventInjector
    {
        internal readonly struct RestoreResult
        {
            internal RestoreResult(int fromPool, int fromReconnect)
            {
                FromPool = fromPool;
                FromReconnect = fromReconnect;
            }

            internal int FromPool { get; }
            internal int FromReconnect { get; }
            internal int TotalAdded => FromPool + FromReconnect;
        }

        internal static RestoreResult RestoreIntoArchive(
            SpeechEventArchive archive,
            string? playerId,
            long playerUID,
            bool isLocal)
        {
            SyncList<SpeechEvent> eventsList = archive.events;
            if (eventsList == null)
            {
                return default;
            }

            float currentTime = GameSessionAccess.GetCurrentTickSec();
            HashSet<long> seenIds = [];
            for (int i = 0; i < eventsList.Count; i++)
            {
                _ = seenIds.Add(eventsList[i].Id);
            }

            int fromPool = 0;
            int fromReconnect = 0;

            if (SpeechEventPoolManager.HasPending())
            {
                List<SpeechEvent> claimed = SpeechEventPoolManager.ClaimEventsForArchive(
                    playerId, playerUID, isLocal, archive);
                fromPool = InjectEvents(eventsList, claimed, seenIds, currentTime);
            }

            if (SpeechEventPoolManager.DisconnectedCacheCount > 0)
            {
                List<SpeechEvent> reclaimed = SpeechEventPoolManager.ClaimDisconnectedEventsForArchive(
                    playerId, playerUID, isLocal);
                fromReconnect = InjectEvents(eventsList, reclaimed, seenIds, currentTime);
            }

            return new RestoreResult(fromPool, fromReconnect);
        }

        internal static int CollectFromArchive(
            SpeechEventArchive archive,
            HashSet<long> seenIds,
            List<SpeechEvent> output)
        {
            SyncList<SpeechEvent>? eventsList = archive.events;
            if (eventsList == null)
            {
                return 0;
            }

            int added = 0;
            for (int i = 0; i < eventsList.Count; i++)
            {
                SpeechEvent ev = eventsList[i];
                if (ev == null || !seenIds.Add(ev.Id))
                {
                    continue;
                }

                output.Add(ev);
                added++;
            }

            return added;
        }

        private static int InjectEvents(
            SyncList<SpeechEvent> eventsList,
            List<SpeechEvent>? events,
            HashSet<long> seenIds,
            float currentTime)
        {
            if (events == null || events.Count == 0)
            {
                return 0;
            }

            int added = 0;
            foreach (SpeechEvent ev in events)
            {
                if (ev == null || seenIds.Contains(ev.Id))
                {
                    continue;
                }

                SpeechEventPoolManager.FixEventTiming(ev, currentTime);
                eventsList.Add(ev);
                _ = seenIds.Add(ev.Id);
                added++;
            }

            return added;
        }
    }
}
