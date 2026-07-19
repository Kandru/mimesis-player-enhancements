using MimesisPlayerEnhancement.Features.DungeonRandomizer;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonRandomizer
{
    public sealed class DungeonRerollExcludePolicyTests
    {
        private static DungeonRandomizerSceneConfig Config(
            bool randomizeDungeonPick = true,
            bool ignoreDungeonExcludeList = true,
            string poolMode = "WidenVanilla") =>
            new(
                enableDungeonRandomizer: true,
                randomizeDungeonPick: randomizeDungeonPick,
                dungeonPickPoolMode: poolMode,
                dungeonAllowlist: "",
                dungeonBlocklist: "",
                ignoreDungeonExcludeList: ignoreDungeonExcludeList,
                randomizeMapVariant: true,
                seedFlavor: DungeonSeedFlavor.Vanilla);

        [Fact]
        public void ShouldIgnoreRerollExcludes_is_true_for_widen_vanilla_with_all_flags()
        {
            bool ignore = DungeonRerollExcludePolicy.ShouldIgnoreRerollExcludes(
                Config(),
                shouldApply: true);

            Assert.True(ignore);
        }

        [Theory]
        [InlineData(false, true, true, "WidenVanilla")]
        [InlineData(true, false, true, "WidenVanilla")]
        [InlineData(true, true, false, "WidenVanilla")]
        [InlineData(true, true, true, "AllActiveUniform")]
        public void ShouldIgnoreRerollExcludes_is_false_when_any_gate_fails(
            bool randomizeDungeonPick,
            bool ignoreDungeonExcludeList,
            bool shouldApply,
            string poolMode)
        {
            bool ignore = DungeonRerollExcludePolicy.ShouldIgnoreRerollExcludes(
                Config(randomizeDungeonPick, ignoreDungeonExcludeList, poolMode),
                shouldApply);

            Assert.False(ignore);
        }
    }
}
