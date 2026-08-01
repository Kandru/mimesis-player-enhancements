using MimesisPlayerEnhancement.Features.DungeonTime;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.DungeonTime
{
    public sealed class DungeonTimeTramClockSyncTests
    {
        [Theory]
        [InlineData(0, 0, 0, 0, 0, 0)]
        [InlineData(14, 37, 12, 14, 0, 0)]
        [InlineData(9, 0, 59, 9, 0, 0)]
        [InlineData(23, 59, 59, 23, 0, 0)]
        [InlineData(0, 1, 0, 0, 0, 0)]
        public void FloorToDisplayHour_zeros_minutes_and_seconds(
            int hours,
            int minutes,
            int seconds,
            int expectedHours,
            int expectedMinutes,
            int expectedSeconds)
        {
            TimeSpan floored = DungeonTimeTramClockSync.FloorToDisplayHour(
                new TimeSpan(hours, minutes, seconds));

            Assert.Equal(expectedHours, floored.Hours);
            Assert.Equal(expectedMinutes, floored.Minutes);
            Assert.Equal(expectedSeconds, floored.Seconds);
            Assert.Equal(0, floored.Milliseconds);
        }

        [Fact]
        public void FloorToDisplayHour_preserves_days()
        {
            TimeSpan floored = DungeonTimeTramClockSync.FloorToDisplayHour(
                new TimeSpan(days: 1, hours: 3, minutes: 45, seconds: 20));

            Assert.Equal(1, floored.Days);
            Assert.Equal(3, floored.Hours);
            Assert.Equal(0, floored.Minutes);
            Assert.Equal(0, floored.Seconds);
        }
    }
}
