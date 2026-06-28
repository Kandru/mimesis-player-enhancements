using System;
using System.Reflection;
using DunGen;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.DungeonSizeScaling;

public static class DungeonSizeScalingPatches
{
    private const string Feature = "DungeonSizeScaling";

    public static void Apply(HarmonyLib.Harmony harmony)
    {
        _ = GameNetworkApi.GetGameAssembly();

        var result = HarmonyPatchHelper.ApplyPatchTypes(
            harmony,
            Feature,
            HarmonyPatchHelper.GetNestedPatchTypes(typeof(DungeonSizeScalingPatches)));

        LogPatchAudit(harmony);
        HarmonyPatchHelper.LogPatchSummary(Feature, result);
    }

    private static void LogPatchAudit(HarmonyLib.Harmony harmony)
    {
        HarmonyPatchHelper.LogPatchAudit(Feature, harmony, new (string, MethodBase?)[]
        {
            ("Generate/DungeonGenerator", AccessTools.Method(typeof(DungeonGenerator), nameof(DungeonGenerator.Generate))),
            ("Clear/DungeonGenerator", AccessTools.Method(typeof(DungeonGenerator), nameof(DungeonGenerator.Clear))),
        });
    }

    [HarmonyPatch(typeof(DungeonGenerator), nameof(DungeonGenerator.Generate))]
    public static class DungeonGeneratorGeneratePatch
    {
        [HarmonyPrefix]
        public static void Prefix(DungeonGenerator __instance)
        {
            try
            {
                DungeonSizeScalingApplier.ApplyBeforeGenerate(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Generate prefix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(DungeonGenerator), nameof(DungeonGenerator.Clear))]
    public static class DungeonGeneratorClearPatch
    {
        [HarmonyPostfix]
        public static void Postfix(DungeonGenerator __instance)
        {
            try
            {
                DungeonSizeScalingApplier.OnGeneratorCleared(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Clear postfix failed — {ex.Message}");
            }
        }
    }
}
