using MimesisPlayerEnhancement.Features.SavegamePreparation;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SavegamePreparation
{
    public sealed class SavegamePreparationResolverTests
    {
        private const int ConfigStartingZoneMax = 99;

        [Theory]
        [InlineData(1, 10, false, 1)]
        [InlineData(0, 10, false, 1)]
        [InlineData(5, 10, false, 5)]
        [InlineData(12, 10, false, 10)]
        [InlineData(16, 7, false, 7)]
        [InlineData(16, 7, true, 16)]
        [InlineData(100, 7, true, 99)]
        [InlineData(0, 7, true, 1)]
        [InlineData(50, 7, true, 50)]
        public void ClampStartingZone_respects_override_cap(
            int requested,
            int maxStage,
            bool overrideMaxStageCount,
            int expected)
        {
            int cap = overrideMaxStageCount ? ConfigStartingZoneMax : maxStage;
            Assert.Equal(expected, SavegamePreparationResolver.ClampStartingZone(requested, cap));
        }

        [Theory]
        [InlineData(false, false, false, false, new int[0])]
        [InlineData(true, false, false, false, new[] { 1 })]
        [InlineData(false, true, false, false, new[] { 2 })]
        [InlineData(false, false, true, false, new[] { 3 })]
        [InlineData(false, false, false, true, new[] { 5 })]
        [InlineData(true, true, false, true, new[] { 1, 2, 5 })]
        [InlineData(true, true, true, true, new[] { 1, 2, 3, 5 })]
        public void MapStartupTramUpgradeIds_maps_flags_to_master_ids(
            bool horn,
            bool scrapScanner,
            bool booster,
            bool light,
            int[] expected)
        {
            List<int> ids = SavegamePreparationResolver.MapStartupTramUpgradeIds(
                horn,
                scrapScanner,
                booster,
                light);

            Assert.Equal(expected, ids);
            Assert.DoesNotContain(4, ids);
            Assert.DoesNotContain(6, ids);
        }

        [Fact]
        public void SavegamePreparation_section_has_no_feature_toggle_key()
        {
            Assert.False(ModConfigRegistry.TryGetFeatureToggleKey(
                SavegamePreparationConfig.SectionId,
                out _));
        }
    }
}
