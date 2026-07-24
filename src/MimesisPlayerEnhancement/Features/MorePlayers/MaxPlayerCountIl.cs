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

        private static readonly HashSet<OpCode> StoreLocalOpcodes =
        [
            OpCodes.Stloc, OpCodes.Stloc_S,
            OpCodes.Stloc_0, OpCodes.Stloc_1, OpCodes.Stloc_2, OpCodes.Stloc_3,
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

        /// <summary>
        /// Rewrites vanilla player-cap literal <c>4</c> used in comparisons or local stores
        /// (e.g. <c>if (n &gt; 4) n = 4;</c>).
        /// </summary>
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

                if (i + 1 >= codes.Count)
                {
                    continue;
                }

                OpCode next = codes[i + 1].opcode;
                if (!ComparisonBranchOpcodes.Contains(next) && !StoreLocalOpcodes.Contains(next))
                {
                    continue;
                }

                codes[i] = new CodeInstruction(OpCodes.Call, getMaxPlayersMethod);
            }

            return codes;
        }

        /// <summary>Rewrites every <c>ldc.i4.4</c> (safe only when that is the sole player-cap literal).</summary>
        internal static IEnumerable<CodeInstruction> ReplaceAllPlayerCapLiteralFour(
            IEnumerable<CodeInstruction> instructions,
            MethodInfo getMaxPlayersMethod)
        {
            List<CodeInstruction> codes = [.. instructions];
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_4)
                {
                    codes[i] = new CodeInstruction(OpCodes.Call, getMaxPlayersMethod);
                }
            }

            return codes;
        }
    }
}
