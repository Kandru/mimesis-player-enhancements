using System.Reflection;
using System.Threading;
using MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen;
using MimesisPlayerEnhancement.Ui;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList
{
    internal static class LoadingWaitPlayerListGrid
    {
        private const string Feature = "Ui";
        private const float HorizontalMargin = 16f;
        private const float BottomMargin = 16f;
        private const float RowGap = 4f;
        private const float MicGap = 4f;
        private const string CommaSpacing = ", ";
        private const float FallbackFontSize = 18f;
        private const float FallbackMicWidth = 18f;
        private const float FallbackRowHeight = 24f;

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
            float micWidth = ResolveMicWidth(templateRow, FallbackMicWidth);
            Component? commaMeasureText = CreateMeasureText(flowRect, assets, fontSize);

            state = new GridState
            {
                TemplateRow = templateRow,
                BoundsRect = boundsRect,
                FlowRect = flowRect,
                Assets = assets,
                FontSize = fontSize,
                MicWidth = micWidth,
                LiveColor = liveColor,
                DeadColor = deadColor,
                CommaMeasureText = commaMeasureText,
                CommaWidth = LoadingWaitPlayerListTextMeasure.MeasurePreferredSize(
                    commaMeasureText,
                    CommaSpacing,
                    fontSize).x,
                RowHeight = Mathf.Max(
                    LoadingWaitPlayerListTextMeasure.MeasurePreferredSize(commaMeasureText, "Ag", fontSize).y,
                    micWidth,
                    FallbackRowHeight),
            };

            return true;
        }

        internal static void Update(
            GridState state,
            Transform loadingRoot,
            IReadOnlyList<LoadingWaitPlayerEntry> players,
            CancellationToken cancellationToken)
        {
            if (state.TemplateRow == null || state.FlowRect == null || state.BoundsRect == null)
            {
                return;
            }

            ApplyContentBounds(state, loadingRoot);
            bool layoutChanged = RefreshLayoutIfNeeded(state, players);
            EnsureSlots(state, players.Count);

            if (layoutChanged)
            {
                PackAndPositionSlots(state, players);
            }

            for (int slotIndex = 0; slotIndex < state.Slots.Count; slotIndex++)
            {
                PlayerSlot slot = state.Slots[slotIndex];
                if (slotIndex >= players.Count)
                {
                    slot.Root.SetActive(false);
                    SpectatorPlayerRowBinder.TurnOffSpeakAnimation(slot.MicProxy);
                    continue;
                }

                slot.Root.SetActive(true);
                BindSlot(state, slot, players[slotIndex], cancellationToken);
            }
        }

        internal static void Destroy(GridState state)
        {
            foreach (PlayerSlot slot in state.Slots)
            {
                if (slot.MicProxy != null)
                {
                    SpectatorPlayerRowBinder.TurnOffSpeakAnimation(slot.MicProxy);
                }
            }

            DestroySlots(state);

            if (state.CommaMeasureText != null)
            {
                UnityEngine.Object.Destroy(state.CommaMeasureText.gameObject);
                state.CommaMeasureText = null;
            }
        }

        private static void ApplyContentBounds(GridState state, Transform loadingRoot)
        {
            RectTransform? parentRect = state.BoundsRect.parent as RectTransform;
            if (parentRect == null)
            {
                return;
            }

            float imageAspect = CustomLoadingScreenImageLayout.FallbackImageAspect;
            CustomLoadingScreenImageLayout.TryResolveImageAspect(loadingRoot, out imageAspect);
            CustomLoadingScreenImageLayout.ApplyContentBoundsInset(state.BoundsRect, parentRect, imageAspect);
        }

        private static bool RefreshLayoutIfNeeded(GridState state, IReadOnlyList<LoadingWaitPlayerEntry> players)
        {
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            float availableWidth = ResolveAvailableWidth(state);

            bool namesChanged = !NamesMatch(state, players);
            bool screenChanged = state.LastScreenWidth != screenWidth || state.LastScreenHeight != screenHeight;
            bool widthChanged = Mathf.Abs(state.LastAvailableWidth - availableWidth) > 0.5f;
            bool countChanged = state.LastPlayerCount != players.Count;

            if (!screenChanged && !widthChanged && !countChanged && !namesChanged)
            {
                return false;
            }

            state.LastScreenWidth = screenWidth;
            state.LastScreenHeight = screenHeight;
            state.LastAvailableWidth = availableWidth;
            state.LastPlayerCount = players.Count;
            state.LastNames = new string[players.Count];
            for (int index = 0; index < players.Count; index++)
            {
                state.LastNames[index] = players[index].DisplayName;
            }

            ModLog.Debug(
                Feature,
                $"Loading wait player list layout — players={players.Count}, availableWidth={availableWidth:F0}");

            return true;
        }

        private static bool NamesMatch(GridState state, IReadOnlyList<LoadingWaitPlayerEntry> players)
        {
            if (state.LastNames.Length != players.Count)
            {
                return false;
            }

            for (int index = 0; index < players.Count; index++)
            {
                if (!string.Equals(state.LastNames[index], players[index].DisplayName, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static float ResolveAvailableWidth(GridState state)
        {
            float boundsWidth = state.BoundsRect.rect.width;
            if (boundsWidth <= 1f)
            {
                boundsWidth = Screen.width;
            }

            return Mathf.Max(boundsWidth - (2f * HorizontalMargin), 32f);
        }

        private static void PackAndPositionSlots(GridState state, IReadOnlyList<LoadingWaitPlayerEntry> players)
        {
            float availableWidth = ResolveAvailableWidth(state);
            List<FlowRow> rows = [];
            FlowRow currentRow = new();
            float currentX = 0f;

            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                LoadingWaitPlayerEntry entry = players[playerIndex];
                float nameWidth = LoadingWaitPlayerListTextMeasure.MeasurePreferredSize(
                    state.Slots[playerIndex].NameText,
                    entry.DisplayName,
                    state.FontSize).x;
                float coreWidth = nameWidth + state.MicWidth + MicGap;
                bool hasFollowingPlayer = playerIndex < players.Count - 1;
                float itemWidth = coreWidth + (hasFollowingPlayer ? state.CommaWidth : 0f);

                if (currentRow.Slots.Count > 0 && currentX + itemWidth > availableWidth)
                {
                    rows.Add(currentRow);
                    currentRow = new FlowRow();
                    currentX = 0f;
                    hasFollowingPlayer = playerIndex < players.Count - 1;
                    itemWidth = coreWidth + (hasFollowingPlayer ? state.CommaWidth : 0f);
                }

                currentRow.Slots.Add(new FlowSlot
                {
                    SlotIndex = playerIndex,
                    NameWidth = nameWidth,
                });
                currentX += itemWidth;
            }

            if (currentRow.Slots.Count > 0)
            {
                rows.Add(currentRow);
            }

            float rowStep = state.RowHeight + RowGap;
            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                FlowRow row = rows[rowIndex];
                float rowY = BottomMargin + (rowIndex * rowStep);
                float rowX = 0f;
                float rowWidth = 0f;

                for (int slotIndex = 0; slotIndex < row.Slots.Count; slotIndex++)
                {
                    FlowSlot flowSlot = row.Slots[slotIndex];
                    bool showComma = slotIndex < row.Slots.Count - 1;
                    float itemWidth = flowSlot.NameWidth + state.MicWidth + MicGap
                        + (showComma ? state.CommaWidth : 0f);
                    flowSlot.X = rowX;
                    flowSlot.ShowComma = showComma;
                    flowSlot.ItemWidth = itemWidth;
                    rowX += itemWidth;
                }

                rowWidth = rowX;
                float startX = HorizontalMargin + Mathf.Max((availableWidth - rowWidth) * 0.5f, 0f);

                for (int slotIndex = 0; slotIndex < row.Slots.Count; slotIndex++)
                {
                    FlowSlot flowSlot = row.Slots[slotIndex];
                    PositionSlot(state.Slots[flowSlot.SlotIndex], flowSlot, startX, rowY);
                }
            }
        }

        private static void PositionSlot(PlayerSlot slot, FlowSlot flowSlot, float rowStartX, float rowY)
        {
            RectTransform rootRect = slot.RootRect;
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(0f, 0f);
            rootRect.pivot = new Vector2(0f, 0f);
            rootRect.anchoredPosition = new Vector2(rowStartX + flowSlot.X, rowY);
            rootRect.sizeDelta = new Vector2(flowSlot.ItemWidth, slot.StateRowHeight);

            RectTransform nameRect = slot.NameRect;
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(0f, 0f);
            nameRect.pivot = new Vector2(0f, 0f);
            nameRect.anchoredPosition = Vector2.zero;
            nameRect.sizeDelta = new Vector2(flowSlot.NameWidth, slot.StateRowHeight);

            RectTransform micRect = slot.MicRect;
            micRect.anchorMin = new Vector2(0f, 0.5f);
            micRect.anchorMax = new Vector2(0f, 0.5f);
            micRect.pivot = new Vector2(0f, 0.5f);
            micRect.anchoredPosition = new Vector2(flowSlot.NameWidth + MicGap, 0f);

            if (slot.CommaText != null && slot.CommaRect != null)
            {
                bool showComma = flowSlot.ShowComma;
                slot.CommaText.gameObject.SetActive(showComma);
                if (showComma)
                {
                    slot.CommaRect.anchorMin = new Vector2(0f, 0f);
                    slot.CommaRect.anchorMax = new Vector2(0f, 0f);
                    slot.CommaRect.pivot = new Vector2(0f, 0f);
                    float commaX = flowSlot.NameWidth + MicGap + slot.StateMicWidth;
                    slot.CommaRect.anchoredPosition = new Vector2(commaX, 0f);
                    slot.CommaRect.sizeDelta = new Vector2(slot.StateCommaWidth, slot.StateRowHeight);
                }
            }
        }

        private static void BindSlot(
            GridState state,
            PlayerSlot slot,
            LoadingWaitPlayerEntry entry,
            CancellationToken cancellationToken)
        {
            Color color = entry.Loaded ? state.LiveColor : state.DeadColor;
            ModUiText.SetText(slot.NameText, entry.DisplayName);
            ModUiText.SetColor(slot.NameText, color);
            SpectatorPlayerRowBinder.TrySetRowColor(slot.MicProxy, color);
            SpectatorPlayerRowBinder.BindSpeakState(slot.MicProxy, entry.Speaking, cancellationToken);
            SpectatorPlayerRowBinder.SetPossessorActive(slot.MicProxy, false);
        }

        private static void EnsureSlots(GridState state, int requiredCount)
        {
            while (state.Slots.Count > requiredCount)
            {
                int lastIndex = state.Slots.Count - 1;
                PlayerSlot slot = state.Slots[lastIndex];
                state.Slots.RemoveAt(lastIndex);
                if (slot.MicProxy != null)
                {
                    SpectatorPlayerRowBinder.TurnOffSpeakAnimation(slot.MicProxy);
                }

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

            UIPrefab_Spectator_PlayerListViewItem micProxy =
                UnityEngine.Object.Instantiate(state.TemplateRow, root.transform);
            micProxy.gameObject.name = "MicProxy";
            HideMicProxyName(micProxy);
            HideMicProxyVisuals(micProxy);
            SpectatorPlayerRowBinder.SetPossessorActive(micProxy, false);
            SpectatorPlayerRowBinder.TurnOffSpeakAnimation(micProxy);

            RectTransform micRect = micProxy.transform as RectTransform ?? micProxy.gameObject.AddComponent<RectTransform>();
            Image? speakIcon = micProxy.SpeakIcon;
            if (speakIcon != null)
            {
                speakIcon.gameObject.SetActive(true);
                micRect = speakIcon.rectTransform;
            }

            GameObject commaObject = new("Comma");
            commaObject.transform.SetParent(root.transform, worldPositionStays: false);
            RectTransform commaRect = commaObject.AddComponent<RectTransform>();
            Component commaText = ModUiFactory.AddText(
                commaObject,
                state.Assets,
                CommaSpacing,
                state.FontSize,
                ModUiFontStyle.Normal);
            ModUiText.SetColor(commaText, state.LiveColor);
            ModUiText.ConfigureTextLayout(commaText, wordWrap: false, ModUiText.OverflowOverflow);
            ModUiText.ConfigureTightSingleLine(commaText);
            SetBottomLeftAlignment(commaText);

            return new PlayerSlot
            {
                Root = root,
                RootRect = rootRect,
                NameText = nameText,
                NameRect = nameRect,
                MicProxy = micProxy,
                MicRect = micRect,
                CommaText = commaText,
                CommaRect = commaRect,
                StateRowHeight = state.RowHeight,
                StateMicWidth = state.MicWidth,
                StateCommaWidth = state.CommaWidth,
            };
        }

        private static Component CreateMeasureText(Transform parent, ModUiAssets assets, float fontSize)
        {
            GameObject measureObject = new("MeasureText");
            measureObject.transform.SetParent(parent, worldPositionStays: false);
            measureObject.SetActive(false);
            Component measureText = ModUiFactory.AddText(
                measureObject,
                assets,
                string.Empty,
                fontSize,
                ModUiFontStyle.Normal);
            ModUiText.ConfigureTextLayout(measureText, wordWrap: false, ModUiText.OverflowOverflow);
            ModUiText.ConfigureTightSingleLine(measureText);
            return measureText;
        }

        private static void HideMicProxyName(UIPrefab_Spectator_PlayerListViewItem micProxy)
        {
            PropertyInfo? nameTextProperty =
                AccessTools.Property(typeof(UIPrefab_Spectator_PlayerListViewItem), "UE_Name_Text");
            if (nameTextProperty?.GetValue(micProxy) is Component nameText)
            {
                nameText.gameObject.SetActive(false);
            }

            SpectatorPlayerRowBinder.SetRowName(micProxy, string.Empty);
        }

        private static void HideMicProxyVisuals(UIPrefab_Spectator_PlayerListViewItem micProxy)
        {
            Image? speakIcon = micProxy.SpeakIcon;
            Transform? speakTransform = speakIcon != null ? speakIcon.transform : null;
            foreach (Graphic graphic in micProxy.GetComponentsInChildren<Graphic>(includeInactive: true))
            {
                if (speakTransform != null
                    && (graphic.transform == speakTransform || graphic.transform.IsChildOf(speakTransform)))
                {
                    continue;
                }

                graphic.gameObject.SetActive(false);
            }
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
                    return fontSize;
                }
            }

            return fallback;
        }

        private static float ResolveMicWidth(UIPrefab_Spectator_PlayerListViewItem templateRow, float fallback)
        {
            Image? speakIcon = templateRow.SpeakIcon;
            if (speakIcon == null)
            {
                return fallback;
            }

            RectTransform micRect = speakIcon.rectTransform;
            float width = micRect.rect.width;
            return width > 1f ? width : fallback;
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

        private sealed class FlowRow
        {
            internal List<FlowSlot> Slots = [];
        }

        private sealed class FlowSlot
        {
            internal int SlotIndex;
            internal float X;
            internal float NameWidth;
            internal bool ShowComma;
            internal float ItemWidth;
        }

        internal sealed class PlayerSlot
        {
            internal GameObject Root = null!;
            internal RectTransform RootRect = null!;
            internal Component NameText = null!;
            internal RectTransform NameRect = null!;
            internal UIPrefab_Spectator_PlayerListViewItem MicProxy = null!;
            internal RectTransform MicRect = null!;
            internal Component? CommaText;
            internal RectTransform? CommaRect;
            internal float StateRowHeight;
            internal float StateMicWidth;
            internal float StateCommaWidth;
        }

        internal sealed class GridState
        {
            internal UIPrefab_Spectator_PlayerListViewItem TemplateRow = null!;
            internal RectTransform BoundsRect = null!;
            internal RectTransform FlowRect = null!;
            internal ModUiAssets Assets = ModUiAssets.Fallback;
            internal Component? CommaMeasureText;
            internal List<PlayerSlot> Slots = [];
            internal Color LiveColor = Color.white;
            internal Color DeadColor = Color.red;
            internal float FontSize = FallbackFontSize;
            internal float MicWidth = FallbackMicWidth;
            internal float CommaWidth;
            internal float RowHeight = FallbackRowHeight;
            internal int LastScreenWidth;
            internal int LastScreenHeight;
            internal float LastAvailableWidth;
            internal int LastPlayerCount = -1;
            internal string[] LastNames = [];
        }
    }
}
