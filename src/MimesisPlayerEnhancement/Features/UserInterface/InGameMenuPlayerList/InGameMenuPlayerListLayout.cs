using System.Reflection;
using MimesisPlayerEnhancement.Features.MorePlayers;
using MimesisPlayerEnhancement.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.UserInterface.InGameMenuPlayerList
{
    internal static class InGameMenuPlayerListLayout
    {
        private const string Feature = "Ui";
        private const string ScrollRootName = "InGameMenuPlayerListScroll";
        private const float TopGapPx = 12f;
        private const float BottomGapPx = 12f;
        private const float SectionGapPx = 8f;

        private static readonly Dictionary<int, MenuLayoutState> States = [];

        private static readonly PropertyInfo? VersionTextProperty =
            AccessTools.Property(typeof(UIPrefab_InGameMenu), "UE_versionText");

        internal static bool IsEnabled() =>
            ModConfig.EnableExtendedInGameMenuPlayerList.Value;

        internal static void Apply(UIPrefab_InGameMenu menu)
        {
            if (!IsEnabled()
                || menu.playerUIElements == null
                || menu.playerUIElements.Count == 0
                || menu.playerUIElements[0].container == null)
            {
                return;
            }

            try
            {
                RectTransform? columnParent = ResolveColumnParent(menu);
                if (columnParent == null)
                {
                    return;
                }

                float topReserved = LayoutJoinCodeTop(menu);
                float bottomReserved = LayoutBottomStack(menu);
                ApplyScrollArea(menu, columnParent, topReserved, bottomReserved);
            }
            catch (System.Exception ex)
            {
                ModLog.Warn(Feature, $"In-game menu player list layout failed — {ex.Message}");
            }
        }

        private static RectTransform? FindScrollRoot(UIPrefab_InGameMenu menu, RectTransform columnParent)
        {
            Transform? directChild = columnParent.Find(ScrollRootName);
            if (directChild != null)
            {
                return directChild.GetComponent<RectTransform>();
            }

            foreach (Transform child in columnParent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == ScrollRootName)
                {
                    return child.GetComponent<RectTransform>();
                }
            }

            _ = menu;
            return null;
        }

        private static RectTransform? ResolveColumnParent(UIPrefab_InGameMenu menu)
        {
            Transform joinCode = menu.UE_InviteRinkCopy.transform;
            RectTransform? versionText = GetVersionTextRect(menu);
            if (versionText == null)
            {
                return null;
            }

            Transform playerRow = menu.playerUIElements[0].container.transform;
            Transform? candidate = playerRow.parent;

            while (candidate != null)
            {
                if (joinCode.IsChildOf(candidate) && versionText.IsChildOf(candidate))
                {
                    return candidate as RectTransform;
                }

                candidate = candidate.parent;
            }

            return playerRow.parent as RectTransform;
        }

        private static float LayoutJoinCodeTop(UIPrefab_InGameMenu menu)
        {
            RectTransform joinRect = menu.UE_InviteRinkCopy.GetComponent<RectTransform>();
            joinRect.anchorMin = new Vector2(joinRect.anchorMin.x, 1f);
            joinRect.anchorMax = new Vector2(joinRect.anchorMax.x, 1f);
            joinRect.pivot = new Vector2(joinRect.pivot.x, 1f);
            joinRect.anchoredPosition = new Vector2(joinRect.anchoredPosition.x, -TopGapPx);
            LayoutRebuilder.ForceRebuildLayoutImmediate(joinRect);
            return TopGapPx + joinRect.rect.height + SectionGapPx;
        }

        private static float LayoutBottomStack(UIPrefab_InGameMenu menu)
        {
            List<RectTransform> entries = [];
            if (menu.UE_PublicRoom.gameObject.activeInHierarchy)
            {
                entries.Add(menu.UE_PublicRoom.GetComponent<RectTransform>());
            }

            if (menu.UE_RoomPassword.gameObject.activeInHierarchy)
            {
                entries.Add(menu.UE_RoomPassword.GetComponent<RectTransform>());
            }

            RectTransform? versionRect = GetVersionTextRect(menu);
            if (versionRect != null)
            {
                entries.Add(versionRect);
            }

            entries.Reverse();

            float y = BottomGapPx;
            foreach (RectTransform rect in entries)
            {
                rect.anchorMin = new Vector2(rect.anchorMin.x, 0f);
                rect.anchorMax = new Vector2(rect.anchorMax.x, 0f);
                rect.pivot = new Vector2(rect.pivot.x, 0f);
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
                y += rect.rect.height + SectionGapPx;
            }

            return y;
        }

        private static void ApplyScrollArea(
            UIPrefab_InGameMenu menu,
            RectTransform columnParent,
            float topReserved,
            float bottomReserved)
        {
            Transform firstRow = menu.playerUIElements[0].container.transform;
            if (firstRow.parent is not RectTransform contentHost)
            {
                return;
            }

            MenuLayoutState state = GetOrCreateState(menu, columnParent);
            float rowHeight = InGameMenuPlayerGrid.MeasureRowHeight(menu);
            float contentHeight = rowHeight * menu.playerUIElements.Count;
            InGameMenuPlayerGrid.ApplyRowLayoutElements(menu, rowHeight);

            if (!state.ScrollWrapped)
            {
                if (contentHost.parent != null && contentHost.parent.name == "Viewport")
                {
                    state.ScrollWrapped = true;
                    state.ScrollRoot = contentHost.parent.parent as RectTransform;
                    state.ContentHost = contentHost;
                }
                else
                {
                    WrapContentInScroll(contentHost, columnParent, state);
                }
            }

            if (state.ScrollRoot == null)
            {
                return;
            }

            RectTransform scrollRoot = state.ScrollRoot;
            float anchorMinX = state.OriginalAnchorMinX;
            float anchorMaxX = state.OriginalAnchorMaxX;
            scrollRoot.anchorMin = new Vector2(anchorMinX, 0f);
            scrollRoot.anchorMax = new Vector2(anchorMaxX, 1f);
            scrollRoot.pivot = new Vector2(0.5f, 0.5f);
            scrollRoot.offsetMin = new Vector2(0f, bottomReserved);
            scrollRoot.offsetMax = new Vector2(0f, -topReserved);

            if (state.ContentHost != null)
            {
                state.ContentHost.sizeDelta = new Vector2(0f, contentHeight);
                LayoutRebuilder.ForceRebuildLayoutImmediate(state.ContentHost);
            }
        }

        private static void WrapContentInScroll(
            RectTransform contentHost,
            RectTransform columnParent,
            MenuLayoutState state)
        {
            Vector2 anchorMin = contentHost.anchorMin;
            Vector2 anchorMax = contentHost.anchorMax;
            state.OriginalAnchorMinX = anchorMin.x;
            state.OriginalAnchorMaxX = anchorMax.x;

            GameObject scrollRootGo = new(ScrollRootName, typeof(RectTransform));
            RectTransform scrollRoot = scrollRootGo.GetComponent<RectTransform>();
            scrollRoot.SetParent(columnParent, false);

            Image scrollHitTarget = scrollRootGo.AddComponent<Image>();
            scrollHitTarget.color = Color.clear;
            scrollHitTarget.raycastTarget = true;

            ScrollRect scrollRect = scrollRootGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 20f;
            scrollRect.inertia = true;

            Scrollbar verticalScrollbar = ModScrollList.CreateVerticalScrollbar(
                scrollRoot,
                ModScrollList.DefaultScrollbarWidth);
            scrollRect.verticalScrollbar = verticalScrollbar;
            scrollRect.verticalScrollbarSpacing = ModScrollList.DefaultScrollbarSpacing;

            GameObject viewportGo = new("Viewport", typeof(RectTransform), typeof(RectMask2D));
            RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.SetParent(scrollRoot, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = new Vector2(
                -(ModScrollList.DefaultScrollbarWidth + ModScrollList.DefaultScrollbarSpacing),
                0f);

            contentHost.SetParent(viewportRect, false);
            contentHost.anchorMin = new Vector2(0f, 1f);
            contentHost.anchorMax = new Vector2(1f, 1f);
            contentHost.pivot = new Vector2(0.5f, 1f);
            contentHost.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = contentHost.gameObject.GetComponent<VerticalLayoutGroup>()
                ?? contentHost.gameObject.AddComponent<VerticalLayoutGroup>();
            PropertyInfo? alignmentProperty = typeof(VerticalLayoutGroup).GetProperty("childAlignment");
            if (alignmentProperty != null)
            {
                alignmentProperty.SetValue(layout, Enum.ToObject(alignmentProperty.PropertyType, 1));
            }

            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 0f;

            ContentSizeFitter fitter = contentHost.gameObject.GetComponent<ContentSizeFitter>()
                ?? contentHost.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentHost;

            state.ScrollWrapped = true;
            state.ScrollRoot = scrollRoot;
            state.ContentHost = contentHost;
            ModLog.Debug(Feature, "In-game menu player list scroll layout applied.");
        }

        private static MenuLayoutState GetOrCreateState(UIPrefab_InGameMenu menu, RectTransform columnParent)
        {
            int menuId = menu.GetInstanceID();
            if (!States.TryGetValue(menuId, out MenuLayoutState? state))
            {
                state = new MenuLayoutState();
                States[menuId] = state;
            }

            if (!state.ScrollWrapped)
            {
                RectTransform? existingScroll = FindScrollRoot(menu, columnParent);
                if (existingScroll != null)
                {
                    state.ScrollWrapped = true;
                    state.ScrollRoot = existingScroll;
                    state.OriginalAnchorMinX = existingScroll.anchorMin.x;
                    state.OriginalAnchorMaxX = existingScroll.anchorMax.x;
                    Transform? viewport = existingScroll.Find("Viewport");
                    if (viewport != null && viewport.childCount > 0)
                    {
                        state.ContentHost = viewport.GetChild(0).GetComponent<RectTransform>();
                    }
                }
            }

            return state;
        }

        private static RectTransform? GetVersionTextRect(UIPrefab_InGameMenu menu)
        {
            if (VersionTextProperty?.GetValue(menu) is Component component)
            {
                return component.GetComponent<RectTransform>();
            }

            return null;
        }

        private sealed class MenuLayoutState
        {
            internal bool ScrollWrapped;
            internal RectTransform? ScrollRoot;
            internal RectTransform? ContentHost;
            internal float OriginalAnchorMinX;
            internal float OriginalAnchorMaxX = 1f;
        }
    }
}
