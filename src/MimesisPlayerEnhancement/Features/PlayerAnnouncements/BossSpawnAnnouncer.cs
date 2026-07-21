using System.Collections;
using MelonLoader;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements
{
    internal static class BossSpawnAnnouncer
    {
        private const float DebounceSeconds = 1f;
        private const float InitialSpawnGraceSeconds = 3f;

        private static readonly Dictionary<int, int> PendingSpawns = [];
        private static int _flushGeneration;
        private static float _suppressUntilTime;

        internal static void BeginDungeonRun()
        {
            _suppressUntilTime = Time.time + InitialSpawnGraceSeconds;
            PendingSpawns.Clear();
            _flushGeneration++;
        }

        internal static void ResetForSessionEnd()
        {
            PendingSpawns.Clear();
            _flushGeneration++;
            _suppressUntilTime = 0f;
        }

        internal static void RecordSpawn(int masterId)
        {
            if (!ModConfig.ShowPlayerAnnouncements.Value)
            {
                return;
            }

            if (Time.time < _suppressUntilTime)
            {
                return;
            }

            SpawnCategory category = SpawnCategoryLookup.GetCategory(masterId);
            if (category is not (SpawnCategory.Boss or SpawnCategory.Special))
            {
                return;
            }

            _ = PendingSpawns.TryGetValue(masterId, out int count);
            PendingSpawns[masterId] = count + 1;

            int generation = ++_flushGeneration;
            _ = MelonCoroutines.Start(FlushAfterDelay(generation));
        }

        private static IEnumerator FlushAfterDelay(int generation)
        {
            yield return new WaitForSeconds(DebounceSeconds);

            if (generation != _flushGeneration)
            {
                yield break;
            }

            if (PendingSpawns.Count == 0)
            {
                yield break;
            }

            string message = BossSpawnMessageFormatter.Format(
                PendingSpawns,
                masterId => EntityDisplayNameFormatter.Humanize(MonsterTypeLookup.GetDisplayName(masterId)));
            PendingSpawns.Clear();

            if (string.IsNullOrWhiteSpace(message))
            {
                yield break;
            }

            PlayerAnnouncements.ShowToast(message, isEntering: false);
        }
    }
}
