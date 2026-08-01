using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList
{
    internal static class LoadingWaitPlayerListGrid
    {
        private const string Feature = "Ui";
        private const float RowGap = 10f;
        private const float FallbackFontSize = 20f;
        private const float FallbackRowHeight = 26f;
        private static readonly Color SpeakingColor = new(0.35f, 0.95f, 0.45f, 1f);

        internal static bool TryInitialize(
            UIPrefab_Spectator_PlayerListView listView,
            RectTransform boundsRect,
            RectTransform flowRect,
            out GridState state)
        {
            state = null!;
            UIPrefab_Spectator_PlayerListViewItem[] rows =
                listView.GetComponentsInChildren<UIPrefab_Spectator_PlayerListViewItem>(includeInactive: true);
            if (rows == null || rows.Length == 0)
            {
                return false;
            }

            UIPrefab_Spectator_PlayerListViewItem templateRow = rows[0];
            SpectatorPlayerRowBinder.CacheColors(listView, out Color liveColor, out Color deadColor);

            ModUiAssets assets = ModUiAssets.FromTextSource(templateRow.gameObject);
            float fontSize = ResolveFontSize(templateRow, FallbackFontSize);
            ConfigureFlowRect(flowRect);
            Component? measureText = CreateMeasureText(flowRect, assets, fontSize);

            state = new GridState
            {
                BoundsRect = boundsRect,
                FlowRect = flowRect,
                Assets = assets,
                FontSize = fontSize,
                LiveColor = liveColor,
                DeadColor = deadColor,
                MeasureText = measureText,
                RowHeight = Mathf.Max(
                    LoadingWaitPlayerListTextMeasure.MeasurePreferredSize(measureText, "Ag", fontSize).y,
                    FallbackRowHeight),
            };

            return true;
        }

        internal static void Update(
            GridState state,
            Transform loadingRoot,
            IReadOnlyList<LoadingWaitPlayerEntry> players)
        {
            if (state.FlowRect == null || state.BoundsRect == null)
            {
                return;
            }

            ApplyContentBounds(state, loadingRoot);
            RefreshLayoutMetrics(state, loadingRoot);
            EnsureSlots(state, players.Count);

            // Always re-pack: speaking/color bind is cheap; positions must stay correct
            // after bounds/band settle across the first frames of the wait screen.
            PackAndPositionSlots(state, loadingRoot, players);

            for (int slotIndex = 0; slotIndex < state.Slots.Count; slotIndex++)
            {
                PlayerSlot slot = state.Slots[slotIndex];
                if (slotIndex >= players.Count)
                {
                    slot.Root.SetActive(false);
                    continue;
                }

                slot.Root.SetActive(true);
                BindSlot(state, slot, players[slotIndex]);
            }
        }

        internal static void Destroy(GridState state)
        {
            DestroyRowContainers(state);
            DestroySlots(state);

            if (state.MeasureText != null)
            {
                UnityEngine.Object.Destroy(state.MeasureText.gameObject);
                state.MeasureText = null;
            }
        }

        private static void ConfigureFlowRect(RectTransform flowRect)
        {
            flowRect.anchorMin = Vector2.zero;
            flowRect.anchorMax = Vector2.one;
            flowRect.offsetMin = Vector2.zero;
            flowRect.offsetMax = Vector2.zero;
            flowRect.pivot = new Vector2(0f, 0f);
            flowRect.anchoredPosition = Vector2.zero;
            flowRect.localScale = Vector3.one;
        }

        private static void ApplyContentBounds(GridState state, Transform loadingRoot)
        {
            RectTransform? layoutParent = ResolveLayoutParent(loadingRoot, state.BoundsRect);
            if (layoutParent == null)
            {
                return;
            }

            float imageAspect = CustomLoadingScreenImageLayout.FallbackImageAspect;
            CustomLoadingScreenImageLayout.TryResolveImageAspect(loadingRoot, out imageAspect);
            CustomLoadingScreenImageLayout.ApplyContentBoundsInset(state.BoundsRect, layoutParent, imageAspect);
            ConfigureFlowRect(state.FlowRect);
        }

        private static RectTransform? ResolveLayoutParent(Transform loadingRoot, RectTransform boundsRect)
        {
            if (loadingRoot != null)
            {
                Transform? overlay = loadingRoot.Find(CustomLoadingScreenConstants.OverlayObjectName);
                if (overlay is RectTransform overlayRect)
                {
                    return overlayRect;
                }
            }

            return boundsRect.parent as RectTransform;
        }

        private static void RefreshLayoutMetrics(GridState state, Transform loadingRoot)
        {
            float boundsWidth = ResolveBoundsWidth(state, loadingRoot);
            float horizontalInset = CustomLoadingScreenImageLayout.ResolveWaitPlayerBandHorizontalInset(boundsWidth);
            state.LastBoundsWidth = boundsWidth;
            state.LastAvailableWidth = Mathf.Max(boundsWidth - (2f * horizontalInset), 32f);
        }

        private static float ResolveBoundsWidth(GridState state, Transform loadingRoot)
        {
            if (CustomLoadingScreenImageLayout.TryResolveContentWidth(
                    loadingRoot,
                    state.BoundsRect,
                    out float contentWidth))
            {
                return contentWidth;
            }

            Canvas.ForceUpdateCanvases();
            RectTransform boundsRect = state.BoundsRect;
            float width = boundsRect.rect.width;
            if (width > 1f)
            {
                return width;
            }

            if (boundsRect.parent is RectTransform parentRect)
            {
                width = parentRect.rect.width + boundsRect.offsetMin.x + boundsRect.offsetMax.x;
                if (width > 1f)
                {
                    return width;
                }
            }

            return Screen.width;
        }

        private static void PackAndPositionSlots(
            GridState state,
            Transform loadingRoot,
            IReadOnlyList<LoadingWaitPlayerEntry> players)
        {
            float boundsWidth = state.LastBoundsWidth;
            float availableWidth = state.LastAvailableWidth;
            CustomLoadingScreenImageLayout.ResolveWaitPlayerBand(
                state.BoundsRect,
                loadingRoot,
                out float bandBottomY,
                out float bandHeight);
            bandHeight = ResolveBandHeight(state, bandHeight);

            LoadingWaitLayoutMetrics metrics = LoadingWaitPlayerListLayout.Resolve(
                state.MeasureText,
                players,
                availableWidth,
                bandHeight,
                RowGap,
                state.FontSize,
                state.RowHeight);
            EnsureRowContainers(state, metrics.Rows.Count);

            int rowCount = metrics.Rows.Count;
            ModLog.Debug(
                Feature,
                $"Loading wait player list rows — count={rowCount}, rowHeight={metrics.RowHeight:F1}, bandY={bandBottomY:F1}, bandH={bandHeight:F1}");

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                LoadingWaitLayoutRow row = metrics.Rows[rowIndex];
                float rowY = LoadingWaitPlayerListLayout.ResolveRowY(
                    rowIndex,
                    rowCount,
                    metrics.RowHeight,
                    RowGap,
                    bandBottomY,
                    bandHeight);
                float rowWidth = LoadingWaitPlayerListLayout.MeasureRowWidth(
                    state.MeasureText,
                    players,
                    row.StartIndex,
                    row.Count,
                    state.FontSize);
                float startX = (boundsWidth - rowWidth) * 0.5f;

                RectTransform rowRect = state.RowRects[rowIndex];
                rowRect.gameObject.SetActive(true);
                rowRect.anchorMin = new Vector2(0f, 0f);
                rowRect.anchorMax = new Vector2(0f, 0f);
                rowRect.pivot = new Vector2(0f, 0f);
                rowRect.anchoredPosition = new Vector2(0f, rowY);
                rowRect.sizeDelta = new Vector2(boundsWidth, metrics.RowHeight);

                float rowX = startX;
                for (int offset = 0; offset < row.Count; offset++)
                {
                    int playerIndex = row.StartIndex + offset;
                    PlayerSlot slot = state.Slots[playerIndex];
                    float itemWidth = LoadingWaitPlayerListLayout.MeasureItemWidth(
                        state.MeasureText,
                        players[playerIndex].DisplayName,
                        state.FontSize);

                    slot.Root.transform.SetParent(rowRect, worldPositionStays: false);
                    PositionSlot(slot, itemWidth, rowX, metrics.RowHeight);
                    rowX += itemWidth + LoadingWaitPlayerListLayout.PlayerGap;
                }
            }

            for (int rowIndex = rowCount; rowIndex < state.RowRects.Count; rowIndex++)
            {
                state.RowRects[rowIndex].gameObject.SetActive(false);
            }
        }

        private static float ResolveBandHeight(GridState state, float bandHeight)
        {
            if (bandHeight > 0.5f)
            {
                return bandHeight;
            }

            float boundsHeight = state.BoundsRect.rect.height;
            if (boundsHeight < 0.5f && state.BoundsRect.parent is RectTransform parentRect)
            {
                boundsHeight = parentRect.rect.height;
            }

            if (boundsHeight < 0.5f)
            {
                boundsHeight = Screen.height;
            }

            return CustomLoadingScreenImageLayout.ResolveWaitPlayerBandFallbackHeight(boundsHeight);
        }

        private static void PositionSlot(PlayerSlot slot, float itemWidth, float x, float rowHeight)
        {
            RectTransform rootRect = slot.RootRect;
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(0f, 0f);
            rootRect.pivot = new Vector2(0f, 0f);
            rootRect.anchoredPosition = new Vector2(x, 0f);
            rootRect.sizeDelta = new Vector2(itemWidth, rowHeight);
            rootRect.localScale = Vector3.one;

            RectTransform nameRect = slot.NameRect;
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = Vector2.one;
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            nameRect.anchoredPosition = Vector2.zero;
            nameRect.sizeDelta = Vector2.zero;
            nameRect.localScale = Vector3.one;
        }

        private static void BindSlot(GridState state, PlayerSlot slot, LoadingWaitPlayerEntry entry)
        {
            ModUiText.SetText(slot.NameText, entry.DisplayName);
            ModUiText.SetColor(slot.NameText, ResolveNameColor(state, entry));
        }

        private static Color ResolveNameColor(GridState state, LoadingWaitPlayerEntry entry)
        {
            if (entry.Speaking)
            {
                return SpeakingColor;
            }

            return entry.Loaded ? state.LiveColor : state.DeadColor;
        }

        private static void EnsureRowContainers(GridState state, int requiredCount)
        {
            while (state.RowRects.Count < requiredCount)
            {
                int index = state.RowRects.Count;
                GameObject rowObject = new($"LoadingWaitPlayerRow_{index + 1}");
                rowObject.transform.SetParent(state.FlowRect, worldPositionStays: false);
                RectTransform rowRect = rowObject.AddComponent<RectTransform>();
                state.RowRects.Add(rowRect);
            }
        }

        private static void EnsureSlots(GridState state, int requiredCount)
        {
            while (state.Slots.Count > requiredCount)
            {
                int lastIndex = state.Slots.Count - 1;
                PlayerSlot slot = state.Slots[lastIndex];
                state.Slots.RemoveAt(lastIndex);
                if (slot.Root != null)
                {
                    UnityEngine.Object.Destroy(slot.Root);
                }
            }

            while (state.Slots.Count < requiredCount)
            {
                state.Slots.Add(CreateSlot(state, state.Slots.Count));
            }
        }

        private static PlayerSlot CreateSlot(GridState state, int slotIndex)
        {
            GameObject root = new($"LoadingWaitPlayerSlot_{slotIndex + 1}");
            root.transform.SetParent(state.FlowRect, worldPositionStays: false);
            RectTransform rootRect = root.AddComponent<RectTransform>();

            GameObject nameObject = new("Name");
            nameObject.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform nameRect = nameObject.AddComponent<RectTransform>();
            Component nameText = ModUiFactory.AddText(
                nameObject,
                state.Assets,
                string.Empty,
                state.FontSize,
                ModUiFontStyle.Normal);
            ModUiText.ConfigureTextLayout(nameText, wordWrap: false, ModUiText.OverflowOverflow);
            ModUiText.ConfigureTightSingleLine(nameText);
            SetBottomLeftAlignment(nameText);

            return new PlayerSlot
            {
                Root = root,
                RootRect = rootRect,
                NameText = nameText,
                NameRect = nameRect,
            };
        }

        private static Component CreateMeasureText(Transform parent, ModUiAssets assets, float fontSize)
        {
            GameObject measureObject = new("MeasureText");
            measureObject.transform.SetParent(parent, worldPositionStays: false);
            RectTransform measureRect = measureObject.AddComponent<RectTransform>();
            measureRect.anchorMin = new Vector2(0f, 0f);
            measureRect.anchorMax = new Vector2(0f, 0f);
            measureRect.pivot = new Vector2(0f, 0f);
            measureRect.anchoredPosition = new Vector2(-10000f, -10000f);
            measureRect.sizeDelta = new Vector2(2048f, 128f);

            Component measureText = ModUiFactory.AddText(
                measureObject,
                assets,
                string.Empty,
                fontSize,
                ModUiFontStyle.Normal);
            ModUiText.ConfigureTextLayout(measureText, wordWrap: false, ModUiText.OverflowOverflow);
            ModUiText.ConfigureTightSingleLine(measureText);

            // Keep active so TMP GetPreferredValues returns real widths; park off-screen.
            CanvasGroup canvasGroup = measureObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            return measureText;
        }

        private static void DestroyRowContainers(GridState state)
        {
            foreach (RectTransform rowRect in state.RowRects)
            {
                if (rowRect != null)
                {
                    UnityEngine.Object.Destroy(rowRect.gameObject);
                }
            }

            state.RowRects.Clear();
        }

        private static void DestroySlots(GridState state)
        {
            foreach (PlayerSlot slot in state.Slots)
            {
                if (slot.Root != null)
                {
                    UnityEngine.Object.Destroy(slot.Root);
                }
            }

            state.Slots.Clear();
        }

        private static float ResolveFontSize(UIPrefab_Spectator_PlayerListViewItem templateRow, float fallback)
        {
            PropertyInfo? nameTextProperty =
                AccessTools.Property(typeof(UIPrefab_Spectator_PlayerListViewItem), "UE_Name_Text");
            if (nameTextProperty?.GetValue(templateRow) is Component nameText)
            {
                PropertyInfo? sizeProperty = nameText.GetType().GetProperty(
                    "fontSize",
                    BindingFlags.Instance | BindingFlags.Public);
                if (sizeProperty?.GetValue(nameText) is float fontSize && fontSize > 0.5f)
                {
                    return fontSize + 2f;
                }
            }

            return fallback;
        }

        private static void SetBottomLeftAlignment(Component? textComponent)
        {
            if (textComponent == null)
            {
                return;
            }

            PropertyInfo? alignmentProperty = textComponent.GetType().GetProperty(
                "alignment",
                BindingFlags.Instance | BindingFlags.Public);
            if (alignmentProperty == null || !alignmentProperty.PropertyType.IsEnum)
            {
                return;
            }

            try
            {
                object value = Enum.Parse(alignmentProperty.PropertyType, "BottomLeft");
                alignmentProperty.SetValue(textComponent, value, null);
            }
            catch (ArgumentException)
            {
                /* unsupported alignment name */
            }
        }

        internal sealed class PlayerSlot
        {
            internal GameObject Root = null!;
            internal RectTransform RootRect = null!;
            internal Component NameText = null!;
            internal RectTransform NameRect = null!;
        }

        internal sealed class GridState
        {
            internal RectTransform BoundsRect = null!;
            internal RectTransform FlowRect = null!;
            internal ModUiAssets Assets = ModUiAssets.Fallback;
            internal Component? MeasureText;
            internal List<PlayerSlot> Slots = [];
            internal List<RectTransform> RowRects = [];
            internal Color LiveColor = Color.white;
            internal Color DeadColor = Color.red;
            internal float FontSize = FallbackFontSize;
            internal float RowHeight = FallbackRowHeight;
            internal float LastAvailableWidth;
            internal float LastBoundsWidth;
        }
    }
}
