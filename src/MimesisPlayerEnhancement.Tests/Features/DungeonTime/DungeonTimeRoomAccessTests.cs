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
        [InlineData("24:00:00", 86400L)]
        [InlineData("24:00", 86400L)]
        [InlineData("00:00:00", 0L)]
        [InlineData("23:59:59", 86399L)]
        public void ParseDisplayTimeToSeconds_maps_twenty_four_to_end_of_day(string displayTime, long expected)
        {
            long seconds = DungeonTimeRoomAccess.ParseDisplayTimeToSeconds(displayTime);

            Assert.Equal(expected, seconds);
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
