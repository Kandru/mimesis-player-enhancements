using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList
{
    internal struct LoadingWaitLayoutMetrics
    {
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
        private const float FallbackRowHeight = 26f;
        internal const float PlayerGap = 12f;

        internal static LoadingWaitLayoutMetrics Resolve(
            Component? measureText,
            IReadOnlyList<LoadingWaitPlayerEntry> players,
            float availableWidth,
            float bandHeight,
            float rowGap,
            float fontSize,
            float rowHeight)
        {
            if (players.Count == 0)
            {
                return new LoadingWaitLayoutMetrics
                {
                    RowHeight = rowHeight,
                    Rows = [],
                };
            }

            float safeWidth = Mathf.Max(availableWidth, 32f);
            int minRows = Mathf.Max(
                BuildGreedyRows(measureText, players, safeWidth, fontSize).Count,
                1);
            int maxRows = ResolveMaxRowCount(bandHeight, rowHeight, rowGap);
            int upperBound = Mathf.Min(players.Count, Mathf.Max(maxRows, minRows));

            for (int rowCount = minRows; rowCount <= upperBound; rowCount++)
            {
                List<LoadingWaitLayoutRow> rows = BuildBalancedRows(players.Count, rowCount);
                if (RowsFitWidth(measureText, players, rows, safeWidth, fontSize))
                {
                    return new LoadingWaitLayoutMetrics
                    {
                        RowHeight = rowHeight,
                        Rows = rows,
                    };
                }
            }

            return new LoadingWaitLayoutMetrics
            {
                RowHeight = rowHeight,
                Rows = BuildBalancedRows(players.Count, minRows),
            };
        }

        internal static int ResolveMaxRowCount(float bandHeight, float rowHeight, float rowGap)
        {
            if (bandHeight <= 0.5f)
            {
                return int.MaxValue;
            }

            float safeRowHeight = Mathf.Max(rowHeight, FallbackRowHeight);
            float rowStep = safeRowHeight + Mathf.Max(rowGap, 1f);
            return Mathf.Max(Mathf.FloorToInt((bandHeight + rowGap) / rowStep), 1);
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
                if (MeasureRowWidth(measureText, players, row.StartIndex, row.Count, fontSize) > availableWidth)
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
            float bandCenter = bandBottomY + (bandHeight * 0.5f);
            float stackBottom = bandCenter - (contentHeight * 0.5f);
            return stackBottom + (rowIndex * rowStep);
        }
    }
}
