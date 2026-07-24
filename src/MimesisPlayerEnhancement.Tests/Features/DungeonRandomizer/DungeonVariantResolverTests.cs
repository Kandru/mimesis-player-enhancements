using MimesisPlayerEnhancement.Features.DungeonRandomizer;
using MimesisPlayerEnhancement.Util;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonRandomizer
{
    public sealed class DungeonVariantResolverTests
    {
        private static DungeonRandomizerSceneConfig Config(bool randomizeMapVariant) =>
            new(
                enableDungeonRandomizer: true,
                randomizeDungeonPick: true,
                dungeonPickPoolMode: "WidenVanilla",
                dungeonAllowlist: "",
                dungeonBlocklist: "",
                ignoreDungeonExcludeList: true,
                randomizeMapVariant: randomizeMapVariant,
                seedFlavor: DungeonSeedFlavor.Vanilla);

        private static int? PickSecond(IReadOnlyList<int> mapIds) => mapIds[1];

        [Fact]
        public void ResolveMapId_returns_null_when_randomization_disabled()
        {
            DungeonRandomizerSceneConfig config = Config(randomizeMapVariant: false);

            int? result = DungeonVariantResolver.ResolveMapId(
                config,
                dungeonId: 1,
                mapIds: [10, 20, 30],
                vanillaMapId: 5,
                PickSecond);

            Assert.Null(result);
        }

        [Fact]
        public void ResolveMapId_returns_picked_map_when_enabled()
        {
            DungeonRandomizerSceneConfig config = Config(randomizeMapVariant: true);

            int? result = DungeonVariantResolver.ResolveMapId(
                config,
                dungeonId: 7,
                mapIds: [10, 20, 30],
                vanillaMapId: 5,
                PickSecond,
                logResult: false);

            Assert.Equal(20, result);
        }
    }
}
