using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.JoinAnytime
{
    public sealed class JoinAnytimeConfigBoundsTests
    {
        private const string SectionId = "MimesisPlayerEnhancement_JoinAnytime";

        [Fact]
        public void JoinConnectionGraceSeconds_has_minimum_one()
        {
            Assert.True(ModConfigEntryBounds.TryGet(
                SectionId,
                "JoinConnectionGraceSeconds",
                out ModConfigEntryBound bound));
            Assert.Equal("1", bound.MinValue);
            Assert.Null(bound.MaxValue);
        }
    }
}
