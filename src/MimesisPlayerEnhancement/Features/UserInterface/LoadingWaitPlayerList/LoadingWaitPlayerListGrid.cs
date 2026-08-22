using System.Reflection;
using HarmonyLib;
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
            RectTransform boundsRect,
            RectTransform flowRect,
            RectTransform shadeRect,
            out GridState state)
        {
            state = null!;
            if (boundsRect == null || flowRect == null || shadeRect == null)
            {
                return false;
            }

            Color liveColor = Color.white;
            Color deadColor = Color.red;
            ModUiAssets assets = ModUiAssets.Fallback;
            float fontSize = FallbackFontSize;

            if (TryResolveSpectatorTemplate(
                    out UIPrefab_Spectator_PlayerListView listView,
                    out UIPrefab_Spectator_PlayerListViewItem templateRow))
            {
                SpectatorPlayerRowBinder.CacheColors(listView, out liveColor, out deadColor);
                assets = ModUiAssets.FromTextSource(templateRow.gameObject);
                fontSize = ResolveFontSize(templateRow, FallbackFontSize);
            }

            ConfigureFlowRect(flowRect);
            Component? measureText = CreateMeasureText(flowRect, assets, fontSize);

            state = new GridState
            {
                BoundsRect = boundsRect,
                FlowRect = flowRect,
                ShadeRect = shadeRect,
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

        private static bool TryResolveSpectatorTemplate(
            out UIPrefab_Spectator_PlayerListView listView,
            out UIPrefab_Spectator_PlayerListViewItem templateRow)
        {
            listView = null!;
            templateRow = null!;

            UIPrefab_Spectator_PlayerListView[] views =
                UnityEngine.Object.FindObjectsByType<UIPrefab_Spectator_PlayerListView>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            for (int i = 0; i < views.Length; i++)
            {
                UIPrefab_Spectator_PlayerListView view = views[i];
                if (view == null)
                {
                    continue;
                }

                UIPrefab_Spectator_PlayerListViewItem[] rows =
                    view.GetComponentsInChildren<UIPrefab_Spectator_PlayerListViewItem>(includeInactive: true);
                if (rows is not { Length: > 0 })
                {
                    continue;
                }

                listView = view;
                templateRow = rows[0];
                return true;
            }

            return false;
        }

        internal static void Update(GridState state, IReadOnlyList<LoadingWaitPlayerEntry> players)
        {
            if (state.FlowRect == null || state.BoundsRect == null)
            {
                return;
            }

            RefreshLayoutMetrics(state);
            LoadingWaitPlayerListBandLayout.ApplyShadeRect(
                state.ShadeRect,
                state.LastBoundsWidth,
                state.LastBoundsHeight);
            EnsureSlots(state, players.Count);
            PackAndPositionSlots(state, players);

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
            flowRect.pivot = Vector2.zero;
            flowRect.anchoredPosition = Vector2.zero;
            flowRect.localScale = Vector3.one;
        }

        private static void RefreshLayoutMetrics(GridState state)
        {
            ResolveBoundsSize(state, out float width, out float height);
            float horizontalInset = LoadingWaitPlayerListBandLayout.ResolveHorizontalInset(width);
            state.LastBoundsWidth = width;
            state.LastBoundsHeight = height;
            state.LastAvailableWidth = Mathf.Max(width - (2f * horizontalInset), 32f);
        }

        private static void ResolveBoundsSize(GridState state, out float width, out float height)
        {
            Canvas.ForceUpdateCanvases();
            RectTransform boundsRect = state.BoundsRect;
            width = boundsRect.rect.width;
            height = boundsRect.rect.height;

            if ((width <= 1f || height <= 1f) && boundsRect.parent is RectTransform parentRect)
            {
                if (width <= 1f)
                {
                    width = parentRect.rect.width;
                }

                if (height <= 1f)
                {
                    height = parentRect.rect.height;
                }
            }

            if (width <= 1f)
            {
                width = Screen.width;
            }

            if (height <= 1f)
            {
                height = Screen.height;
            }
        }

        private static void PackAndPositionSlots(
            GridState state,
            IReadOnlyList<LoadingWaitPlayerEntry> players)
        {
            float boundsWidth = state.LastBoundsWidth;
            float availableWidth = state.LastAvailableWidth;
            LoadingWaitPlayerListBandLayout.ResolveBand(
                state.LastBoundsHeight,
                out float bandBottomY,
                out float bandHeight);

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
                rowRect.anchorMin = Vector2.zero;
                rowRect.anchorMax = Vector2.zero;
                rowRect.pivot = Vector2.zero;
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

        private static void PositionSlot(PlayerSlot slot, float itemWidth, float x, float rowHeight)
        {
            RectTransform rootRect = slot.RootRect;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.zero;
            rootRect.pivot = Vector2.zero;
            rootRect.anchoredPosition = new Vector2(x, 0f);
            rootRect.sizeDelta = new Vector2(itemWidth, rowHeight);
            rootRect.localScale = Vector3.one;

            ModUiLayout.Stretch(slot.NameRect);
            slot.NameRect.localScale = Vector3.one;
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
                state.RowRects.Add(rowObject.AddComponent<RectTransform>());
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
            measureRect.anchorMin = Vector2.zero;
            measureRect.anchorMax = Vector2.zero;
            measureRect.pivot = Vector2.zero;
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
            internal RectTransform ShadeRect = null!;
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
            internal float LastBoundsHeight;
        }
    }
}
