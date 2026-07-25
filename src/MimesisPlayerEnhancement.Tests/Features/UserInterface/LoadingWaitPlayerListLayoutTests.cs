using MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class LoadingWaitPlayerListLayoutTests
    {
        [Fact]
        public void BuildGreedyRows_wraps_when_row_exceeds_available_width()
        {
            List<LoadingWaitPlayerEntry> players =
            [
                new() { DisplayName = "Player One" },
                new() { DisplayName = "Player Two" },
                new() { DisplayName = "Player Three" },
                new() { DisplayName = "Player Four" },
            ];

            List<LoadingWaitLayoutRow> rows = LoadingWaitPlayerListLayout.BuildGreedyRows(
                measureText: null,
                players,
                availableWidth: 140f,
                fontSize: 18f);

            Assert.True(rows.Count > 1);
            int covered = 0;
            foreach (LoadingWaitLayoutRow row in rows)
            {
                float rowWidth = LoadingWaitPlayerListLayout.MeasureRowWidth(
                    null,
                    players,
                    row.StartIndex,
                    row.Count,
                    fontSize: 18f);
                Assert.True(rowWidth <= 140f);
                covered += row.Count;
            }

            Assert.Equal(players.Count, covered);
        }

        [Fact]
        public void Resolve_uses_larger_font_for_few_players()
        {
            List<LoadingWaitPlayerEntry> players =
            [
                new() { DisplayName = "Alpha" },
                new() { DisplayName = "Bravo" },
            ];

            LoadingWaitLayoutMetrics few = LoadingWaitPlayerListLayout.Resolve(
                measureText: null,
                players,
                availableWidth: 1600f,
                bandHeight: 200f,
                templateFontSize: 18f,
                rowGap: 8f);

            LoadingWaitLayoutMetrics many = LoadingWaitPlayerListLayout.Resolve(
                measureText: null,
                Enumerable.Range(1, 10).Select(index => new LoadingWaitPlayerEntry
                {
                    DisplayName = $"Player {index:00}",
                }).ToList(),
                availableWidth: 1600f,
                bandHeight: 200f,
                templateFontSize: 18f,
                rowGap: 8f);

            Assert.True(few.FontSize > many.FontSize);
            Assert.Single(few.Rows);
        }

        [Fact]
        public void Resolve_never_exceeds_max_font_size()
        {
            LoadingWaitLayoutMetrics metrics = LoadingWaitPlayerListLayout.Resolve(
                measureText: null,
                [new() { DisplayName = "A" }],
                availableWidth: 4000f,
                bandHeight: 400f,
                templateFontSize: 40f,
                rowGap: 8f);

            Assert.True(metrics.FontSize <= LoadingWaitPlayerListLayout.MaxFontSize);
            Assert.Equal(LoadingWaitPlayerListLayout.MaxFontSize, metrics.FontSize);
        }

        [Fact]
        public void Resolve_keeps_multiple_rows_instead_of_collapsing_to_one_line()
        {
            List<LoadingWaitPlayerEntry> players = Enumerable.Range(1, 8)
                .Select(index => new LoadingWaitPlayerEntry { DisplayName = $"LongPlayerName{index:00}" })
                .ToList();

            LoadingWaitLayoutMetrics metrics = LoadingWaitPlayerListLayout.Resolve(
                measureText: null,
                players,
                availableWidth: 420f,
                bandHeight: 220f,
                templateFontSize: 20f,
                rowGap: 10f);

            Assert.True(metrics.Rows.Count > 1);
            Assert.True(metrics.FontSize <= LoadingWaitPlayerListLayout.MaxFontSize);
            Assert.True(metrics.FontSize >= LoadingWaitPlayerListLayout.MinFontSize);
        }

        [Fact]
        public void Resolve_wraps_to_multiple_rows_when_width_is_tight()
        {
            List<LoadingWaitPlayerEntry> players = Enumerable.Range(1, 6)
                .Select(index => new LoadingWaitPlayerEntry { DisplayName = $"Player {index:00}" })
                .ToList();

            LoadingWaitLayoutMetrics metrics = LoadingWaitPlayerListLayout.Resolve(
                measureText: null,
                players,
                availableWidth: 320f,
                bandHeight: 200f,
                templateFontSize: 18f,
                rowGap: 8f);

            Assert.True(metrics.Rows.Count > 1);
        }

        [Fact]
        public void Resolve_preserves_player_order_in_rows()
        {
            List<LoadingWaitPlayerEntry> players =
            [
                new() { DisplayName = "One" },
                new() { DisplayName = "Two" },
                new() { DisplayName = "Three" },
                new() { DisplayName = "Four" },
                new() { DisplayName = "Five" },
            ];

            LoadingWaitLayoutMetrics metrics = LoadingWaitPlayerListLayout.Resolve(
                measureText: null,
                players,
                availableWidth: 900f,
                bandHeight: 200f,
                templateFontSize: 18f,
                rowGap: 8f);

            int covered = 0;
            foreach (LoadingWaitLayoutRow row in metrics.Rows)
            {
                covered += row.Count;
                Assert.Equal(covered - row.Count, row.StartIndex);
            }

            Assert.Equal(players.Count, covered);
        }

        [Fact]
        public void ResolveRowY_stacks_rows_with_distinct_y()
        {
            float bandBottomY = 0f;
            float bandHeight = 200f;
            float rowHeight = 26f;
            float rowGap = 10f;
            int rowCount = 3;

            float bottomRowY = LoadingWaitPlayerListLayout.ResolveRowY(
                0,
                rowCount,
                rowHeight,
                rowGap,
                bandBottomY,
                bandHeight);
            float middleRowY = LoadingWaitPlayerListLayout.ResolveRowY(
                1,
                rowCount,
                rowHeight,
                rowGap,
                bandBottomY,
                bandHeight);
            float topRowY = LoadingWaitPlayerListLayout.ResolveRowY(
                2,
                rowCount,
                rowHeight,
                rowGap,
                bandBottomY,
                bandHeight);

            Assert.Equal(rowHeight + rowGap, middleRowY - bottomRowY, 0.01f);
            Assert.Equal(rowHeight + rowGap, topRowY - middleRowY, 0.01f);
            Assert.True(topRowY > middleRowY);
            Assert.True(middleRowY > bottomRowY);

            float contentHeight = (rowCount * rowHeight) + ((rowCount - 1) * rowGap);
            float expectedMiddleCenter = bandBottomY + (bandHeight * 0.5f);
            float actualMiddleCenter = middleRowY + (rowHeight * 0.5f);
            Assert.Equal(expectedMiddleCenter, actualMiddleCenter, 0.01f);
        }

        [Theory]
        [InlineData(8, 3, new[] { 3, 3, 2 })]
        [InlineData(5, 2, new[] { 3, 2 })]
        public void BuildBalancedRows_splits_counts_evenly(int playerCount, int rowCount, int[] expectedCounts)
        {
            List<LoadingWaitLayoutRow> rows = LoadingWaitPlayerListLayout.BuildBalancedRows(playerCount, rowCount);

            Assert.Equal(expectedCounts.Length, rows.Count);
            int start = 0;
            for (int index = 0; index < expectedCounts.Length; index++)
            {
                Assert.Equal(start, rows[index].StartIndex);
                Assert.Equal(expectedCounts[index], rows[index].Count);
                start += expectedCounts[index];
            }
        }
    }
}
