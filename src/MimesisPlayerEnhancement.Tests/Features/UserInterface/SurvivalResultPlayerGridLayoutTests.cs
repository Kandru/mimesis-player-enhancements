using MimesisPlayerEnhancement.Features.UserInterface.SurvivalResultPlayerList;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class SurvivalResultPlayerGridLayoutTests
    {
        [Theory]
        [InlineData(true, 5, true)]
        [InlineData(true, 4, false)]
        [InlineData(false, 8, false)]
        public void ShouldUseExtendedLayout_requires_more_players_and_count_above_vanilla(
            bool enableMorePlayers,
            int playerCount,
            bool expected)
        {
            object[] parameters = [1, true, playerCount];

            Assert.Equal(
                expected,
                SurvivalResultPlayerGridLayout.ShouldUseExtendedLayout(enableMorePlayers, parameters));
        }

        [Fact]
        public void FormatDayResultsTitle_formats_cycle_count()
        {
            Assert.Equal("DAY 3 RESULTS", SurvivalResultPlayerGridLayout.FormatDayResultsTitle(3));
        }

        [Theory]
        [InlineData(606f, 96f)]
        [InlineData(120f, 72f)]
        public void ComputeCellWidth_respects_minimum_column_width(float gridWidth, float expected)
        {
            Assert.Equal(expected, SurvivalResultPlayerGridLayout.ComputeCellWidth(gridWidth));
        }

        [Theory]
        [InlineData(1920f, 1f, 1728f)]
        [InlineData(1920f, 0f, 1728f)]
        public void ResolveGridWidthLocal_scales_by_screen_and_local_ratio(
            float screenWidth,
            float screenPerLocalX,
            float expected)
        {
            Assert.Equal(
                expected,
                SurvivalResultPlayerGridLayout.ResolveGridWidthLocal(screenWidth, screenPerLocalX),
                precision: 3);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(6, 1)]
        [InlineData(7, 2)]
        [InlineData(0, 0)]
        public void ComputeRowCount_ceil_divides_by_columns(int displayCount, int expectedRows)
        {
            Assert.Equal(expectedRows, SurvivalResultPlayerGridLayout.ComputeRowCount(displayCount));
        }

        [Fact]
        public void ResolveActualPlayerCount_uses_claimed_count_when_run_failed()
        {
            object[] parameters =
            [
                2,
                false,
                6,
                0,
                0,
                0,
            ];

            Assert.Equal(1, SurvivalResultPlayerGridLayout.ResolveActualPlayerCount(parameters, success: false, claimedCount: 6));
        }

        [Fact]
        public void ResolveActualPlayerCount_finds_largest_valid_success_layout()
        {
            object[] parameters =
            [
                1,
                true,
                2,
                0, 0, 0,
                1, 10, 20,
            ];

            Assert.Equal(1, SurvivalResultPlayerGridLayout.ResolveActualPlayerCount(parameters, success: true, claimedCount: 2));
        }
    }
}
