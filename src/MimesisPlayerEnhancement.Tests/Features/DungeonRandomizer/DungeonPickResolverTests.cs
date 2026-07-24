using MimesisPlayerEnhancement.Features.DungeonRandomizer;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonRandomizer
{
    public sealed class DungeonPickResolverTests
    {
        private static DungeonRandomizerSceneConfig Config(
            string poolMode = "WidenVanilla",
            string allowlist = "",
            string blocklist = "") =>
            new(
                enableDungeonRandomizer: true,
                randomizeDungeonPick: true,
                dungeonPickPoolMode: poolMode,
                dungeonAllowlist: allowlist,
                dungeonBlocklist: blocklist,
                ignoreDungeonExcludeList: true,
                randomizeMapVariant: true,
                seedFlavor: DungeonSeedFlavor.Vanilla);

        private static int? PickFirst(IReadOnlyList<int> pool) => pool.Count > 0 ? pool[0] : null;

        [Fact]
        public void ResolvePick_keeps_eligible_vanilla_result_in_WidenVanilla_mode()
        {
            DungeonRandomizerSceneConfig config = Config();
            HashSet<int> allowlist = [];
            HashSet<int> blocklist = [];
            List<int> activePool = [10, 20, 30];

            int result = DungeonPickResolver.ResolvePick(
                vanillaResult: 20,
                excludeDungeonIds: [99],
                config,
                allowlist,
                blocklist,
                activePool,
                PickFirst);

            Assert.Equal(20, result);
        }

        [Fact]
        public void ResolvePick_replaces_ineligible_vanilla_result_in_WidenVanilla_mode()
        {
            DungeonRandomizerSceneConfig config = Config(blocklist: "20");
            HashSet<int> allowlist = [];
            HashSet<int> blocklist = DungeonIdListParser.Parse("20");
            List<int> activePool = [10, 20, 30];

            int result = DungeonPickResolver.ResolvePick(
                vanillaResult: 20,
                excludeDungeonIds: [],
                config,
                allowlist,
                blocklist,
                activePool,
                PickFirst,
                logResult: false);

            Assert.Equal(10, result);
        }

        [Fact]
        public void ResolvePick_always_picks_from_pool_in_AllActiveUniform_mode()
        {
            DungeonRandomizerSceneConfig config = Config(poolMode: "AllActiveUniform");
            HashSet<int> allowlist = [];
            HashSet<int> blocklist = [];
            List<int> activePool = [10, 20, 30];

            int result = DungeonPickResolver.ResolvePick(
                vanillaResult: 20,
                excludeDungeonIds: [],
                config,
                allowlist,
                blocklist,
                activePool,
                PickFirst,
                logResult: false);

            Assert.Equal(10, result);
        }
    }
}
