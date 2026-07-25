using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList
{
    internal struct LoadingWaitLayoutMetrics
    {
        internal float FontSize;
        internal float RowHeight;
        internal IReadOnlyList<LoadingWaitLayoutRow> Rows;
    }

    internal struct LoadingWaitLayoutRow
    {
        internal int StartIndex;
        internal int Count;
    }

    internal static class LoadingWaitPlayerListLayout
    {
        /// <summary>Smallest font used when packing many/long names into the wait band.</summary>
        internal const float MinFontSize = 16f;

        /// <summary>Hard upper limit for dynamic font scaling (few short names).</summary>
        internal const float MaxFontSize = 34f;

        private const float FallbackRowHeight = 26f;
        internal const float PlayerGap = 12f;

        internal static LoadingWaitLayoutMetrics Resolve(
            Component? measureText,
            IReadOnlyList<LoadingWaitPlayerEntry> players,
            float availableWidth,
            float bandHeight,
            float templateFontSize,
            float rowGap)
        {
            if (players.Count == 0)
            {
                return new LoadingWaitLayoutMetrics
                {
                    FontSize = Mathf.Clamp(templateFontSize, MinFontSize, MaxFontSize),
                    RowHeight = FallbackRowHeight,
                    Rows = [],
                };
            }

            float baseFont = Mathf.Clamp(templateFontSize, MinFontSize, MaxFontSize);
            float maxFont = Mathf.Min(MaxFontSize, baseFont * ResolveMaxFontScale(players.Count));
            float minFont = Mathf.Max(MinFontSize, baseFont * 0.85f);
            float safeWidth = Mathf.Max(availableWidth, 32f);

            // Probe at the target font so we keep multi-row wrapping instead of
            // shrinking onto a single overlapping line.
            int targetRowCount = ResolveTargetRowCount(measureText, players, safeWidth, maxFont);

            for (float fontSize = maxFont; fontSize >= minFont; fontSize -= 1f)
            {
                LoadingWaitLayoutMetrics? metrics = TryBuildMetrics(
                    measureText,
                    players,
                    safeWidth,
                    bandHeight,
                    fontSize,
                    rowGap,
                    targetRowCount);
                if (metrics != null)
                {
                    return metrics.Value;
                }
            }

            float fallbackFont = minFont;
            float fallbackRowHeight = MeasureRowHeight(measureText, fallbackFont);
            return new LoadingWaitLayoutMetrics
            {
                FontSize = fallbackFont,
                RowHeight = fallbackRowHeight,
                Rows = BuildGreedyRows(measureText, players, safeWidth, fallbackFont),
            };
        }

        private static float ResolveMaxFontScale(int playerCount) =>
            playerCount switch
            {
                <= 2 => 1.75f,
                <= 4 => 1.45f,
                <= 6 => 1.2f,
                <= 8 => 1.05f,
                _ => 1f,
            };

        private static int ResolveTargetRowCount(
            Component? measureText,
            IReadOnlyList<LoadingWaitPlayerEntry> players,
            float availableWidth,
            float fontSize)
        {
            List<LoadingWaitLayoutRow> greedy = BuildGreedyRows(
                measureText,
                players,
                availableWidth,
                fontSize);
            return Mathf.Clamp(greedy.Count, 1, players.Count);
        }

        private static LoadingWaitLayoutMetrics? TryBuildMetrics(
            Component? measureText,
            IReadOnlyList<LoadingWaitPlayerEntry> players,
            float availableWidth,
            float bandHeight,
            float fontSize,
            float rowGap,
            int targetRowCount)
        {
            float rowHeight = MeasureRowHeight(measureText, fontSize);
            List<LoadingWaitLayoutRow> rows = BuildGreedyRows(
                measureText,
                players,
                availableWidth,
                fontSize);

            // Keep at least the probed wrap count so font shrink cannot collapse to one line.
            if (rows.Count < targetRowCount)
            {
                rows = BuildBalancedRows(players.Count, targetRowCount);
            }

            if (!RowsFitWidth(measureText, players, rows, availableWidth, fontSize))
            {
                // Need more wrap; try one denser row split before giving up this font.
                int denserRows = Mathf.Min(players.Count, Mathf.Max(rows.Count + 1, targetRowCount + 1));
                rows = BuildBalancedRows(players.Count, denserRows);
                if (!RowsFitWidth(measureText, players, rows, availableWidth, fontSize))
                {
                    return null;
                }
            }

            float contentHeight = (rows.Count * rowHeight) + ((rows.Count - 1) * rowGap);
            if (bandHeight > 0.5f && contentHeight > bandHeight)
            {
                return null;
            }

            return new LoadingWaitLayoutMetrics
            {
                FontSize = fontSize,
                RowHeight = rowHeight,
                Rows = rows,
            };
        }

        private static bool RowsFitWidth(
            Component? measureText,
            IReadOnlyList<LoadingWaitPlayerEntry> players,
            IReadOnlyList<LoadingWaitLayoutRow> rows,
            float availableWidth,
            float fontSize)
        {
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                LoadingWaitLayoutRow row = rows[rowIndex];
                float rowWidth = MeasureRowWidth(
                    measureText,
                    players,
                    row.StartIndex,
                    row.Count,
                    fontSize);
                if (row.Count > 1 && rowWidth > availableWidth)
                {
                    return false;
                }
            }

            return true;
        }

        internal static List<LoadingWaitLayoutRow> BuildBalancedRows(int playerCount, int rowCount)
        {
            int clampedRows = Mathf.Clamp(rowCount, 1, Mathf.Max(playerCount, 1));
            int baseSize = playerCount / clampedRows;
            int remainder = playerCount % clampedRows;
            List<LoadingWaitLayoutRow> rows = new(clampedRows);
            int startIndex = 0;
            for (int rowIndex = 0; rowIndex < clampedRows; rowIndex++)
            {
                int count = baseSize + (rowIndex < remainder ? 1 : 0);
                if (count <= 0)
                {
                    continue;
                }

                rows.Add(new LoadingWaitLayoutRow
                {
                    StartIndex = startIndex,
                    Count = count,
                });
                startIndex += count;
            }

            return rows;
        }

        internal static List<LoadingWaitLayoutRow> BuildGreedyRows(
            Component? measureText,
            IReadOnlyList<LoadingWaitPlayerEntry> players,
            float availableWidth,
            float fontSize)
        {
            List<LoadingWaitLayoutRow> rows = [];
            int startIndex = 0;
            while (startIndex < players.Count)
            {
                int count = 0;
                float rowWidth = 0f;
                while (startIndex + count < players.Count)
                {
                    float itemWidth = MeasureItemWidth(
                        measureText,
                        players[startIndex + count].DisplayName,
                        fontSize);
                    float nextWidth = rowWidth + (count > 0 ? PlayerGap : 0f) + itemWidth;
                    if (count > 0 && nextWidth > availableWidth)
                    {
                        break;
                    }

                    rowWidth = nextWidth;
                    count++;
                    if (itemWidth > availableWidth)
                    {
                        break;
                    }
                }

                if (count <= 0)
                {
                    count = 1;
                }

                rows.Add(new LoadingWaitLayoutRow
                {
                    StartIndex = startIndex,
                    Count = count,
                });
                startIndex += count;
            }

            return rows;
        }

        internal static float MeasureItemWidth(
            Component? measureText,
            string displayName,
            float fontSize) =>
            Mathf.Max(
                LoadingWaitPlayerListTextMeasure.MeasurePreferredSize(measureText, displayName, fontSize).x,
                fontSize * 0.5f);

        internal static float MeasureRowWidth(
            Component? measureText,
            IReadOnlyList<LoadingWaitPlayerEntry> players,
            int startIndex,
            int count,
            float fontSize)
        {
            float width = 0f;
            for (int offset = 0; offset < count; offset++)
            {
                width += MeasureItemWidth(measureText, players[startIndex + offset].DisplayName, fontSize);
                if (offset < count - 1)
                {
                    width += PlayerGap;
                }
            }

            return width;
        }

        /// <summary>
        /// Bottom-left Y for row <paramref name="rowIndex"/> (0 = bottom of stack).
        /// Rows are spaced by rowHeight + rowGap and the stack is centered in the band.
        /// </summary>
        internal static float ResolveRowY(
            int rowIndex,
            int rowCount,
            float rowHeight,
            float rowGap,
            float bandBottomY,
            float bandHeight)
        {
            float safeRowHeight = Mathf.Max(rowHeight, FallbackRowHeight);
            float rowStep = safeRowHeight + Mathf.Max(rowGap, 1f);
            float contentHeight = (rowCount * safeRowHeight) + (Mathf.Max(rowCount - 1, 0) * Mathf.Max(rowGap, 1f));
            float bandCenter = bandBottomY + (Mathf.Max(bandHeight, contentHeight) * 0.5f);
            float stackBottom = bandCenter - (contentHeight * 0.5f);
            return stackBottom + (rowIndex * rowStep);
        }

        private static float MeasureRowHeight(Component? measureText, float fontSize) =>
            Mathf.Max(
                LoadingWaitPlayerListTextMeasure.MeasurePreferredSize(measureText, "Ag", fontSize).y,
                fontSize * 1.2f,
                FallbackRowHeight);
    }
}
