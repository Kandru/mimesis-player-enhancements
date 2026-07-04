using System;
using System.Reflection;
using System.Reflection.Emit;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    [HarmonyPatch]
    internal static class SteamInviteDispatcherSetLobbyPublicCoroutineTranspiler
    {
        private static readonly MethodInfo CoercePublicRoomWriteFlagMethod =
            AccessTools.Method(
                typeof(JoinAnytimePublicLobbyTools),
                nameof(JoinAnytimePublicLobbyTools.CoercePublicRoomWriteFlag));

        private static MethodBase? TargetMethod() =>
            HarmonyPatchHelper.GetEnumeratorMoveNext(
                typeof(SteamInviteDispatcher),
                "SetLobbyPublicCoroutine",
                [typeof(bool)]);

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase original)
        {
            FieldInfo? isPublicField = ResolveIsPublicField(original.DeclaringType);
            if (isPublicField == null)
            {
                return instructions;
            }

            List<CodeInstruction> codes = [.. instructions];
            MethodInfo? toStringMethod = AccessTools.Method(typeof(bool), nameof(bool.ToString), []);

            for (int i = 1; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Call || codes[i].operand is not MethodInfo calledMethod)
                {
                    continue;
                }

                if (calledMethod != toStringMethod)
                {
                    continue;
                }

                OpCode loadLocalOpcode = codes[i - 1].opcode;
                if (loadLocalOpcode != OpCodes.Ldloc && loadLocalOpcode != OpCodes.Ldloc_0
                    && loadLocalOpcode != OpCodes.Ldloc_1 && loadLocalOpcode != OpCodes.Ldloc_2
                    && loadLocalOpcode != OpCodes.Ldloc_3 && loadLocalOpcode != OpCodes.Ldloc_S)
                {
                    continue;
                }

                CodeInstruction loadFlag = codes[i - 1];
                codes[i - 1] = new CodeInstruction(OpCodes.Ldarg_0);
                codes.Insert(i, new CodeInstruction(OpCodes.Ldfld, isPublicField));
                codes.Insert(i + 1, loadFlag);
                codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, CoercePublicRoomWriteFlagMethod));
                break;
            }

            return codes;
        }

        private static FieldInfo? ResolveIsPublicField(Type? stateMachineType)
        {
            if (stateMachineType == null)
            {
                return null;
            }

            FieldInfo? field = AccessTools.Field(stateMachineType, "isPublic");
            if (field != null)
            {
                return field;
            }

            return AccessTools.Field(stateMachineType, "<>3__isPublic");
        }
    }
}
