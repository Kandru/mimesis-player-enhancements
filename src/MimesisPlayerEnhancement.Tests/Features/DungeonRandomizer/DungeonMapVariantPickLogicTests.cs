using MimesisPlayerEnhancement.Features.DungeonRandomizer;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonRandomizer
{
    public sealed class DungeonMapVariantPickLogicTests
    {
        private static int? PickSecond(IReadOnlyList<int> mapIds) => mapIds[1];

        [Fact]
        public void Resolve_returns_null_when_randomization_disabled()
        {
            int? result = DungeonMapVariantPickLogic.Resolve(
                randomizeMapVariant: false,
                dungeonId: 1,
                mapIds: [10, 20, 30],
                vanillaMapId: 5,
                PickSecond);

            Assert.Null(result);
        }

        [Fact]
        public void Resolve_returns_null_when_no_map_ids()
        {
            int? result = DungeonMapVariantPickLogic.Resolve(
                randomizeMapVariant: true,
                dungeonId: 1,
                mapIds: [],
                vanillaMapId: 5,
                _ => null);

            Assert.Null(result);
        }

        [Fact]
        public void Resolve_returns_picked_map_when_enabled()
        {
            int? result = DungeonMapVariantPickLogic.Resolve(
                randomizeMapVariant: true,
                dungeonId: 7,
                mapIds: [10, 20, 30],
                vanillaMapId: 5,
                PickSecond);

            Assert.Equal(20, result);
        }
    }
}
