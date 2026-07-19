using MimesisPlayerEnhancement.Features.DungeonRandomizer;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonRandomizer
{
    public sealed class DungeonPickLogicTests
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
        public void Resolve_keeps_eligible_vanilla_result_in_WidenVanilla_mode()
        {
            DungeonRandomizerSceneConfig config = Config();
            HashSet<int> allowlist = [];
            HashSet<int> blocklist = [];
            List<int> activePool = [10, 20, 30];

            int result = DungeonPickLogic.Resolve(
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
        public void Resolve_replaces_ineligible_vanilla_result_in_WidenVanilla_mode()
        {
            DungeonRandomizerSceneConfig config = Config(blocklist: "20");
            HashSet<int> allowlist = [];
            HashSet<int> blocklist = DungeonIdListParser.Parse("20");
            List<int> activePool = [10, 20, 30];

            int result = DungeonPickLogic.Resolve(
                vanillaResult: 20,
                excludeDungeonIds: [],
                config,
                allowlist,
                blocklist,
                activePool,
                PickFirst);

            Assert.Equal(10, result);
        }

        [Fact]
        public void Resolve_always_picks_from_pool_in_AllActiveUniform_mode()
        {
            DungeonRandomizerSceneConfig config = Config(poolMode: "AllActiveUniform");
            HashSet<int> allowlist = [];
            HashSet<int> blocklist = [];
            List<int> activePool = [10, 20, 30];

            int result = DungeonPickLogic.Resolve(
                vanillaResult: 20,
                excludeDungeonIds: [],
                config,
                allowlist,
                blocklist,
                activePool,
                PickFirst);

            Assert.Equal(10, result);
        }

        [Fact]
        public void Resolve_uses_filtered_active_pool_when_replacing_vanilla()
        {
            DungeonRandomizerSceneConfig config = Config(allowlist: "30");
            HashSet<int> allowlist = DungeonIdListParser.Parse("30");
            HashSet<int> blocklist = [];
            List<int> activePool = [30];

            int result = DungeonPickLogic.Resolve(
                vanillaResult: 10,
                excludeDungeonIds: [],
                config,
                allowlist,
                blocklist,
                activePool,
                PickFirst);

            Assert.Equal(30, result);
        }

        [Fact]
        public void Resolve_falls_back_to_full_pool_when_excludes_empty_eligible_set()
        {
            DungeonRandomizerSceneConfig config = Config(poolMode: "AllActiveUniform");
            HashSet<int> allowlist = [];
            HashSet<int> blocklist = [];
            List<int> activePool = [10, 20, 30];

            int result = DungeonPickLogic.Resolve(
                vanillaResult: 99,
                excludeDungeonIds: [10, 20, 30],
                config,
                allowlist,
                blocklist,
                activePool,
                PickFirst);

            Assert.Equal(10, result);
        }

        [Fact]
        public void Resolve_keeps_vanilla_when_pool_is_empty()
        {
            DungeonRandomizerSceneConfig config = Config(poolMode: "AllActiveUniform");
            HashSet<int> allowlist = [];
            HashSet<int> blocklist = [];
            List<int> activePool = [];

            int result = DungeonPickLogic.Resolve(
                vanillaResult: 42,
                excludeDungeonIds: [],
                config,
                allowlist,
                blocklist,
                activePool,
                _ => null);

            Assert.Equal(42, result);
        }

        [Theory]
        [InlineData(5, "", "", true)]
        [InlineData(0, "", "", false)]
        [InlineData(5, "5", "", true)]
        [InlineData(5, "6", "", false)]
        [InlineData(5, "", "5", false)]
        public void IsEligible_applies_allowlist_and_blocklist(
            int dungeonId,
            string allowlistCsv,
            string blocklistCsv,
            bool expected)
        {
            bool eligible = DungeonPickLogic.IsEligible(
                dungeonId,
                DungeonIdListParser.Parse(allowlistCsv),
                DungeonIdListParser.Parse(blocklistCsv));

            Assert.Equal(expected, eligible);
        }
    }
}
