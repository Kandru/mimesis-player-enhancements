using System.Reflection;
using System.Reflection.Emit;

namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    internal static class MaxPlayerCountIl
    {
        private static readonly HashSet<OpCode> ComparisonBranchOpcodes =
        [
            OpCodes.Blt, OpCodes.Blt_S, OpCodes.Blt_Un, OpCodes.Blt_Un_S,
            OpCodes.Bgt, OpCodes.Bgt_S, OpCodes.Bgt_Un, OpCodes.Bgt_Un_S,
            OpCodes.Bge, OpCodes.Bge_S, OpCodes.Bge_Un, OpCodes.Bge_Un_S,
            OpCodes.Ble, OpCodes.Ble_S, OpCodes.Ble_Un, OpCodes.Ble_Un_S,
            OpCodes.Clt, OpCodes.Clt_Un,
            OpCodes.Cgt, OpCodes.Cgt_Un,
        ];

        internal static IEnumerable<CodeInstruction> ReplaceConstMaxPlayerCount(
            IEnumerable<CodeInstruction> instructions,
            MethodInfo getMaxPlayersMethod)
        {
            List<CodeInstruction> codes = [.. instructions];
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Ldfld
                    || codes[i].operand is not FieldInfo field
                    || field.Name != "C_MaxPlayerCount")
                {
                    continue;
                }

                codes[i] = new CodeInstruction(OpCodes.Pop);
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, getMaxPlayersMethod));
                i++;
            }

            return codes;
        }

        internal static IEnumerable<CodeInstruction> ReplacePlayerCapLiteralFour(
            IEnumerable<CodeInstruction> instructions,
            MethodInfo getMaxPlayersMethod)
        {
            List<CodeInstruction> codes = [.. instructions];
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode != OpCodes.Ldc_I4_4)
                {
                    continue;
                }

                if (i + 1 >= codes.Count || !ComparisonBranchOpcodes.Contains(codes[i + 1].opcode))
                {
                    continue;
                }

                codes[i] = new CodeInstruction(OpCodes.Call, getMaxPlayersMethod);
            }

            return codes;
        }
    }
}
