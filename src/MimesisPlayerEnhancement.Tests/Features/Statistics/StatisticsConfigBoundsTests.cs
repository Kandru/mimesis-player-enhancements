using MimesisPlayerEnhancement;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class StatisticsConfigBoundsTests
    {
        private const string SectionId = "MimesisPlayerEnhancement_Statistics";

        [Fact]
        public void SessionReconnectGraceMinutes_has_minimum_one()
        {
            Assert.True(ModConfigEntryBounds.TryGet(SectionId, "SessionReconnectGraceMinutes", out ModConfigEntryBound bound));
            Assert.Equal("1", bound.MinValue);
            Assert.Null(bound.MaxValue);
        }
    }
}
