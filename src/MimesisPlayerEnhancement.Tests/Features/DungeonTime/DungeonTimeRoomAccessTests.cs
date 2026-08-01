using MimesisPlayerEnhancement.Features.DungeonTime;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonTime
{
    public sealed class DungeonTimeRoomAccessTests
    {
        [Fact]
        public void ParseDisplayTimeToSeconds_parses_hh_mm_ss()
        {
            long seconds = DungeonTimeRoomAccess.ParseDisplayTimeToSeconds("10:00:00");

            Assert.Equal(36_000L, seconds);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-time")]
        public void ParseDisplayTimeToSeconds_returns_zero_for_invalid_input(string? displayTime)
        {
            long seconds = DungeonTimeRoomAccess.ParseDisplayTimeToSeconds(displayTime!);

            Assert.Equal(0L, seconds);
        }
    }
}
