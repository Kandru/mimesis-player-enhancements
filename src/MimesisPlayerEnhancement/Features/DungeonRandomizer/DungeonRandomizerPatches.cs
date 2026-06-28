using System;
using System.Collections.Generic;
using System.Reflection;
using Bifrost.Cooked;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.DungeonRandomizer;

public static class DungeonRandomizerPatches
{
    private const string Feature = "DungeonRandomizer";

    public static void Apply(HarmonyLib.Harmony harmony)
    {
        _ = GameNetworkApi.GetGameAssembly();

        var result = HarmonyPatchHelper.ApplyPatchTypes(
            harmony,
            Feature,
            HarmonyPatchHelper.GetNestedPatchTypes(typeof(DungeonRandomizerPatches)));

        LogPatchAudit(harmony);
        HarmonyPatchHelper.LogPatchSummary(Feature, result);
    }

    private static void LogPatchAudit(HarmonyLib.Harmony harmony)
    {
        HarmonyPatchHelper.LogPatchAudit(Feature, harmony, new (string, MethodBase?)[]
        {
            ("PickDungeon/ExcelDataManager", AccessTools.Method(typeof(ExcelDataManager), nameof(ExcelDataManager.PickDungeon))),
            ("GetRandomDungenName/DungeonMasterInfo", AccessTools.Method(typeof(DungeonMasterInfo), nameof(DungeonMasterInfo.GetRandomDungenName))),
            ("PickMapID/DungeonMasterInfo", AccessTools.Method(typeof(DungeonMasterInfo), nameof(DungeonMasterInfo.PickMapID))),
            ("SetNextDungeonMasterID/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), nameof(GameSessionInfo.SetNextDungeonMasterID))),
        });
    }

    [HarmonyPatch(typeof(ExcelDataManager), nameof(ExcelDataManager.PickDungeon))]
    public static class ExcelDataManagerPickDungeonPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref List<int> excludeDungeonIDs)
        {
            try
            {
                if (!DungeonPickResolver.ShouldClearExcludeList())
                    return;

                excludeDungeonIDs = new List<int>();
            }
            catch (Exception ex)
            {
                DungeonRandomizerLog.Warn($"PickDungeon prefix failed — {ex.Message}");
            }
        }

        [HarmonyPostfix]
        public static void Postfix(ref int __result)
        {
            try
            {
                if (!DungeonRandomizerHost.ShouldApply() || !ModConfig.RandomizeDungeonPick.Value)
                    return;

                __result = DungeonPickResolver.ResolvePick(__result);
            }
            catch (Exception ex)
            {
                DungeonRandomizerLog.Warn($"PickDungeon postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(DungeonMasterInfo), nameof(DungeonMasterInfo.GetRandomDungenName))]
    public static class DungeonMasterInfoGetRandomDungenNamePatch
    {
        [HarmonyPostfix]
        public static void Postfix(DungeonMasterInfo __instance, ref string __result)
        {
            try
            {
                if (!DungeonRandomizerHost.ShouldApply())
                    return;

                string? replacement = DungeonVariantResolver.ResolveLayoutFlow(__instance, __result);
                if (replacement != null)
                    __result = replacement;
            }
            catch (Exception ex)
            {
                DungeonRandomizerLog.Warn($"GetRandomDungenName postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(DungeonMasterInfo), nameof(DungeonMasterInfo.PickMapID))]
    public static class DungeonMasterInfoPickMapIdPatch
    {
        [HarmonyPostfix]
        public static void Postfix(DungeonMasterInfo __instance, ref int __result)
        {
            try
            {
                if (!DungeonRandomizerHost.ShouldApply())
                    return;

                int? replacement = DungeonVariantResolver.ResolveMapId(__instance, __result);
                if (replacement.HasValue)
                    __result = replacement.Value;
            }
            catch (Exception ex)
            {
                DungeonRandomizerLog.Warn($"PickMapID postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GameSessionInfo), nameof(GameSessionInfo.SetNextDungeonMasterID))]
    public static class GameSessionInfoSetNextDungeonMasterIdPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref int randomDungeonSeed)
        {
            try
            {
                if (!DungeonRandomizerHost.ShouldApply())
                    return;

                randomDungeonSeed = DungeonSeedResolver.RollSeed(randomDungeonSeed);
            }
            catch (Exception ex)
            {
                DungeonRandomizerLog.Warn($"SetNextDungeonMasterID prefix failed — {ex.Message}");
            }
        }
    }
}
