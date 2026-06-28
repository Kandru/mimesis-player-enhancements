using System;
using System.Collections;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator;

internal static class LootSpawnDataLookup
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly FieldInfo SpawnedActorDatasField =
        typeof(DungeonRoom).GetField("_spawnedActorDatas", InstanceFlags)
        ?? throw new InvalidOperationException("DungeonRoom._spawnedActorDatas not found");

    internal static bool TryFindByMarkerIndex(DungeonRoom room, int markerIndex, out SpawnedActorData? spawnData)
    {
        spawnData = null;
        if (markerIndex == 0)
            return false;

        if (SpawnedActorDatasField.GetValue(room) is not IDictionary spawnDatas)
            return false;

        foreach (DictionaryEntry entry in spawnDatas)
        {
            if (entry.Value is not SpawnedActorData candidate)
                continue;

            if (candidate.Index == markerIndex)
            {
                spawnData = candidate;
                return true;
            }
        }

        return false;
    }
}
