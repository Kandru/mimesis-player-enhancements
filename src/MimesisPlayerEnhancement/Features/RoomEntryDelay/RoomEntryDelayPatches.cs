using System;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.RoomEntryDelay
{
    public static class RoomEntryDelayPatches
    {
        private const string Feature = "RoomEntryDelay";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(RoomEntryDelayPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("GetGameActionDelay/ILevelObjectInfo", AccessTools.Method(typeof(ILevelObjectInfo), nameof(ILevelObjectInfo.GetGameActionDelay))),
                ("GetTransitionDuration/StaticLevelObject", AccessTools.Method(typeof(StaticLevelObject), nameof(StaticLevelObject.GetTransitionDuration))),
            ]);
        }

        [HarmonyPatch(typeof(ILevelObjectInfo), nameof(ILevelObjectInfo.GetGameActionDelay))]
        public static class ILevelObjectInfoGetGameActionDelayPatch
        {
            [HarmonyPostfix]
            public static void Postfix(
                ILevelObjectInfo __instance,
                int fromState,
                int toState,
                ref long __result)
            {
                try
                {
                    if (!RoomEntryDelayResolver.ShouldApply)
                    {
                        return;
                    }

                    float multiplier = RoomEntryDelayResolver.GetMultiplier();
                    if (RoomEntryDelayResolver.IsVanillaMultiplier(multiplier))
                    {
                        return;
                    }

                    if (!RoomEntryDelayFilter.IsRoomEntryTeleportTransition(__instance, fromState, toState))
                    {
                        return;
                    }

                    __result = RoomEntryDelayResolver.ScaleDelayMilliseconds(__result, multiplier);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"GetGameActionDelay postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(StaticLevelObject), nameof(StaticLevelObject.GetTransitionDuration))]
        public static class StaticLevelObjectGetTransitionDurationPatch
        {
            [HarmonyPostfix]
            public static void Postfix(StaticLevelObject __instance, ref float __result)
            {
                try
                {
                    if (!RoomEntryDelayResolver.ShouldApply)
                    {
                        return;
                    }

                    float multiplier = RoomEntryDelayResolver.GetMultiplier();
                    if (RoomEntryDelayResolver.IsVanillaMultiplier(multiplier))
                    {
                        return;
                    }

                    if (!RoomEntryDelayTransitionAccess.TryGetCurrentTransition(__instance, out int fromState, out int toState))
                    {
                        return;
                    }

                    if (!RoomEntryDelayFilter.IsRoomEntryTeleportTransition(__instance, fromState, toState))
                    {
                        return;
                    }

                    __result = RoomEntryDelayResolver.ScaleDurationSeconds(__result, multiplier);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"GetTransitionDuration postfix failed — {ex.Message}");
                }
            }
        }
    }
}
