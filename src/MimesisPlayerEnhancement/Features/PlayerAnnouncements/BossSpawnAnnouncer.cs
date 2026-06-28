using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using MimesisPlayerEnhancement.Features.SpawnScaling;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements;

internal static class BossSpawnAnnouncer
{
    private const float DebounceSeconds = 1f;
    private const float InitialSpawnGraceSeconds = 3f;

    private static readonly Dictionary<int, int> PendingSpawns = new();
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
            return;

        if (Time.time < _suppressUntilTime)
            return;

        SpawnCategory category = SpawnCategoryLookup.GetCategory(masterId);
        if (category is not (SpawnCategory.Boss or SpawnCategory.Special))
            return;

        PendingSpawns.TryGetValue(masterId, out int count);
        PendingSpawns[masterId] = count + 1;

        int generation = ++_flushGeneration;
        MelonCoroutines.Start(FlushAfterDelay(generation));
    }

    private static IEnumerator FlushAfterDelay(int generation)
    {
        yield return new WaitForSeconds(DebounceSeconds);

        if (generation != _flushGeneration)
            yield break;

        if (PendingSpawns.Count == 0)
            yield break;

        string message = FormatSpawnMessage(PendingSpawns);
        PendingSpawns.Clear();

        if (string.IsNullOrWhiteSpace(message))
            yield break;

        PlayerAnnouncements.ShowToast(message, isEntering: false);
    }

    private static string FormatSpawnMessage(Dictionary<int, int> spawns)
    {
        var segments = new List<string>();
        foreach (var kvp in spawns)
        {
            string name = MonsterTypeLookup.GetDisplayName(kvp.Key);
            segments.Add(FormatSegment(kvp.Value, name));
        }

        if (segments.Count == 0)
            return "";

        string joined = segments.Count switch
        {
            1 => segments[0],
            2 => $"{segments[0]} and {segments[1]}",
            _ => string.Join(", ", segments.GetRange(0, segments.Count - 1)) + $", and {segments[^1]}",
        };

        return $"{joined} appeared. Be careful!";
    }

    private static string FormatSegment(int count, string name)
    {
        if (count <= 0)
            return "";

        if (count == 1)
            return $"A {name}";

        return $"{count} {name}";
    }
}
