using MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList;
using Xunit;

namespace MimesisPlayerEnhancement.Tests.Features.UserInterface
{
    public sealed class LoadingWaitPlayerListLayoutTests
    {
        private const float FontSize = 20f;
        private const float RowHeight = 26f;
        private const float RowGap = 10f;
        private const float BandHeight = 70f;
        private const float WideWidth = 1600f;
        private const float NarrowWidth = 320f;

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
                fontSize: FontSize);

            Assert.True(rows.Count > 1);
            int covered = 0;
            foreach (LoadingWaitLayoutRow row in rows)
            {
                float rowWidth = LoadingWaitPlayerListLayout.MeasureRowWidth(
                    null,
                    players,
                    row.StartIndex,
                    row.Count,
                    fontSize: FontSize);
                Assert.True(rowWidth <= 140f);
                covered += row.Count;
            }

            Assert.Equal(players.Count, covered);
        }

        [Fact]
        public void Resolve_uses_single_row_for_four_short_names_on_wide_band()
        {
            List<LoadingWaitPlayerEntry> players =
            [
                new() { DisplayName = "A" },
                new() { DisplayName = "B" },
                new() { DisplayName = "C" },
                new() { DisplayName = "D" },
            ];

            LoadingWaitLayoutMetrics metrics = LoadingWaitPlayerListLayout.Resolve(
                measureText: null,
                players,
                availableWidth: WideWidth,
                bandHeight: BandHeight,
                rowGap: RowGap,
                fontSize: FontSize,
                rowHeight: RowHeight);

            Assert.Single(metrics.Rows);
        }

        [Fact]
        public void Resolve_wraps_long_names_to_multiple_rows()
        {
            List<LoadingWaitPlayerEntry> players =
            [
                new() { DisplayName = "VeryLongPlayerNameOne" },
                new() { DisplayName = "VeryLongPlayerNameTwo" },
                new() { DisplayName = "VeryLongPlayerNameThree" },
                new() { DisplayName = "VeryLongPlayerNameFour" },
            ];

            LoadingWaitLayoutMetrics metrics = LoadingWaitPlayerListLayout.Resolve(
                measureText: null,
                players,
                availableWidth: 600f,
                bandHeight: BandHeight,
                rowGap: RowGap,
                fontSize: FontSize,
                rowHeight: RowHeight);

            Assert.True(metrics.Rows.Count > 1);
        }

        [Fact]
        public void Resolve_balances_players_evenly_across_rows()
        {
            List<LoadingWaitPlayerEntry> players = Enumerable.Range(1, 10)
                .Select(index => new LoadingWaitPlayerEntry { DisplayName = $"Player {index:00}" })
                .ToList();

            LoadingWaitLayoutMetrics metrics = LoadingWaitPlayerListLayout.Resolve(
                measureText: null,
                players,
                availableWidth: 420f,
                bandHeight: BandHeight,
                rowGap: RowGap,
                fontSize: FontSize,
                rowHeight: RowHeight);

            Assert.True(metrics.Rows.Count > 1);
            int minCount = metrics.Rows.Min(row => row.Count);
            int maxCount = metrics.Rows.Max(row => row.Count);
            Assert.True(maxCount - minCount <= 1);
            Assert.Equal(players.Count, CountCoveredPlayers(metrics.Rows));
        }

        [Fact]
        public void Resolve_wraps_on_narrow_width_without_dropping_players()
        {
            List<LoadingWaitPlayerEntry> players = Enumerable.Range(1, 6)
                .Select(index => new LoadingWaitPlayerEntry { DisplayName = $"Player {index:00}" })
                .ToList();

            LoadingWaitLayoutMetrics metrics = LoadingWaitPlayerListLayout.Resolve(
                measureText: null,
                players,
                availableWidth: NarrowWidth,
                bandHeight: BandHeight,
                rowGap: RowGap,
                fontSize: FontSize,
                rowHeight: RowHeight);

            Assert.True(metrics.Rows.Count > 1);
            Assert.Equal(players.Count, CountCoveredPlayers(metrics.Rows));
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
                bandHeight: BandHeight,
                rowGap: RowGap,
                fontSize: FontSize,
                rowHeight: RowHeight);

            int covered = 0;
            foreach (LoadingWaitLayoutRow row in metrics.Rows)
            {
                covered += row.Count;
                Assert.Equal(covered - row.Count, row.StartIndex);
            }

            Assert.Equal(players.Count, covered);
        }

        [Fact]
        public void ResolveMaxRowCount_fits_rows_within_band_height()
        {
            int maxRows = LoadingWaitPlayerListLayout.ResolveMaxRowCount(
                bandHeight: BandHeight,
                rowHeight: RowHeight,
                rowGap: RowGap);

            Assert.Equal(2, maxRows);
            Assert.Equal(LoadingWaitPlayerListLayout.MaxRows, maxRows);

            float contentHeight = (maxRows * RowHeight) + ((maxRows - 1) * RowGap);
            Assert.True(contentHeight <= BandHeight + 0.01f);
        }

        [Fact]
        public void ResolveMaxRowCount_hard_caps_at_MaxRows_even_on_tall_band()
        {
            int maxRows = LoadingWaitPlayerListLayout.ResolveMaxRowCount(
                bandHeight: 216f,
                rowHeight: RowHeight,
                rowGap: RowGap);

            Assert.Equal(LoadingWaitPlayerListLayout.MaxRows, maxRows);
        }

        [Fact]
        public void Resolve_caps_rows_to_band_height()
        {
            List<LoadingWaitPlayerEntry> players = Enumerable.Range(1, 32)
                .Select(index => new LoadingWaitPlayerEntry { DisplayName = $"P{index:00}" })
                .ToList();
            const float tightBandHeight = 72f;

            LoadingWaitLayoutMetrics metrics = LoadingWaitPlayerListLayout.Resolve(
                measureText: null,
                players,
                availableWidth: WideWidth,
                bandHeight: tightBandHeight,
                rowGap: RowGap,
                fontSize: FontSize,
                rowHeight: RowHeight);

            int maxRows = LoadingWaitPlayerListLayout.ResolveMaxRowCount(
                tightBandHeight,
                RowHeight,
                RowGap);
            Assert.True(metrics.Rows.Count <= maxRows);
        }

        [Fact]
        public void ResolveRowY_stacks_rows_with_distinct_y()
        {
            float bandBottomY = 0f;
            float bandHeight = 200f;
            float rowGap = 10f;
            int rowCount = 3;

            float bottomRowY = LoadingWaitPlayerListLayout.ResolveRowY(
                0,
                rowCount,
                RowHeight,
                rowGap,
                bandBottomY,
                bandHeight);
            float middleRowY = LoadingWaitPlayerListLayout.ResolveRowY(
                1,
                rowCount,
                RowHeight,
                rowGap,
                bandBottomY,
                bandHeight);
            float topRowY = LoadingWaitPlayerListLayout.ResolveRowY(
                2,
                rowCount,
                RowHeight,
                rowGap,
                bandBottomY,
                bandHeight);

            Assert.Equal(RowHeight + rowGap, middleRowY - bottomRowY, 0.01f);
            Assert.Equal(RowHeight + rowGap, topRowY - middleRowY, 0.01f);
            Assert.True(topRowY > middleRowY);
            Assert.True(middleRowY > bottomRowY);

            float contentHeight = (rowCount * RowHeight) + ((rowCount - 1) * rowGap);
            float expectedMiddleCenter = bandBottomY + (bandHeight * 0.5f);
            float actualMiddleCenter = middleRowY + (RowHeight * 0.5f);
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

        private static int CountCoveredPlayers(IReadOnlyList<LoadingWaitLayoutRow> rows)
        {
            int covered = 0;
            foreach (LoadingWaitLayoutRow row in rows)
            {
                covered += row.Count;
            }

            return covered;
        }
    }
}
