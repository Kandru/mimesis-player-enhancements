using System.Reflection;
using System.Reflection.Emit;

namespace MimesisPlayerEnhancement.Features.LootMultiplicator.Patches
{
    [HarmonyPatch(typeof(DungeonRoom), "ManageSpawnData")]
    internal static class DungeonRoomManageSpawnDataTranspiler
    {
        private static readonly MethodInfo RollScaledBudgetMethod =
            AccessTools.Method(typeof(RandomMapLootBudgetScaler), nameof(RandomMapLootBudgetScaler.RollScaledBudget))
            ?? throw new InvalidOperationException("RandomMapLootBudgetScaler.RollScaledBudget not found");

        private static readonly MethodInfo SimpleRandNextMethod =
            AccessTools.Method(typeof(SimpleRandUtil), "Next", [typeof(int), typeof(int)])
            ?? throw new InvalidOperationException("SimpleRandUtil.Next not found");

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = [.. instructions];

            for (int i = 0; i < codes.Count; i++)
            {
                if (!IsCallTo(codes[i], SimpleRandNextMethod) || !IsMiscBudgetRoll(codes, i))
                {
                    continue;
                }

                int roomArgIndex = FindRoomArgInsertionIndex(codes, i);
                if (roomArgIndex < 0)
                {
                    continue;
                }

                codes.Insert(roomArgIndex, new CodeInstruction(OpCodes.Ldarg_0));
                codes[i + 1] = new CodeInstruction(OpCodes.Call, RollScaledBudgetMethod);
                break;
            }

            return codes;
        }

        private static bool IsCallTo(CodeInstruction instruction, MethodInfo expected)
        {
            return instruction.opcode == OpCodes.Call
                && instruction.operand is MethodInfo called
                && called.DeclaringType == expected.DeclaringType
                && called.Name == expected.Name
                && called.GetParameters().Length == expected.GetParameters().Length;
        }

        private static int FindRoomArgInsertionIndex(List<CodeInstruction> codes, int callIndex)
        {
            for (int j = callIndex - 1; j >= Math.Max(0, callIndex - 12); j--)
            {
                if (codes[j].opcode != OpCodes.Ldfld
                    || codes[j].operand is not FieldInfo field
                    || field.Name != "MiscMinVal"
                    || j < 2
                    || codes[j - 1].opcode != OpCodes.Ldfld
                    || codes[j - 2].opcode != OpCodes.Ldarg_0)
                {
                    continue;
                }

                return j - 2;
            }

            return -1;
        }

        private static bool IsMiscBudgetRoll(List<CodeInstruction> codes, int callIndex)
        {
            for (int j = Math.Max(0, callIndex - 12); j < callIndex; j++)
            {
                if (codes[j].opcode == OpCodes.Ldfld
                    && codes[j].operand is FieldInfo field
                    && field.Name == "MiscMaxVal")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
