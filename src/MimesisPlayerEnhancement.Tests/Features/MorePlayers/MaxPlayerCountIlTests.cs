using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MimesisPlayerEnhancement.Features.MorePlayers;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.MorePlayers
{
    public sealed class MaxPlayerCountIlTests
    {
        private static readonly FieldInfo MaxPlayerCountField =
            typeof(TestFields).GetField(nameof(TestFields.C_MaxPlayerCount), BindingFlags.Public | BindingFlags.Static)!;

        private static readonly MethodInfo GetMaxPlayersMethod =
            typeof(MorePlayersPatchHelpers).GetMethod(nameof(MorePlayersPatchHelpers.GetMaxPlayers), Type.EmptyTypes)!;

        [Fact]
        public void ReplaceConstMaxPlayerCount_swaps_ldfld_with_call()
        {
            var instructions = new List<CodeInstruction>
            {
                new(OpCodes.Ldfld, MaxPlayerCountField),
                new(OpCodes.Ret),
            };

            List<CodeInstruction> result = MaxPlayerCountIl
                .ReplaceConstMaxPlayerCount(instructions, GetMaxPlayersMethod)
                .ToList();

            Assert.Equal(OpCodes.Pop, result[0].opcode);
            Assert.Equal(OpCodes.Call, result[1].opcode);
            Assert.Same(GetMaxPlayersMethod, result[1].operand);
        }

        [Fact]
        public void ReplaceConstMaxPlayerCount_leaves_unrelated_ldfld_unchanged()
        {
            FieldInfo otherField = typeof(TestFields).GetField(nameof(TestFields.OtherField), BindingFlags.Public | BindingFlags.Static)!;
            var instructions = new List<CodeInstruction>
            {
                new(OpCodes.Ldfld, otherField),
            };

            List<CodeInstruction> result = MaxPlayerCountIl
                .ReplaceConstMaxPlayerCount(instructions, GetMaxPlayersMethod)
                .ToList();

            Assert.Equal(OpCodes.Ldfld, result[0].opcode);
            Assert.Same(otherField, result[0].operand);
        }

        [Fact]
        public void ReplacePlayerCapLiteralFour_swaps_ldc_i4_4_before_branch()
        {
            var instructions = new List<CodeInstruction>
            {
                new(OpCodes.Ldc_I4_4),
                new(OpCodes.Blt_S, (object)0),
            };

            List<CodeInstruction> result = MaxPlayerCountIl
                .ReplacePlayerCapLiteralFour(instructions, GetMaxPlayersMethod)
                .ToList();

            Assert.Equal(OpCodes.Call, result[0].opcode);
            Assert.Same(GetMaxPlayersMethod, result[0].operand);
            Assert.Equal(OpCodes.Blt_S, result[1].opcode);
        }

        [Fact]
        public void ReplacePlayerCapLiteralFour_leaves_ldc_i4_4_without_following_branch_or_store()
        {
            var instructions = new List<CodeInstruction>
            {
                new(OpCodes.Ldc_I4_4),
                new(OpCodes.Ret),
            };

            List<CodeInstruction> result = MaxPlayerCountIl
                .ReplacePlayerCapLiteralFour(instructions, GetMaxPlayersMethod)
                .ToList();

            Assert.Equal(OpCodes.Ldc_I4_4, result[0].opcode);
        }

        [Fact]
        public void ReplacePlayerCapLiteralFour_swaps_ldc_i4_4_before_store()
        {
            var instructions = new List<CodeInstruction>
            {
                new(OpCodes.Ldc_I4_4),
                new(OpCodes.Stloc_0),
            };

            List<CodeInstruction> result = MaxPlayerCountIl
                .ReplacePlayerCapLiteralFour(instructions, GetMaxPlayersMethod)
                .ToList();

            Assert.Equal(OpCodes.Call, result[0].opcode);
            Assert.Same(GetMaxPlayersMethod, result[0].operand);
            Assert.Equal(OpCodes.Stloc_0, result[1].opcode);
        }

        [Fact]
        public void ReplaceAllPlayerCapLiteralFour_swaps_every_ldc_i4_4()
        {
            var instructions = new List<CodeInstruction>
            {
                new(OpCodes.Ldc_I4_4),
                new(OpCodes.Call, GetMaxPlayersMethod),
                new(OpCodes.Ldc_I4_4),
                new(OpCodes.Ret),
            };

            List<CodeInstruction> result = MaxPlayerCountIl
                .ReplaceAllPlayerCapLiteralFour(instructions, GetMaxPlayersMethod)
                .ToList();

            Assert.Equal(OpCodes.Call, result[0].opcode);
            Assert.Same(GetMaxPlayersMethod, result[0].operand);
            Assert.Equal(OpCodes.Call, result[2].opcode);
            Assert.Same(GetMaxPlayersMethod, result[2].operand);
        }

        private static class TestFields
        {
            public static int C_MaxPlayerCount = 0;
            public static int OtherField = 0;
        }
    }
}
