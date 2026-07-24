using MimesisPlayerEnhancement.Features.Statistics;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.Statistics
{
    public sealed class StatisticsArchiveIdentityTests
    {
        [Theory]
        [InlineData(0, true, null, true)]
        [InlineData(0, true, "", true)]
        [InlineData(12, false, null, true)]
        [InlineData(12, false, "", true)]
        [InlineData(0, false, "voice-1", true)]
        [InlineData(0, false, null, false)]
        [InlineData(0, false, "", false)]
        [InlineData(0, false, "   ", true)]
        public void IsIdentityReady_matches_archive_field_rules(
            long playerUid,
            bool isLocal,
            string? playerId,
            bool expected)
        {
            Assert.Equal(expected, StatisticsArchiveIdentity.IsIdentityReady(playerUid, isLocal, playerId));
        }
    }
}
