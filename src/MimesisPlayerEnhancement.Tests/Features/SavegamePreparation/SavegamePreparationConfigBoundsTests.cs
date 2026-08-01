using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.SavegamePreparation
{
    public sealed class SavegamePreparationConfigBoundsTests
    {
        private const string SectionId = "MimesisPlayerEnhancement_SavegamePreparation";

        [Fact]
        public void StartupMoney_has_minimum_zero()
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, "StartupMoney", out ModConfigEntryBound bound));
            Assert.Equal("0", bound.MinValue);
            Assert.Null(bound.MaxValue);
        }

        [Fact]
        public void StartingZone_is_bounded_between_1_and_99()
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, "StartingZone", out ModConfigEntryBound bound));
            Assert.Equal("1", bound.MinValue);
            Assert.Equal("99", bound.MaxValue);
        }
    }
}
