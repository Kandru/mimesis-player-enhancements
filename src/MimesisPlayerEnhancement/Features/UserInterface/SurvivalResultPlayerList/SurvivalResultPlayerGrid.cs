using System.Reflection;
using System.Runtime.CompilerServices;
using ReluProtocol.Enum;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.SurvivalResultPlayerList
{
    internal readonly struct SurvivalResultRowSlot
    {
        internal SurvivalResultRowSlot(
            Transform rowRoot,
            Component nameText,
            GameObject survivalIcon,
            GameObject killedIcon,
            GameObject wastedIcon,
            Component awardText)
        {
            RowRoot = rowRoot;
            NameText = nameText;
            SurvivalIcon = survivalIcon;
            KilledIcon = killedIcon;
            WastedIcon = wastedIcon;
            AwardText = awardText;
        }

        internal Transform RowRoot { get; }
        internal Component NameText { get; }
        internal GameObject SurvivalIcon { get; }
        internal GameObject KilledIcon { get; }
        internal GameObject WastedIcon { get; }
        internal Component AwardText { get; }
    }

    internal static class SurvivalResultPlayerGrid
    {
        private const string Feature = "Ui";
        private const int VanillaPlayerRows = 4;
        private const int ColumnsPerRow = 6;
        private const int MaxDisplayPlayers = 24;
        private const float GridWidthFraction = 0.9f;
        private const float ColumnGap = 6f;
        private const float RowGap = 14f;
        private const float RowElementGap = 3f;
        private const float HeaderGap = 30f;
        private const float LossGap = 12f;
        private const float ScreenEdgeMargin = 8f;
        private const float CornerMargin = 16f;
        private const float FallbackCellWidth = 120f;
        private const float FallbackCellHeight = 28f;
        private const float MinColumnWidth = 72f;
        private const float AwardLineReserve = 18f;

        private static readonly ConditionalWeakTable<object, SurvivalResultGridState> GridStates = new();

        private static readonly MemberInfo? RandDungeonSeedMember =
            (MemberInfo?)AccessTools.Field(typeof(Hub.PersistentData), "randDungeonSeed")
            ?? AccessTools.Property(typeof(Hub.PersistentData), "randDungeonSeed");

        private sealed class SurvivalResultGridState
        {
            internal SurvivalResultRowSlot[] NativeRows = [];
            internal readonly List<SurvivalResultRowSlot> CloneRows = [];
            internal RectTransform? GridRoot;
            internal float CellWidth = FallbackCellWidth;
            internal float CellHeight = FallbackCellHeight;
            internal float ColStep = FallbackCellWidth + ColumnGap;
            internal float RowStep = FallbackCellHeight + RowGap;
            internal float YDirection = -1f;
            internal float TitleCenterX;
            internal bool MetricsMeasured;
            internal RectTransform? TitleRect;
            internal Component? TitleText;
            internal RectTransform? LostAllRect;
            internal RectTransform? LostScrabsRect;
            internal bool ExtendedActive;
            internal int LastCycleCount;
            internal int LastDisplayCount;
            internal int LastRowCount;
            internal float LastLossHeight;
            internal bool LastLossVisible;
        }

        internal static bool ShouldUseExtendedLayout(object[] parameters) =>
            ModConfig.EnableMorePlayers.Value
            && parameters.Length >= 3
            && parameters[2] is int playerCount
            && playerCount > 4;

        private static readonly string[] VanillaDecorGraphicNames =
        [
            "TitleShape",
            "TxtShape",
        ];

        private static readonly string[] SeedChromeObjectNames =
        [
            "InputField",
            "SeedShape",
            "RandomSeedBg",
        ];

        private static readonly string[] StrayQuotaLabelNames =
        [
            "TotalQuota",
            "TotalQuotaTitle",
            "CycleCount",
            "Quota",
            "NextQuota",
            "RemainDays",
        ];

        internal static void RefreshVisibleLayout(object ui)
        {
            SurvivalResultGridState state = GetOrCreateState(ui);
            if (!state.ExtendedActive)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            SuppressVanillaChrome(ui, state);
            state.MetricsMeasured = false;
            MeasureMetrics(ui, state);
            float[] rowHeights = PrepareRowLayout(state, state.LastDisplayCount);
            float headerHeight = ConfigureHeaderText(state, state.LastCycleCount);
            PositionGridRoot(ui, state, rowHeights, headerHeight, state.LastLossVisible, state.LastLossHeight);
            PlaceHeaderLabel(state, rowHeights, headerHeight);
            PlaceCloneRows(ui, state, state.LastDisplayCount, rowHeights);
            PlaceLossSection(state, rowHeights, state.LastLossVisible);
            HideStrayQuotaLabels(ui);
            PlaceSeedLabel(ui);
        }

        private readonly struct ParsedParameters
        {
            internal ParsedParameters(
                int cycleCount,
                bool success,
                int claimedCount,
                int actualCount,
                int scrapBaseIndex)
            {
                CycleCount = cycleCount;
                Success = success;
                ClaimedCount = claimedCount;
                ActualCount = actualCount;
                ScrapBaseIndex = scrapBaseIndex;
            }

            internal int CycleCount { get; }
            internal bool Success { get; }
            internal int ClaimedCount { get; }
            internal int ActualCount { get; }
            internal int ScrapBaseIndex { get; }
        }

        internal static void ApplyPatchParameter(object ui, object[] parameters)
        {
            if (!TryParseParameters(parameters, out ParsedParameters parsed))
            {
                ModLog.Warn(
                    Feature,
                    $"SurvivalResult params unreadable — length={parameters.Length}");
                ApplyMinimalFallback(ui, parameters);
                return;
            }

            if (parsed.ActualCount != parsed.ClaimedCount)
            {
                ModLog.Warn(
                    Feature,
                    $"SurvivalResult player count mismatch — claimed={parsed.ClaimedCount}, actual={parsed.ActualCount}, length={parameters.Length}");
            }

            SurvivalResultGridState state = GetOrCreateState(ui);
            CacheNativeRows(ui, state);
            if (state.NativeRows.Length == 0)
            {
                ModLog.Warn(Feature, "SurvivalResult native rows missing — minimal fallback");
                ApplyMinimalFallback(ui, parameters);
                return;
            }

            MeasureMetrics(ui, state);
            SuppressVanillaChrome(ui, state);
            HideNativeRows(state);
            HideStrayQuotaLabels(ui);

            bool lossVisible = WillShowLoss(parameters, parsed);
            float lossHeight = EstimateLossHeight(state, lossVisible, parsed);

            int maxVisible = ComputeMaxVisible(state, lossVisible, lossHeight);
            int displayCount = Math.Min(
                Math.Min(parsed.ActualCount, MorePlayersPatchHelpers.GetMaxPlayers()),
                Math.Min(maxVisible, MaxDisplayPlayers));

            EnsureCloneRows(ui, state, displayCount);

            for (int i = 0; i < displayCount; i++)
            {
                ResetSlot(state.CloneRows[i]);
                PopulateRow(state.CloneRows[i], parameters, i);
            }

            for (int i = displayCount; i < state.CloneRows.Count; i++)
            {
                state.CloneRows[i].RowRoot.gameObject.SetActive(false);
            }

            float[] rowHeights = PrepareRowLayout(state, displayCount);
            int rowCount = rowHeights.Length;
            float headerHeight = ConfigureHeaderText(state, parsed.CycleCount);

            state.ExtendedActive = true;
            state.LastCycleCount = parsed.CycleCount;
            state.LastDisplayCount = displayCount;
            state.LastRowCount = rowCount;
            state.LastLossHeight = lossHeight;
            state.LastLossVisible = lossVisible;

            PositionGridRoot(ui, state, rowHeights, headerHeight, lossVisible, lossHeight);
            PlaceHeaderLabel(state, rowHeights, headerHeight);
            PlaceCloneRows(ui, state, displayCount, rowHeights);

            try
            {
                ApplyLostScrapSection(ui, parameters, parsed);
                PlaceLossSection(state, rowHeights, lossVisible);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SurvivalResult loss section failed — {ex.Message}");
            }

            HideStrayQuotaLabels(ui);
            PlaceSeedLabel(ui);

            ModLog.Debug(
                Feature,
                $"SurvivalResult layout — display={displayCount}/{parsed.ActualCount}, rows={rowCount}, maxVisible={maxVisible}, loss={lossVisible}");
        }

        internal static void ApplyMinimalFallback(object ui, object[] parameters)
        {
            SurvivalResultGridState state = GetOrCreateState(ui);
            state.ExtendedActive = false;

            try
            {
                if (parameters.Length >= 1 && parameters[0] is int cycle)
                {
                    SetTitle(ui, FormatDayResultsTitle(cycle));
                }
            }
            catch
            {
                // Best-effort only — must not throw before Show().
            }
        }

        private static bool TryParseParameters(object[] parameters, out ParsedParameters parsed)
        {
            parsed = default;
            if (parameters.Length < 3
                || parameters[0] is not int cycleCount
                || parameters[1] is not bool success
                || parameters[2] is not int claimedCount
                || claimedCount < 0)
            {
                return false;
            }

            int actualCount = ResolveActualPlayerCount(parameters, success, claimedCount);
            int scrapBase = 3 + (actualCount * 3);
            parsed = new ParsedParameters(cycleCount, success, claimedCount, actualCount, scrapBase);
            return true;
        }

        private static int ResolveActualPlayerCount(object[] parameters, bool success, int claimedCount)
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

        private static SurvivalResultGridState GetOrCreateState(object ui) =>
            GridStates.GetValue(ui, _ => new SurvivalResultGridState());

        private static void CacheNativeRows(object ui, SurvivalResultGridState state)
        {
            if (state.NativeRows.Length > 0)
            {
                return;
            }

            List<SurvivalResultRowSlot> rows = [];
            for (int i = 0; i < VanillaPlayerRows; i++)
            {
                if (!TryGetUiText(ui, $"P{i + 1}Name", out Component? nameText) || nameText == null)
                {
                    break;
                }

                if (!TryBindRow(nameText.transform.parent, out SurvivalResultRowSlot slot))
                {
                    ModLog.Warn(Feature, $"SurvivalResult native row P{i + 1} bind failed");
                    break;
                }

                rows.Add(slot);
            }

            state.NativeRows = rows.ToArray();
        }

        private static void SuppressVanillaChrome(object ui, SurvivalResultGridState state)
        {
            HideVanillaBanner(ui);
            state.TitleCenterX = ResolveGridCenterX(ui);
        }

        private static void HideVanillaBanner(object ui)
        {
            if (TryGetUiImage(ui, "rootNode", out Image? rootNode) && rootNode != null)
            {
                DisableGraphic(rootNode);
            }

            if (ui is not Component host)
            {
                return;
            }

            Transform dialogRoot = host.transform;
            foreach (string objectName in VanillaDecorGraphicNames)
            {
                Transform? decor = FindChildRecursive(dialogRoot, objectName);
                if (decor == null)
                {
                    continue;
                }

                foreach (Graphic graphic in decor.GetComponents<Graphic>())
                {
                    DisableGraphic(graphic);
                }
            }
        }

        private static RectTransform? GetDialogRoot(object ui) =>
            ui is Component host ? host.transform as RectTransform : null;

        private static Transform? FindChildRecursive(Transform root, string objectName)
        {
            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform? match = FindChildRecursive(root.GetChild(i), objectName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static void DisableGraphic(Graphic graphic)
        {
            graphic.enabled = false;
            graphic.raycastTarget = false;

            if (graphic is not Image image)
            {
                return;
            }

            image.sprite = null;
            Color clear = image.color;
            clear.a = 0f;
            image.color = clear;
        }

        private static void MeasureMetrics(object ui, SurvivalResultGridState state)
        {
            if (state.MetricsMeasured)
            {
                return;
            }

            if (TryGetUiText(ui, "title", out Component? title) && title?.transform is RectTransform titleRect)
            {
                state.TitleRect = titleRect;
                state.TitleText = title;
                state.TitleCenterX = titleRect.anchoredPosition.x;
            }

            RectTransform? anchor = state.NativeRows[0].RowRoot as RectTransform;
            UpdateGridColumnMetrics(state, anchor);

            SurvivalResultRowSlot template = state.NativeRows[0];
            CompactRowLayout(template, state.CellWidth, out _, out float compactHeight);

            state.CellWidth = Mathf.Max(MinColumnWidth, state.CellWidth);
            state.CellHeight = compactHeight > 1f ? compactHeight : FallbackCellHeight;
            state.RowStep = state.CellHeight + RowGap + AwardLineReserve;

            MeasureRowDirection(state);

            if (TryGetUiObject(ui, "UE_LostAllScrab", out GameObject? lostAll)
                && lostAll?.transform is RectTransform lostAllRect)
            {
                state.LostAllRect = lostAllRect;
            }

            if (TryGetUiObject(ui, "UE_LostScrabs", out GameObject? lostScrabs)
                && lostScrabs?.transform is RectTransform lostScrabsRect)
            {
                state.LostScrabsRect = lostScrabsRect;
            }

            state.MetricsMeasured = true;
        }

        private static float[] PrepareRowLayout(SurvivalResultGridState state, int displayCount)
        {
            RectTransform? anchor = state.NativeRows[0].RowRoot as RectTransform;
            UpdateGridColumnMetrics(state, anchor);

            for (int i = 0; i < displayCount && i < state.CloneRows.Count; i++)
            {
                CompactRowLayout(state.CloneRows[i], state.CellWidth, out _, out _);
            }

            return MeasureRowHeights(state, displayCount);
        }

        private static void UpdateGridColumnMetrics(SurvivalResultGridState state, RectTransform? anchor)
        {
            float gridWidth = ResolveGridWidthLocal(anchor);
            state.CellWidth = Mathf.Max(
                MinColumnWidth,
                (gridWidth - ((ColumnsPerRow - 1) * ColumnGap)) / ColumnsPerRow);
            state.ColStep = state.CellWidth + ColumnGap;
        }

        private static float ResolveGridWidthLocal(RectTransform? anchor)
        {
            float screenPerLocalX = ResolveScreenPerLocalX(anchor);
            if (screenPerLocalX <= 0.01f)
            {
                screenPerLocalX = 1f;
            }

            return (Screen.width * GridWidthFraction) / screenPerLocalX;
        }

        private static float[] MeasureRowHeights(SurvivalResultGridState state, int displayCount)
        {
            if (displayCount <= 0)
            {
                return [];
            }

            int rowCount = (displayCount + ColumnsPerRow - 1) / ColumnsPerRow;
            float[] heights = new float[rowCount];
            for (int i = 0; i < displayCount && i < state.CloneRows.Count; i++)
            {
                int row = i / ColumnsPerRow;
                if (state.CloneRows[i].RowRoot is RectTransform rowRect)
                {
                    heights[row] = Mathf.Max(heights[row], rowRect.rect.height);
                }
            }

            for (int row = 0; row < rowCount; row++)
            {
                heights[row] = Mathf.Max(heights[row], FallbackCellHeight);
            }

            float maxHeight = FallbackCellHeight;
            for (int row = 0; row < rowCount; row++)
            {
                maxHeight = Mathf.Max(maxHeight, heights[row]);
            }

            state.CellHeight = maxHeight;
            return heights;
        }

        private static float GetRowCenterOffset(float yDirection, float[] rowHeights, int rowIndex)
        {
            if (rowIndex <= 0 || rowHeights.Length == 0)
            {
                return 0f;
            }

            float y = 0f;
            for (int row = 1; row <= rowIndex; row++)
            {
                y += yDirection * ((rowHeights[row - 1] * 0.5f) + RowGap + (rowHeights[row] * 0.5f));
            }

            return y;
        }

        private static float GetGridBottomOffset(float yDirection, float[] rowHeights)
        {
            if (rowHeights.Length == 0)
            {
                return 0f;
            }

            int lastRow = rowHeights.Length - 1;
            float lastCenter = GetRowCenterOffset(yDirection, rowHeights, lastRow);
            return lastCenter - (rowHeights[lastRow] * 0.5f);
        }

        private static string FormatDayResultsTitle(int cycleCount) =>
            $"DAY {cycleCount} RESULTS";

        private static float ConfigureHeaderText(SurvivalResultGridState state, int cycleCount)
        {
            if (state.TitleRect == null || state.TitleText == null)
            {
                return 36f;
            }

            SetText(state.TitleText, FormatDayResultsTitle(cycleCount));
            ConfigureHorizontalTitle(state);
            Canvas.ForceUpdateCanvases();
            return Mathf.Max(32f, GetRectHeight(state.TitleRect));
        }

        private static void ConfigureHorizontalTitle(SurvivalResultGridState state)
        {
            if (state.TitleRect == null || state.TitleText == null)
            {
                return;
            }

            float titleWidth = (ColumnsPerRow * state.CellWidth) + ((ColumnsPerRow - 1) * ColumnGap);
            state.TitleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, titleWidth);
            state.TitleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40f);

            ModUiText.ConfigureTextLayout(state.TitleText, wordWrap: false, overflowMode: ModUiText.OverflowOverflow);
            ModUiText.ConfigureTightSingleLine(state.TitleText);

            PropertyInfo? alignmentProp = state.TitleText.GetType().GetProperty("alignment");
            if (alignmentProp != null && alignmentProp.PropertyType.IsEnum)
            {
                alignmentProp.SetValue(state.TitleText, Enum.ToObject(alignmentProp.PropertyType, 514));
            }
        }

        private static float GetHeaderTopOffset(float[] rowHeights, float headerHeight)
        {
            float row0Height = rowHeights.Length > 0 ? rowHeights[0] : FallbackCellHeight;
            return (row0Height * 0.5f) + HeaderGap + headerHeight;
        }

        private static void PlaceHeaderLabel(
            SurvivalResultGridState state,
            float[] rowHeights,
            float headerHeight)
        {
            if (state.TitleRect == null || state.GridRoot == null || state.TitleText == null)
            {
                return;
            }

            state.TitleRect.gameObject.SetActive(true);
            state.TitleRect.SetParent(state.GridRoot, false);
            state.TitleRect.anchorMin = new Vector2(0.5f, 0.5f);
            state.TitleRect.anchorMax = new Vector2(0.5f, 0.5f);
            state.TitleRect.pivot = new Vector2(0.5f, 0.5f);

            float row0Height = rowHeights.Length > 0 ? rowHeights[0] : state.CellHeight;
            float headerY = (row0Height * 0.5f) + HeaderGap + (headerHeight * 0.5f);
            state.TitleRect.anchoredPosition = new Vector2(0f, headerY);
        }

        private static void HideStrayQuotaLabels(object ui)
        {
            if (ui is not Component host)
            {
                return;
            }

            Transform dialogRoot = host.transform;
            foreach (string objectName in StrayQuotaLabelNames)
            {
                Transform? label = FindChildRecursive(dialogRoot, objectName);
                if (label != null)
                {
                    label.gameObject.SetActive(false);
                }
            }
        }

        private static void PlaceSeedLabel(object ui)
        {
            if (!TryGetUiText(ui, "RandomSeed", out Component? randomSeed)
                || randomSeed?.transform is not RectTransform seedRect
                || GetDialogRoot(ui) is not RectTransform dialogRoot)
            {
                return;
            }

            Transform? seedContainer = seedRect.parent;
            HideSeedChrome(ui, seedRect);

            object? seedValue = GetRandomDungeonSeed();
            SetText(randomSeed, seedValue?.ToString() ?? string.Empty);
            randomSeed.gameObject.SetActive(true);

            seedRect.SetParent(dialogRoot, false);
            seedRect.SetAsLastSibling();
            seedRect.anchorMin = new Vector2(0f, 0f);
            seedRect.anchorMax = new Vector2(0f, 0f);
            seedRect.pivot = new Vector2(0f, 0f);
            seedRect.anchoredPosition = new Vector2(CornerMargin, CornerMargin);

            if (seedContainer != null && seedContainer != dialogRoot)
            {
                seedContainer.gameObject.SetActive(false);
            }
        }

        private static void HideSeedChrome(object ui, RectTransform seedRect)
        {
            Transform? container = seedRect.parent;
            if (container != null)
            {
                foreach (Graphic graphic in container.GetComponents<Graphic>())
                {
                    if (graphic.transform != seedRect)
                    {
                        DisableGraphic(graphic);
                    }
                }
            }

            if (ui is not Component host)
            {
                return;
            }

            Transform dialogRoot = host.transform;
            foreach (string objectName in SeedChromeObjectNames)
            {
                Transform? chrome = FindChildRecursive(dialogRoot, objectName);
                if (chrome != null)
                {
                    chrome.gameObject.SetActive(false);
                }
            }
        }

        private static void MeasureRowDirection(SurvivalResultGridState state)
        {
            if (state.NativeRows.Length < 2
                || state.NativeRows[0].RowRoot is not RectTransform first
                || state.NativeRows[1].RowRoot is not RectTransform second)
            {
                state.YDirection = -1f;
                return;
            }

            float deltaY = second.anchoredPosition.y - first.anchoredPosition.y;
            state.YDirection = Mathf.Approximately(deltaY, 0f) ? -1f : Mathf.Sign(deltaY);
            if (Mathf.Abs(deltaY) > 1f)
            {
                state.RowStep = Mathf.Max(Mathf.Abs(deltaY), state.RowStep);
            }
        }

        private static void DisableLayoutDrivers(Transform node)
        {
            foreach (LayoutGroup layout in node.GetComponents<LayoutGroup>())
            {
                layout.enabled = false;
            }

            foreach (ContentSizeFitter fitter in node.GetComponents<ContentSizeFitter>())
            {
                fitter.enabled = false;
            }
        }

        private static RectTransform EnsureGridRoot(object ui, SurvivalResultGridState state)
        {
            if (state.GridRoot != null)
            {
                return state.GridRoot;
            }

            RectTransform? dialogRoot = GetDialogRoot(ui);
            Transform gridParent = dialogRoot != null
                ? dialogRoot
                : state.NativeRows[0].RowRoot.parent;

            Transform nativeParent = state.NativeRows[0].RowRoot.parent;
            DisableLayoutDrivers(nativeParent);
            if (nativeParent.parent != null)
            {
                DisableLayoutDrivers(nativeParent.parent);
            }

            GameObject gridGo = new GameObject("SurvivalResultGrid", typeof(RectTransform));
            RectTransform gridRect = gridGo.GetComponent<RectTransform>();
            gridRect.SetParent(gridParent, false);
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = Vector2.zero;
            gridRect.sizeDelta = Vector2.zero;
            gridGo.layer = gridParent.gameObject.layer;

            state.GridRoot = gridRect;
            return gridRect;
        }

        private static float GetContentBottomOffset(
            SurvivalResultGridState state,
            float[] rowHeights,
            bool lossVisible,
            float lossHeight)
        {
            float bottom = GetGridBottomOffset(state.YDirection, rowHeights);
            if (lossVisible)
            {
                bottom += state.YDirection * (LossGap + lossHeight);
            }

            return bottom;
        }

        private static float ResolveScreenCenterY(object ui)
        {
            RectTransform? dialogRoot = GetDialogRoot(ui);
            if (dialogRoot == null)
            {
                return 0f;
            }

            Camera? cam = GetCanvasCamera(dialogRoot);
            Vector2 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.5f);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(dialogRoot, screenCenter, cam, out Vector2 local)
                ? local.y
                : 0f;
        }

        private static void PositionGridRoot(
            object ui,
            SurvivalResultGridState state,
            float[] rowHeights,
            float headerHeight,
            bool lossVisible,
            float lossHeight)
        {
            RectTransform gridRoot = EnsureGridRoot(ui, state);
            float centerX = ResolveGridCenterX(ui);
            float screenCenterY = ResolveScreenCenterY(ui);

            float contentTop = GetHeaderTopOffset(rowHeights, headerHeight);
            float contentBottom = GetContentBottomOffset(state, rowHeights, lossVisible, lossHeight);
            float contentCenterOffset = (contentTop + contentBottom) * 0.5f;
            float row0Y = screenCenterY - contentCenterOffset;

            gridRoot.anchoredPosition = new Vector2(centerX, row0Y);
        }

        private static void HideNativeRows(SurvivalResultGridState state)
        {
            foreach (SurvivalResultRowSlot row in state.NativeRows)
            {
                if (row.RowRoot != null)
                {
                    row.RowRoot.gameObject.SetActive(false);
                }
            }
        }

        private static bool WillShowLoss(object[] parameters, ParsedParameters parsed)
        {
            if (!parsed.Success)
            {
                return true;
            }

            if (parsed.ScrapBaseIndex >= parameters.Length || parameters[parsed.ScrapBaseIndex] is not int scrapCount)
            {
                return false;
            }

            return scrapCount > 0;
        }

        private static float EstimateLossHeight(
            SurvivalResultGridState state,
            bool lossVisible,
            ParsedParameters parsed)
        {
            if (!lossVisible)
            {
                return 0f;
            }

            RectTransform? active = !parsed.Success ? state.LostAllRect : state.LostScrabsRect;
            if (active != null && active.rect.height > 1f)
            {
                return active.rect.height;
            }

            return state.CellHeight;
        }

        private static int ComputeMaxVisible(SurvivalResultGridState state, bool lossVisible, float lossHeight)
        {
            float screenPerLocal = ResolveScreenPerLocalY(state.TitleRect ?? state.NativeRows[0].RowRoot as RectTransform);
            if (screenPerLocal <= 0.01f)
            {
                screenPerLocal = 1f;
            }

            float estimatedHeaderHeight = 36f;
            float headerBlock = estimatedHeaderHeight + HeaderGap;
            float lossBlock = lossVisible ? lossHeight + LossGap : 0f;
            float availableLocal = (Screen.height / screenPerLocal) - (2f * ScreenEdgeMargin) - headerBlock - lossBlock;
            int maxRows = Mathf.Max(1, Mathf.FloorToInt(availableLocal / state.RowStep));
            return Math.Min(MaxDisplayPlayers, maxRows * ColumnsPerRow);
        }

        private static float ResolveScreenPerLocalY(RectTransform? sample)
        {
            if (sample == null)
            {
                return 1f;
            }

            Vector3 worldDelta = sample.TransformVector(new Vector3(0f, 1f, 0f));
            Camera? cam = GetCanvasCamera(sample);
            Vector3 p0 = RectTransformUtility.WorldToScreenPoint(cam, sample.position);
            Vector3 p1 = RectTransformUtility.WorldToScreenPoint(cam, sample.position + worldDelta);
            float delta = p1.y - p0.y;
            return Mathf.Abs(delta) > 0.01f ? delta : 1f;
        }

        private static float ResolveScreenPerLocalX(RectTransform? sample)
        {
            if (sample == null)
            {
                return 1f;
            }

            Vector3 worldDelta = sample.TransformVector(new Vector3(1f, 0f, 0f));
            Camera? cam = GetCanvasCamera(sample);
            Vector3 p0 = RectTransformUtility.WorldToScreenPoint(cam, sample.position);
            Vector3 p1 = RectTransformUtility.WorldToScreenPoint(cam, sample.position + worldDelta);
            float delta = p1.x - p0.x;
            return Mathf.Abs(delta) > 0.01f ? delta : 1f;
        }

        private static Camera? GetCanvasCamera(RectTransform rect)
        {
            Canvas? canvas = rect.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera;
        }

        private static void EnsureCloneRows(object ui, SurvivalResultGridState state, int requiredSlots)
        {
            RectTransform gridRoot = EnsureGridRoot(ui, state);
            SurvivalResultRowSlot template = state.NativeRows[0];

            while (state.CloneRows.Count > requiredSlots)
            {
                int last = state.CloneRows.Count - 1;
                SurvivalResultRowSlot slot = state.CloneRows[last];
                state.CloneRows.RemoveAt(last);
                if (slot.RowRoot != null)
                {
                    UnityEngine.Object.Destroy(slot.RowRoot.gameObject);
                }
            }

            while (state.CloneRows.Count < requiredSlots)
            {
                int slotIndex = state.CloneRows.Count;
                Transform cloneRoot = UnityEngine.Object.Instantiate(template.RowRoot, gridRoot);
                cloneRoot.gameObject.SetActive(true);
                cloneRoot.name = $"SurvivalResultRow_{slotIndex + 1}";
                DisableLayoutDrivers(cloneRoot);
                if (!TryBindRow(cloneRoot, out SurvivalResultRowSlot slot))
                {
                    UnityEngine.Object.Destroy(cloneRoot.gameObject);
                    ModLog.Warn(Feature, $"SurvivalResult clone bind failed at slot {slotIndex}");
                    break;
                }

                state.CloneRows.Add(slot);
            }
        }

        private static void PlaceCloneRows(
            object ui,
            SurvivalResultGridState state,
            int displayCount,
            float[] rowHeights)
        {
            RectTransform gridRoot = EnsureGridRoot(ui, state);
            DisableLayoutDrivers(gridRoot);

            for (int i = 0; i < displayCount && i < state.CloneRows.Count; i++)
            {
                if (state.CloneRows[i].RowRoot is not RectTransform rowRect)
                {
                    continue;
                }

                int col = i % ColumnsPerRow;
                int row = i / ColumnsPerRow;
                float x = (col - ((ColumnsPerRow - 1) * 0.5f)) * state.ColStep;
                float y = GetRowCenterOffset(state.YDirection, rowHeights, row);

                rowRect.SetParent(gridRoot, false);
                rowRect.anchorMin = new Vector2(0.5f, 0.5f);
                rowRect.anchorMax = new Vector2(0.5f, 0.5f);
                rowRect.pivot = new Vector2(0.5f, 0.5f);
                rowRect.anchoredPosition = new Vector2(x, y);
                rowRect.gameObject.SetActive(true);
            }
        }

        private static float ResolveGridCenterX(object ui)
        {
            RectTransform? dialogRoot = GetDialogRoot(ui);
            if (dialogRoot == null)
            {
                return 0f;
            }

            Camera? cam = GetCanvasCamera(dialogRoot);
            Vector2 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.5f);
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(dialogRoot, screenCenter, cam, out Vector2 local)
                ? local.x
                : 0f;
        }

        private static void CompactRowLayout(SurvivalResultRowSlot slot, float cellWidth, out float width, out float height)
        {
            width = cellWidth;
            height = FallbackCellHeight;

            if (slot.RowRoot is not RectTransform rowRect)
            {
                return;
            }

            float y = 0f;

            if (slot.NameText.transform is RectTransform nameRect)
            {
                SetRectTopCenter(nameRect, y);
                float h = GetRectHeight(nameRect);
                y -= h + RowElementGap;
            }

            RectTransform? stateRect = GetActiveStateRect(slot);
            if (stateRect != null)
            {
                SetRectTopCenter(stateRect, y);
                float h = GetRectHeight(stateRect);
                y -= h + RowElementGap;
            }

            if (slot.AwardText.gameObject.activeSelf
                && slot.AwardText.transform is RectTransform awardRect
                && !string.IsNullOrWhiteSpace(GetText(slot.AwardText)))
            {
                SetRectTopCenter(awardRect, y);
                y -= GetRectHeight(awardRect);
            }

            height = Mathf.Max(FallbackCellHeight, Mathf.Abs(y) + RowElementGap);
            rowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            rowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cellWidth);
        }

        private static RectTransform? GetActiveStateRect(SurvivalResultRowSlot slot)
        {
            if (slot.SurvivalIcon.activeSelf)
            {
                return slot.SurvivalIcon.transform as RectTransform;
            }

            if (slot.KilledIcon.activeSelf)
            {
                return slot.KilledIcon.transform as RectTransform;
            }

            if (slot.WastedIcon.activeSelf)
            {
                return slot.WastedIcon.transform as RectTransform;
            }

            return null;
        }

        private static void SetRectTopCenter(RectTransform rect, float topY)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, topY);
        }

        private static float GetRectHeight(RectTransform rect) =>
            rect.rect.height > 1f ? rect.rect.height : rect.sizeDelta.y > 1f ? rect.sizeDelta.y : 14f;

        private static void PlaceLossSection(
            SurvivalResultGridState state,
            float[] rowHeights,
            bool lossVisible)
        {
            if (!lossVisible || rowHeights.Length == 0 || state.GridRoot == null)
            {
                return;
            }

            RectTransform gridRoot = state.GridRoot;
            int lastRow = rowHeights.Length - 1;
            float lastRowCenterY = GetRowCenterOffset(state.YDirection, rowHeights, lastRow);
            float lastRowEdge = lastRowCenterY + (state.YDirection * (rowHeights[lastRow] * 0.5f));
            float lossCenterY = lastRowEdge + (state.YDirection * LossGap);

            if (state.LostAllRect != null && state.LostAllRect.gameObject.activeSelf)
            {
                float height = state.LostAllRect.rect.height > 1f ? state.LostAllRect.rect.height : state.CellHeight;
                state.LostAllRect.SetParent(gridRoot, false);
                state.LostAllRect.anchorMin = new Vector2(0.5f, 0.5f);
                state.LostAllRect.anchorMax = new Vector2(0.5f, 0.5f);
                state.LostAllRect.pivot = new Vector2(0.5f, 0.5f);
                state.LostAllRect.anchoredPosition = new Vector2(0f, lossCenterY - (height * 0.5f));
            }

            if (state.LostScrabsRect != null && state.LostScrabsRect.gameObject.activeSelf)
            {
                float height = state.LostScrabsRect.rect.height > 1f ? state.LostScrabsRect.rect.height : state.CellHeight;
                state.LostScrabsRect.SetParent(gridRoot, false);
                state.LostScrabsRect.anchorMin = new Vector2(0.5f, 0.5f);
                state.LostScrabsRect.anchorMax = new Vector2(0.5f, 0.5f);
                state.LostScrabsRect.pivot = new Vector2(0.5f, 0.5f);
                state.LostScrabsRect.anchoredPosition = new Vector2(0f, lossCenterY - (height * 0.5f));
            }
        }

        private static bool TryBindRow(Transform rowRoot, out SurvivalResultRowSlot slot)
        {
            slot = default;
            Component? nameText = null;
            GameObject? survival = null;
            GameObject? killed = null;
            GameObject? wasted = null;
            Component? award = null;

            foreach (Component component in rowRoot.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    continue;
                }

                string name = component.name;
                if (name.EndsWith("Name", StringComparison.Ordinal) && nameText == null && HasSetText(component))
                {
                    nameText = component;
                }
                else if (name.EndsWith("Survival", StringComparison.Ordinal))
                {
                    survival = component.gameObject;
                }
                else if (name.EndsWith("Killed", StringComparison.Ordinal))
                {
                    killed = component.gameObject;
                }
                else if (name.EndsWith("Wasted", StringComparison.Ordinal))
                {
                    wasted = component.gameObject;
                }
                else if (name.EndsWith("Award", StringComparison.Ordinal) && award == null && HasSetText(component))
                {
                    award = component;
                }
            }

            if (nameText == null || survival == null || killed == null || wasted == null || award == null)
            {
                return false;
            }

            slot = new SurvivalResultRowSlot(rowRoot, nameText, survival, killed, wasted, award);
            return true;
        }

        private static void ResetSlot(SurvivalResultRowSlot slot)
        {
            slot.RowRoot.gameObject.SetActive(true);
            slot.SurvivalIcon.SetActive(false);
            slot.KilledIcon.SetActive(false);
            slot.WastedIcon.SetActive(false);
            SetText(slot.AwardText, string.Empty);
            slot.AwardText.gameObject.SetActive(false);
        }

        private static void PopulateRow(SurvivalResultRowSlot slot, object[] parameters, int index)
        {
            slot.RowRoot.gameObject.SetActive(true);
            int nameIndex = (3 * index) + 3;
            int stateIndex = (3 * index) + 4;
            int awardIndex = (3 * index) + 5;
            if (nameIndex >= parameters.Length || stateIndex >= parameters.Length || awardIndex >= parameters.Length)
            {
                return;
            }

            SetText(slot.NameText, parameters[nameIndex]?.ToString() ?? string.Empty);

            int survivalState = Convert.ToInt32(parameters[stateIndex]);
            switch (survivalState)
            {
                case 0:
                    slot.SurvivalIcon.SetActive(true);
                    break;
                case 1:
                    slot.WastedIcon.SetActive(true);
                    break;
                case 2:
                    slot.KilledIcon.SetActive(true);
                    break;
                default:
                    ModLog.Warn(Feature, $"Unknown survival state {survivalState} for {GetText(slot.NameText)}");
                    break;
            }

            try
            {
                ApplyAward(slot, (AwardType)parameters[awardIndex]);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SurvivalResult award failed — {ex.Message}");
            }
        }

        private static void ApplyAward(SurvivalResultRowSlot slot, AwardType awardType)
        {
            switch (awardType)
            {
                case AwardType.None:
                    SetText(slot.AwardText, string.Empty);
                    slot.AwardText.gameObject.SetActive(false);
                    break;
                case AwardType.BestCarryItem:
                    slot.AwardText.gameObject.SetActive(true);
                    SetText(slot.AwardText, GetL10NText("STRING_SETTLEMENT_REPORT_CASE_1"));
                    break;
                case AwardType.BestDamageToAlly:
                    slot.AwardText.gameObject.SetActive(true);
                    SetText(slot.AwardText, GetL10NText("STRING_SETTLEMENT_REPORT_CASE_2"));
                    break;
                case AwardType.BestMimicEncounter:
                    slot.AwardText.gameObject.SetActive(true);
                    SetText(slot.AwardText, GetL10NText("STRING_SETTLEMENT_REPORT_CASE_3"));
                    break;
                case AwardType.BestCamper:
                    slot.AwardText.gameObject.SetActive(true);
                    SetText(slot.AwardText, GetL10NText("STRING_SETTLEMENT_REPORT_CASE_4"));
                    break;
            }
        }

        private static void ApplyLostScrapSection(object ui, object[] parameters, ParsedParameters parsed)
        {
            if (!TryGetUiObject(ui, "UE_LostAllScrab", out GameObject? lostAllScrab) || lostAllScrab == null
                || !TryGetUiObject(ui, "UE_LostScrabs", out GameObject? lostScrabs) || lostScrabs == null)
            {
                return;
            }

            Component?[] scrabs =
            [
                TryGetUiText(ui, "Scrab1", out Component? s1) ? s1 : null,
                TryGetUiText(ui, "Scrab2", out Component? s2) ? s2 : null,
                TryGetUiText(ui, "Scrab3", out Component? s3) ? s3 : null,
            ];

            if (!parsed.Success)
            {
                lostScrabs.SetActive(false);
                lostAllScrab.SetActive(true);
                return;
            }

            lostAllScrab.SetActive(false);
            if (parsed.ScrapBaseIndex >= parameters.Length || parameters[parsed.ScrapBaseIndex] is not int scrapCount)
            {
                lostScrabs.SetActive(false);
                return;
            }

            if (scrapCount <= 0)
            {
                lostScrabs.SetActive(false);
                return;
            }

            lostScrabs.SetActive(true);
            int maxScrab = Math.Min(scrapCount, scrabs.Length);
            for (int j = 0; j < scrabs.Length; j++)
            {
                Component? scrab = scrabs[j];
                if (scrab == null)
                {
                    continue;
                }

                if (j >= maxScrab)
                {
                    scrab.gameObject.SetActive(false);
                    continue;
                }

                int keyIndex = parsed.ScrapBaseIndex + 1 + (j * 2);
                int amountIndex = keyIndex + 1;
                if (amountIndex >= parameters.Length)
                {
                    scrab.gameObject.SetActive(false);
                    continue;
                }

                string key = parameters[keyIndex]?.ToString() ?? string.Empty;
                int amount = parameters[amountIndex] is int amountValue ? amountValue : 0;
                scrab.gameObject.SetActive(true);
                SetText(scrab, $"\\ {GetL10NText(key),-15} : ${amount,-5}\n");
            }
        }

        private static void SetTitle(object ui, string text)
        {
            if (TryGetUiText(ui, "title", out Component? title) && title != null)
            {
                SetText(title, text);
            }
        }

        private static bool TryGetUiText(object ui, string propertyName, out Component? component)
        {
            component = null;
            PropertyInfo? property = AccessTools.Property(ui.GetType(), "UE_" + propertyName)
                ?? AccessTools.Property(ui.GetType(), propertyName);
            if (property?.GetValue(ui) is Component found)
            {
                component = found;
                return true;
            }

            return false;
        }

        private static bool TryGetUiObject(object ui, string propertyName, out GameObject? go)
        {
            go = null;
            PropertyInfo? property = AccessTools.Property(ui.GetType(), propertyName);
            object? value = property?.GetValue(ui);
            go = value switch
            {
                GameObject gameObject => gameObject,
                Component component => component.gameObject,
                _ => null,
            };
            return go != null;
        }

        private static bool TryGetUiImage(object ui, string propertyName, out Image? image)
        {
            image = null;
            PropertyInfo? property = AccessTools.Property(ui.GetType(), "UE_" + propertyName)
                ?? AccessTools.Property(ui.GetType(), propertyName);
            if (property?.GetValue(ui) is Image found)
            {
                image = found;
                return true;
            }

            return false;
        }

        private static bool HasSetText(Component component) =>
            component.GetType().GetMethod("SetText", [typeof(string)]) != null;

        private static void SetText(Component textComponent, string value)
        {
            MethodInfo? setText = textComponent.GetType().GetMethod("SetText", [typeof(string)]);
            if (setText != null)
            {
                _ = setText.Invoke(textComponent, [value]);
                return;
            }

            PropertyInfo? textProperty = textComponent.GetType().GetProperty("text");
            textProperty?.SetValue(textComponent, value);
        }

        private static string GetText(Component textComponent)
        {
            PropertyInfo? textProperty = textComponent.GetType().GetProperty("text");
            return textProperty?.GetValue(textComponent) as string ?? string.Empty;
        }

        private static string GetL10NText(string key, params object[] formattingArgs) =>
            GameLocaleAccess.GetL10NText(key, formattingArgs);

        private static object? GetRandomDungeonSeed()
        {
            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            if (pdata == null || RandDungeonSeedMember == null)
            {
                return string.Empty;
            }

            return RandDungeonSeedMember switch
            {
                FieldInfo field => field.GetValue(pdata),
                PropertyInfo property => property.GetValue(pdata),
                _ => string.Empty,
            };
        }
    }
}
