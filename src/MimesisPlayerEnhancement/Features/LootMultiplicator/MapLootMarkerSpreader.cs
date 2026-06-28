using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Bifrost.ConstEnum;
using HarmonyLib;
using MimesisPlayerEnhancement.Features.SpawnScaling;
using ReluProtocol;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator;

/// <summary>
/// Places extra map loot copies at unused loot markers instead of stacking at the spawn position.
/// Used for random loot pools at spawn time; fixed loot uses <see cref="FixedLootSpawnCoordinator"/>.
/// </summary>
internal static class MapLootMarkerSpreader
{
    private const BindingFlags InstanceFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private static readonly Type[] SpawnLootingObjectParameterTypes =
    {
        typeof(ItemElement),
        typeof(PosWithRot),
        typeof(bool),
        typeof(ReasonOfSpawn),
        typeof(int),
        typeof(int),
        typeof(long),
        typeof(bool),
        typeof(bool),
    };

    private static readonly FieldInfo SpawnedActorDatasField =
        typeof(DungeonRoom).GetField("_spawnedActorDatas", InstanceFlags)
        ?? throw new InvalidOperationException("DungeonRoom._spawnedActorDatas not found");

    private static readonly MethodInfo SpawnLootingObjectMethod =
        AccessTools.Method(typeof(IVroom), "SpawnLootingObject", SpawnLootingObjectParameterTypes)
        ?? throw new InvalidOperationException("IVroom.SpawnLootingObject not found");

    internal static void ShuffleMarkers<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    internal static void TrySpreadToUnusedMarkers(
        IVroom vroom,
        ItemElement template,
        ReasonOfSpawn reasonOfSpawn,
        int prevProjectileActorId,
        long projectileDropTime,
        bool ignoreNav,
        bool isRestored)
    {
        if (vroom is not DungeonRoom room || template == null || template.ItemMasterID <= 0)
            return;

        ItemType itemType = ItemElementStackHelper.GetItemType(template);
        int playerCount = LootPlayerCountHelper.ResolvePlayerCount(vroom);
        float multiplier = LootMultiplierResolver.GetEffectiveMultiplier(LootSource.Map, itemType, playerCount);
        int targetPiles = LootMultiplierResolver.ScaleCount(1, multiplier);
        int extraPiles = targetPiles - 1;
        if (extraPiles <= 0)
            return;

        var usedMarkerIds = CollectUsedMarkerIds(room);
        List<MapMarker_LootingObjectSpawnPoint> unusedMarkers =
            CollectUnusedMarkers(template.ItemMasterID, usedMarkerIds);

        if (unusedMarkers.Count == 0)
            return;

        ShuffleMarkers(unusedMarkers);

        int spawned = 0;
        LootSpawnScalingContext.BeginDuplicating();
        try
        {
            for (int i = 0; i < unusedMarkers.Count && spawned < extraPiles; i++)
            {
                MapMarker_LootingObjectSpawnPoint marker = unusedMarkers[i];
                ItemElement? copy = CreateSpawnCopy(vroom, template);
                if (copy == null)
                    continue;

                int actorId = (int)SpawnLootingObjectMethod.Invoke(
                    vroom,
                    new object[]
                    {
                        copy,
                        marker.pos,
                        marker.IsIndoor,
                        reasonOfSpawn,
                        marker.ID,
                        prevProjectileActorId,
                        projectileDropTime,
                        marker.ignoreNav || ignoreNav,
                        isRestored,
                    })!;

                if (actorId != 0)
                    spawned++;
            }
        }
        finally
        {
            LootSpawnScalingContext.EndDuplicating();
        }

        if (spawned <= 0)
            return;

        LootMultiplicatorLog.InfoRuntimeScaled(
            LootSource.Map,
            itemType,
            template.ItemMasterID,
            1,
            1 + spawned,
            multiplier,
            $"SpawnLootingObject/{reasonOfSpawn}/markers");
    }

    internal static List<MapMarker_LootingObjectSpawnPoint> CollectUnusedMarkers(
        int masterId,
        HashSet<int> usedMarkerIds)
    {
        var unused = new List<MapMarker_LootingObjectSpawnPoint>();

        foreach (MapMarker_LootingObjectSpawnPoint marker in CollectLootMarkers())
        {
            if (marker.masterID != masterId || usedMarkerIds.Contains(marker.ID))
                continue;

            unused.Add(marker);
        }

        return unused;
    }

    internal static HashSet<int> CollectUsedMarkerIds(DungeonRoom room)
    {
        var used = new HashSet<int>();

        if (SpawnedActorDatasField.GetValue(room) is not IDictionary spawnDatas)
            return used;

        foreach (DictionaryEntry entry in spawnDatas)
        {
            if (entry.Value is not SpawnedActorData spawnData || spawnData.Index == 0)
                continue;

            used.Add(spawnData.Index);
        }

        return used;
    }

    internal static MapMarker_LootingObjectSpawnPoint[] CollectLootMarkers() =>
        UnityEngine.Object.FindObjectsByType<MapMarker_LootingObjectSpawnPoint>(FindObjectsSortMode.None);

    private static ItemElement? CreateSpawnCopy(IVroom vroom, ItemElement template)
    {
        try
        {
            ItemInfo info = template.toItemInfo();
            return vroom.GetNewItemElement(
                info.itemMasterID,
                info.isFake,
                1,
                info.durability,
                info.remainGauge,
                info.price);
        }
        catch
        {
            return null;
        }
    }
}
