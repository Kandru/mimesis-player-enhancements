using MimesisPlayerEnhancement.Features.JoinAnytime;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.JoinAnytime
{
    public sealed class JoinAnytimeLobbyDisplayTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(-1, 0)]
        [InlineData(1, 1)]
        [InlineData(2, 2)]
        [InlineData(3, 3)]
        [InlineData(4, 3)]
        [InlineData(8, 3)]
        public void GetBrowsePlayerCount_caps_at_three(int realCount, int expected)
        {
            int actual = JoinAnytimeLobbyDisplay.GetBrowsePlayerCount(realCount);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(0, "0/4")]
        [InlineData(1, "1/4")]
        [InlineData(3, "3/4")]
        [InlineData(5, "3/4")]
        public void FormatBrowsePlayerCount_uses_vanilla_denominator(int realCount, string expected)
        {
            string actual = JoinAnytimeLobbyDisplay.FormatBrowsePlayerCount(realCount);

            Assert.Equal(expected, actual);
        }
    }
}
