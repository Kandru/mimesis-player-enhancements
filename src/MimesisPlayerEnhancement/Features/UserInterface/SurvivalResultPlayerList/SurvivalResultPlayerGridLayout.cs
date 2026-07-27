using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.SurvivalResultPlayerList
{
    internal static class SurvivalResultPlayerGridLayout
    {
        internal const int VanillaPlayerRows = 4;
        internal const int ColumnsPerRow = 6;
        internal const float ColumnGap = 6f;
        internal const float MinColumnWidth = 72f;
        internal const float GridWidthFraction = 0.9f;

        internal static bool ShouldUseExtendedLayout(bool enableMorePlayers, object[] parameters) =>
            enableMorePlayers
            && parameters.Length >= 3
            && parameters[2] is int playerCount
            && playerCount > VanillaPlayerRows;

        internal static string FormatDayResultsTitle(int cycleCount) =>
            $"DAY {cycleCount} RESULTS";

        internal static float ComputeCellWidth(float gridWidthLocal) =>
            Mathf.Max(
                MinColumnWidth,
                (gridWidthLocal - ((ColumnsPerRow - 1) * ColumnGap)) / ColumnsPerRow);

        internal static float ResolveGridWidthLocal(float screenWidth, float screenPerLocalX)
        {
            if (screenPerLocalX <= 0.01f)
            {
                screenPerLocalX = 1f;
            }

            return (screenWidth * GridWidthFraction) / screenPerLocalX;
        }

        internal static int ResolveActualPlayerCount(object[] parameters, bool success, int claimedCount)
        {
            int maxByLength = Math.Max(0, (parameters.Length - 3) / 3);
            int upper = Math.Min(claimedCount, maxByLength);

            if (!success)
            {
                return upper;
            }

            for (int actual = upper; actual >= 0; actual--)
            {
                int scrapBase = 3 + (actual * 3);
                if (scrapBase >= parameters.Length || parameters[scrapBase] is not int scrapCount || scrapCount < 0)
                {
                    continue;
                }

                int needed = scrapBase + 1 + (scrapCount * 2);
                if (needed <= parameters.Length)
                {
                    return actual;
                }
            }

            return upper;
        }

        internal static int ComputeRowCount(int displayCount) =>
            displayCount <= 0 ? 0 : (displayCount + ColumnsPerRow - 1) / ColumnsPerRow;
    }
}
