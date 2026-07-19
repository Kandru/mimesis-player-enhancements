using System.Globalization;
using MimesisPlayerEnhancement;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonTime
{
    public sealed class DungeonTimeConfigBoundsTests
    {
        private const string SectionId = "MimesisPlayerEnhancement_DungeonTime";

        [Fact]
        public void DungeonTimeBaselinePlayerCount_has_minimum_one()
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, "DungeonTimeBaselinePlayerCount", out ModConfigEntryBound bound));
            Assert.Equal("1", bound.MinValue);
            Assert.Null(bound.MaxValue);
        }

        [Fact]
        public void ExtraShiftSecondsPerPlayerAboveBaseline_has_minimum_zero()
        {
            Assert.True(ModConfigEntryBounds.TryGet(
                SectionId,
                "ExtraShiftSecondsPerPlayerAboveBaseline",
                out ModConfigEntryBound bound));
            Assert.Equal(0f, float.Parse(bound.MinValue!, CultureInfo.InvariantCulture));
            Assert.Null(bound.MaxValue);
        }
    }
}
