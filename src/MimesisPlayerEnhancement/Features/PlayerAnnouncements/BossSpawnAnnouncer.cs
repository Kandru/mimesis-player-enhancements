using System.Collections;
using MelonLoader;
using MimesisPlayerEnhancement.Features.SpawnScaling;
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

            string message = FormatSpawnMessage(PendingSpawns);
            PendingSpawns.Clear();

            if (string.IsNullOrWhiteSpace(message))
            {
                yield break;
            }

            PlayerAnnouncements.ShowToast(message, isEntering: false);
        }

        private static string FormatSpawnMessage(Dictionary<int, int> spawns)
        {
            List<string> segments = [];
            foreach (KeyValuePair<int, int> kvp in spawns)
            {
                string humanizedName = EntityDisplayNameFormatter.Humanize(MonsterTypeLookup.GetDisplayName(kvp.Key));
                string segment = FormatSegment(kvp.Value, humanizedName, capitalizeArticle: segments.Count == 0);
                if (!string.IsNullOrWhiteSpace(segment))
                {
                    segments.Add(segment);
                }
            }

            if (segments.Count == 0)
            {
                return "";
            }

            string joined = segments.Count switch
            {
                1 => segments[0],
                2 => $"{segments[0]} and {segments[1]}",
                _ => string.Join(", ", segments.GetRange(0, segments.Count - 1)) + $", and {segments[^1]}",
            };

            return $"{joined} appeared. Be careful!";
        }

        private static string FormatSegment(int count, string humanizedName, bool capitalizeArticle)
        {
            if (count <= 0 || string.IsNullOrWhiteSpace(humanizedName))
            {
                return "";
            }

            return count == 1
                ? EntityDisplayNameFormatter.FormatWithArticle(humanizedName, capitalizeArticle)
                : $"{count} {EntityDisplayNameFormatter.Pluralize(humanizedName)}";
        }
    }
}
