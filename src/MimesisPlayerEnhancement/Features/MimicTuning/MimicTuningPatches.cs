using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using Mimic.Actors;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.MimicTuning
{
    public static class MimicTuningPatches
    {
        private const string Feature = "MimicTuning";

        private static readonly FieldInfo? PossessionDurationField =
            AccessTools.Field(typeof(Bifrost.ConstEnum.DataConsts), nameof(Bifrost.ConstEnum.DataConsts.C_PossessionDuration));

        private static readonly FieldInfo? PossessionCooltimeField =
            AccessTools.Field(typeof(Bifrost.ConstEnum.DataConsts), nameof(Bifrost.ConstEnum.DataConsts.C_PossessionCooltime));

        private static readonly MethodInfo? PossessionProgressbarCoMethod =
            AccessTools.Method(typeof(ProtoActor), "PossessionProgressbarCo");

        private static readonly MethodInfo RollPossessionDurationMsMethod =
            AccessTools.Method(typeof(MimicTuningResolver), nameof(MimicTuningResolver.RollPossessionDurationMs));

        private static readonly MethodInfo ScalePossessionCooltimeMsMethod =
            AccessTools.Method(typeof(MimicTuningResolver), nameof(MimicTuningResolver.ScalePossessionCooltimeMs));

        private static readonly Dictionary<int, ProgressBarRestartState> ProgressBarRestartStates = [];

        private static readonly object[] ProgressBarCoArgs = new object[3];

        private sealed class ProgressBarRestartState
        {
            internal float TargetLeftTime;
            internal float TotalSeconds;
        }

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNestedPatchTypes(typeof(MimicTuningPatches)));

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("HandleStartPossessing/PossessionController",
                    AccessTools.Method(typeof(PossessionController), nameof(PossessionController.HandleStartPossessing))),
                ("ClearPossessingStateInternal/PossessionController",
                    AccessTools.Method(typeof(PossessionController), "ClearPossessingStateInternal")),
                ("UpdatePossessionProgressbar/ProtoActor",
                    AccessTools.Method(typeof(ProtoActor), nameof(ProtoActor.UpdatePossessionProgressbar))),
                ("Start/UIPrefab_Spectator",
                    AccessTools.Method(typeof(UIPrefab_Spectator), "Start")),
                ("UpdatePossessionCooltime/UIPrefab_Spectator",
                    AccessTools.Method(typeof(UIPrefab_Spectator), nameof(UIPrefab_Spectator.UpdatePossessionCooltime))),
                ("OnEndPossession/ProtoActor",
                    AccessTools.Method(typeof(ProtoActor), nameof(ProtoActor.OnEndPossession))),
            ]);
        }

        [HarmonyPatch(typeof(PossessionController), nameof(PossessionController.HandleStartPossessing))]
        internal static class HandleStartPossessingTranspiler
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceConstIntLoad(
                    instructions,
                    PossessionDurationField,
                    afterLoad =>
                    [
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Call, RollPossessionDurationMsMethod),
                    ]);
            }
        }

        [HarmonyPatch(typeof(PossessionController), "ClearPossessingStateInternal")]
        internal static class ClearPossessingStateInternalTranspiler
        {
            [HarmonyTranspiler]
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return ReplaceConstIntLoad(
                    instructions,
                    PossessionCooltimeField,
                    _ =>
                    [
                        new CodeInstruction(OpCodes.Call, ScalePossessionCooltimeMsMethod),
                    ]);
            }
        }

        [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.UpdatePossessionProgressbar))]
        internal static class UpdatePossessionProgressbarPrefix
        {
            [HarmonyPrefix]
            internal static bool Prefix(
                ProtoActor __instance,
                float inServerLeftTime,
                ref Coroutine ____progressCoroutine,
                Image ____possessionProgressbar)
            {
                if (!ModConfig.EnableMimicTuning.Value || PossessionProgressbarCoMethod == null)
                {
                    return true;
                }

                try
                {
                    float totalSeconds = MimicTuningResolver.GetProgressBarTotalSeconds(
                        __instance.ActorID,
                        inServerLeftTime);
                    float targetLeftTime = inServerLeftTime * 0.001f;

                    if (____progressCoroutine != null
                        && ProgressBarRestartStates.TryGetValue(__instance.ActorID, out ProgressBarRestartState? last)
                        && last.TargetLeftTime == targetLeftTime
                        && last.TotalSeconds == totalSeconds)
                    {
                        return false;
                    }

                    if (____progressCoroutine != null)
                    {
                        __instance.StopCoroutine(____progressCoroutine);
                    }

                    float currentLeftTime = ____possessionProgressbar.fillAmount * totalSeconds;

                    ProgressBarCoArgs[0] = targetLeftTime;
                    ProgressBarCoArgs[1] = currentLeftTime;
                    ProgressBarCoArgs[2] = totalSeconds;
                    IEnumerator routine = (IEnumerator)PossessionProgressbarCoMethod.Invoke(
                        __instance,
                        ProgressBarCoArgs);
                    ____progressCoroutine = __instance.StartCoroutine(routine);
                    ProgressBarRestartStates[__instance.ActorID] = new ProgressBarRestartState
                    {
                        TargetLeftTime = targetLeftTime,
                        TotalSeconds = totalSeconds,
                    };
                    return false;
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"UpdatePossessionProgressbar prefix failed — {ex.Message}");
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(UIPrefab_Spectator), "Start")]
        internal static class UIPrefabSpectatorStartPostfix
        {
            [HarmonyPostfix]
            internal static void Postfix(ref float ____possessionCooltime)
            {
                try
                {
                    if (!ModConfig.EnableMimicTuning.Value)
                    {
                        return;
                    }

                    ____possessionCooltime = MimicTuningResolver.GetCooltimeTotalSeconds();
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"Spectator Start postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(UIPrefab_Spectator), nameof(UIPrefab_Spectator.UpdatePossessionCooltime))]
        internal static class UIPrefabSpectatorUpdatePossessionCooltimePostfix
        {
            [HarmonyPostfix]
            internal static void Postfix(
                float inCooltime,
                Image ____possessionKeyCooltime,
                ref float ____possessionCooltime)
            {
                try
                {
                    if (!ModConfig.EnableMimicTuning.Value
                        || !MimicTuningResolver.ShouldScaleCooltime
                        || inCooltime <= 0f)
                    {
                        return;
                    }

                    if (____possessionKeyCooltime != null && ____possessionKeyCooltime.fillAmount >= 0.99f)
                    {
                        ____possessionCooltime = inCooltime * 0.001f;
                    }
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"UpdatePossessionCooltime postfix failed — {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(ProtoActor), nameof(ProtoActor.OnEndPossession))]
        internal static class ProtoActorOnEndPossessionPostfix
        {
            [HarmonyPostfix]
            internal static void Postfix(ProtoActor __instance)
            {
                try
                {
                    MimicTuningPossessionSessions.ClearSession(__instance.ActorID);
                    _ = ProgressBarRestartStates.Remove(__instance.ActorID);
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"OnEndPossession postfix failed — {ex.Message}");
                }
            }
        }

        private static IEnumerable<CodeInstruction> ReplaceConstIntLoad(
            IEnumerable<CodeInstruction> instructions,
            FieldInfo? constField,
            Func<int, CodeInstruction[]> insertAfterLoad)
        {
            if (constField == null)
            {
                ModLog.Warn(Feature, "Const field not found — transpiler skipped");
                return instructions;
            }

            List<CodeInstruction> codes = [.. instructions];
            int insertCount = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                if (!IsConstFieldLoad(codes[i], constField))
                {
                    continue;
                }

                CodeInstruction[] insert = insertAfterLoad(insertCount++);
                for (int j = 0; j < insert.Length; j++)
                {
                    codes.Insert(i + 1 + j, insert[j]);
                }

                i += insert.Length;
            }

            return codes;
        }

        private static bool IsConstFieldLoad(CodeInstruction instruction, FieldInfo? constField)
        {
            if (constField == null || instruction.opcode != OpCodes.Ldfld || instruction.operand is not FieldInfo field)
            {
                return false;
            }

            return ReferenceEquals(instruction.operand, constField)
                || (field.Name == constField.Name
                    && field.DeclaringType?.FullName == constField.DeclaringType?.FullName);
        }
    }
}
