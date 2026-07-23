using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MimicTuning.Patches
{
    internal static class MimicPossessionPatchSupport
    {
        internal const string Feature = "MimicTuning";
        internal const float CooltimeFillCompleteThreshold = 0.99f;

        internal static readonly FieldInfo? PossessionDurationField =
            AccessTools.Field(typeof(Bifrost.ConstEnum.DataConsts), nameof(Bifrost.ConstEnum.DataConsts.C_PossessionDuration));

        internal static readonly FieldInfo? PossessionCooltimeField =
            AccessTools.Field(typeof(Bifrost.ConstEnum.DataConsts), nameof(Bifrost.ConstEnum.DataConsts.C_PossessionCooltime));

        internal static readonly MethodInfo? PossessionProgressbarCoMethod =
            AccessTools.Method(typeof(ProtoActor), "PossessionProgressbarCo");

        internal delegate IEnumerator PossessionProgressbarCoDelegate(
            ProtoActor instance,
            float targetLeftTime,
            float currentLeftTime,
            float possessionDurationTime);

        internal static readonly PossessionProgressbarCoDelegate? RunPossessionProgressbarCo =
            PossessionProgressbarCoMethod != null
                ? (PossessionProgressbarCoDelegate)Delegate.CreateDelegate(
                    typeof(PossessionProgressbarCoDelegate),
                    PossessionProgressbarCoMethod)
                : null;

        internal static readonly MethodInfo RollPossessionDurationMsMethod =
            AccessTools.Method(
                typeof(MimicPossessionResolver),
                nameof(MimicPossessionResolver.RollPossessionDurationMs),
                [typeof(long), typeof(int)]);

        internal static readonly MethodInfo ScalePossessionCooltimeMsMethod =
            AccessTools.Method(
                typeof(MimicPossessionResolver),
                nameof(MimicPossessionResolver.ScalePossessionCooltimeMs),
                [typeof(long)]);

        internal static readonly Dictionary<int, ProgressBarRestartState> ProgressBarRestartStates = [];

        internal sealed class ProgressBarRestartState
        {
            internal float TargetLeftTime;
            internal float TotalSeconds;
        }

        internal static void ClearProgressBarRestartStates()
        {
            ProgressBarRestartStates.Clear();
        }

        internal static IEnumerable<CodeInstruction> ReplaceConstIntLoad(
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

    [HarmonyPatch(typeof(PossessionController), nameof(PossessionController.HandleStartPossessing))]
    internal static class HandleStartPossessingTranspiler
    {
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            MimicPossessionPatchSupport.ReplaceConstIntLoad(
                instructions,
                MimicPossessionPatchSupport.PossessionDurationField,
                _ =>
                [
                    new CodeInstruction(OpCodes.Ldarg_1),
                    new CodeInstruction(OpCodes.Call, MimicPossessionPatchSupport.RollPossessionDurationMsMethod),
                ]);
    }

    [HarmonyPatch(typeof(PossessionController), "ClearPossessingStateInternal")]
    internal static class ClearPossessingStateInternalTranspiler
    {
        [HarmonyTranspiler]
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            MimicPossessionPatchSupport.ReplaceConstIntLoad(
                instructions,
                MimicPossessionPatchSupport.PossessionCooltimeField,
                _ =>
                [
                    new CodeInstruction(OpCodes.Call, MimicPossessionPatchSupport.ScalePossessionCooltimeMsMethod),
                ]);
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
            if (!MimicPossessionResolver.ShouldRandomizeDuration || MimicPossessionPatchSupport.RunPossessionProgressbarCo == null)
            {
                return true;
            }

            try
            {
                float totalSeconds = MimicPossessionResolver.GetProgressBarTotalSeconds(
                    __instance.ActorID,
                    inServerLeftTime);
                float targetLeftTime = inServerLeftTime * 0.001f;

                if (____progressCoroutine != null
                    && MimicPossessionPatchSupport.ProgressBarRestartStates.TryGetValue(
                        __instance.ActorID,
                        out MimicPossessionPatchSupport.ProgressBarRestartState? last)
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

                IEnumerator routine = MimicPossessionPatchSupport.RunPossessionProgressbarCo(
                    __instance,
                    targetLeftTime,
                    currentLeftTime,
                    totalSeconds);
                ____progressCoroutine = __instance.StartCoroutine(routine);
                MimicPossessionPatchSupport.ProgressBarRestartStates[__instance.ActorID] =
                    new MimicPossessionPatchSupport.ProgressBarRestartState
                    {
                        TargetLeftTime = targetLeftTime,
                        TotalSeconds = totalSeconds,
                    };
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(MimicPossessionPatchSupport.Feature, $"UpdatePossessionProgressbar prefix failed — {ex.Message}");
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
                if (!MimicPossessionResolver.ShouldApplyHost)
                {
                    return;
                }

                ____possessionCooltime = MimicPossessionResolver.GetCooltimeTotalSeconds();
            }
            catch (Exception ex)
            {
                ModLog.Warn(MimicPossessionPatchSupport.Feature, $"Spectator Start postfix failed — {ex.Message}");
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
                if (!MimicPossessionResolver.ShouldScaleCooltime
                    || inCooltime <= 0f)
                {
                    return;
                }

                if (____possessionKeyCooltime != null
                    && ____possessionKeyCooltime.fillAmount >= MimicPossessionPatchSupport.CooltimeFillCompleteThreshold)
                {
                    ____possessionCooltime = inCooltime * 0.001f;
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(MimicPossessionPatchSupport.Feature, $"UpdatePossessionCooltime postfix failed — {ex.Message}");
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
                MimicPossessionSessions.ClearSession(__instance.ActorID);
                _ = MimicPossessionPatchSupport.ProgressBarRestartStates.Remove(__instance.ActorID);
            }
            catch (Exception ex)
            {
                ModLog.Warn(MimicPossessionPatchSupport.Feature, $"OnEndPossession postfix failed — {ex.Message}");
            }
        }
    }
}
