using System;
using System.Collections.Generic;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.DungeonRandomizer
{
    public static class DungeonRandomizerPatches
    {
        private const string Feature = "DungeonRandomizer";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(DungeonRandomizerPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("PickDungeon/ExcelDataManager", AccessTools.Method(typeof(ExcelDataManager), nameof(ExcelDataManager.PickDungeon))),
                ("RollDiceDungeon/VWaitingRoom", AccessTools.Method(typeof(VWaitingRoom), nameof(VWaitingRoom.RollDiceDungeon))),
                ("GetRandomDungenName/DungeonMasterInfo", AccessTools.Method(typeof(DungeonMasterInfo), nameof(DungeonMasterInfo.GetRandomDungenName))),
                ("PickMapID/DungeonMasterInfo", AccessTools.Method(typeof(DungeonMasterInfo), nameof(DungeonMasterInfo.PickMapID))),
                ("SetNextDungeonMasterID/GameSessionInfo", AccessTools.Method(typeof(GameSessionInfo), nameof(GameSessionInfo.SetNextDungeonMasterID))),
            ]);
        }

        private static bool ShouldApply =>
            HostApplyGate.ShouldApplyHostOnlyFeature(() => ModConfig.EnableDungeonRandomizer.Value);

        [HarmonyPatch(typeof(VWaitingRoom), nameof(VWaitingRoom.RollDiceDungeon))]
        public static class VWaitingRoomRollDiceDungeonPatch
        {
            internal static bool ClearFirstPickExcludes { get; set; }

            [HarmonyPrefix]
            public static void Prefix(bool reroll)
            {
                ClearFirstPickExcludes = reroll && ShouldIgnoreRerollExcludes();
            }

            [HarmonyFinalizer]
            public static Exception? Finalizer(Exception? __exception)
            {
                ClearFirstPickExcludes = false;
                return __exception;
            }
        }

        [HarmonyPatch(typeof(ExcelDataManager), nameof(ExcelDataManager.PickDungeon))]
        public static class ExcelDataManagerPickDungeonPatch
        {
            private static IReadOnlyList<int> _excludeIdsForResolve = [];

            [HarmonyPrefix]
            public static void Prefix(ref List<int> excludeDungeonIDs)
            {
                try
                {
                    if (VWaitingRoomRollDiceDungeonPatch.ClearFirstPickExcludes)
                    {
                        _excludeIdsForResolve = Array.Empty<int>();
                        excludeDungeonIDs = [];
                        VWaitingRoomRollDiceDungeonPatch.ClearFirstPickExcludes = false;
                        return;
                    }

                    _excludeIdsForResolve = excludeDungeonIDs;
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"PickDungeon prefix failed — {ex.Message}");
                }
            }

            [HarmonyPostfix]
            public static void Postfix(ref int __result)
            {
                try
                {
                    if (!ShouldApply || !ModConfig.RandomizeDungeonPick.Value)
                    {
                        return;
                    }

                    __result = DungeonPickResolver.ResolvePick(__result, _excludeIdsForResolve);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"PickDungeon postfix failed — {ex.Message}");
                }
                finally
                {
                    _excludeIdsForResolve = [];
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
                    if (!ShouldApply)
                    {
                        return;
                    }

                    string? replacement = DungeonVariantResolver.ResolveLayoutFlow(__instance, __result);
                    if (replacement != null)
                    {
                        __result = replacement;
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"GetRandomDungenName postfix failed — {ex.Message}");
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
                    if (!ShouldApply)
                    {
                        return;
                    }

                    int? replacement = DungeonVariantResolver.ResolveMapId(__instance, __result);
                    if (replacement.HasValue)
                    {
                        __result = replacement.Value;
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"PickMapID postfix failed — {ex.Message}");
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
                    if (!ShouldApply)
                    {
                        return;
                    }

                    randomDungeonSeed = DungeonSeedResolver.RollSeed(randomDungeonSeed);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"SetNextDungeonMasterID prefix failed — {ex.Message}");
                }
            }
        }

        private static bool ShouldIgnoreRerollExcludes()
        {
            return ShouldApply
                   && ModConfig.RandomizeDungeonPick.Value
                   && ModConfig.IgnoreDungeonExcludeList.Value
                   && DungeonIdListParser.ParsePoolMode(ModConfig.DungeonPickPoolMode.Value) == DungeonPickPoolMode.WidenVanilla;
        }
    }
}
