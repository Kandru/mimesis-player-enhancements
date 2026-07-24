using System.Linq;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.InGameMenuPlayerList
{
    /// <summary>
    /// Right-side screen overlay for the ESC menu player list. Reparents vanilla invite-code
    /// and player rows into a top-layer panel without reshaping the rest of the menu.
    /// </summary>
    internal static class InGameMenuPlayerListOverlay
    {
        private const string Feature = "Ui";
        private const string OverlayRootName = "InGameMenuPlayerListOverlay";
        private const string LegacyScrollName = "InGameMenuPlayerListScroll";

        private const float PanelMinX = 0.62f;
        private const float PanelMaxX = 0.98f;
        private const float PanelMinY = 0.04f;
        private const float PanelMaxY = 0.96f;
        private const float PanelLeftExpandPx = 24f;
        private const float PanelBottomInsetPx = 30f;
        private const float InviteScrollGapPx = 8f;

        private static readonly Dictionary<int, OverlayState> States = [];

        internal static bool IsEnabled() =>
            ModConfig.EnableExtendedInGameMenuPlayerList.Value;

        internal static void TryApply(UIPrefab_InGameMenu? menu)
        {
            if (menu == null || !IsEnabled())
            {
                return;
            }

            Show(menu);
        }

        internal static void Show(UIPrefab_InGameMenu menu)
        {
            if (!IsEnabled()
                || !CanUseMenu(menu)
                || menu.playerUIElements == null
                || menu.playerUIElements.Count == 0
                || menu.playerUIElements[0].container == null)
            {
                return;
            }

            ShowInternal(menu);
        }

        internal static bool ShowForDebug(UIPrefab_InGameMenu menu)
        {
            if (!CanUseMenu(menu)
                || menu.playerUIElements == null
                || menu.playerUIElements.Count == 0
                || menu.playerUIElements[0].container == null)
            {
                return false;
            }

            ShowInternal(menu);
            return true;
        }

        private static void ShowInternal(UIPrefab_InGameMenu menu)
        {
            try
            {
                CleanupLegacyLayout(menu);
                OverlayState state = GetOrCreateState(menu);
                EnsureOverlayCreated(state);
                if (state.OverlayRoot == null || state.ScrollList == null || state.InviteBand == null)
                {
                    return;
                }

                MountVanillaWidgets(menu, state);
                state.OverlayRoot.SetActive(true);
            }
            catch (System.Exception ex)
            {
                ModLog.Warn(Feature, $"In-game menu player list overlay failed — {ex.Message}");
            }
        }

        internal static void Hide(UIPrefab_InGameMenu menu)
        {
            if (!States.TryGetValue(menu.GetInstanceID(), out OverlayState? state))
            {
                return;
            }

            try
            {
                RestoreVanillaWidgets(state);
                if (state.OverlayRoot != null)
                {
                    state.OverlayRoot.SetActive(false);
                }
            }
            catch (System.Exception ex)
            {
                ModLog.Warn(Feature, $"In-game menu player list overlay hide failed — {ex.Message}");
            }
        }

        internal static void RefreshFromConfig()
        {
            bool enabled = IsEnabled();

            foreach (OverlayState state in States.Values.ToList())
            {
                if (state.Menu == null)
                {
                    continue;
                }

                if (enabled)
                {
                    Show(state.Menu);
                }
                else
                {
                    RevertToVanilla(state.Menu);
                }
            }
        }

        internal static void OnSessionEnded()
        {
            foreach (OverlayState state in States.Values.ToList())
            {
                if (state.Menu != null)
                {
                    RevertToVanilla(state.Menu);
                }
            }

            States.Clear();
        }

        private static void RevertToVanilla(UIPrefab_InGameMenu menu)
        {
            Hide(menu);

            if (States.TryGetValue(menu.GetInstanceID(), out OverlayState? state))
            {
                if (state.OverlayRoot != null)
                {
                    UnityEngine.Object.Destroy(state.OverlayRoot);
                }

                States.Remove(menu.GetInstanceID());
            }
        }

        private static bool CanUseMenu(UIPrefab_InGameMenu menu) =>
            menu.isActiveAndEnabled && menu.gameObject.activeInHierarchy;

        private static OverlayState GetOrCreateState(UIPrefab_InGameMenu menu)
        {
            int menuId = menu.GetInstanceID();
            if (!States.TryGetValue(menuId, out OverlayState? state))
            {
                state = new OverlayState { Menu = menu };
                States[menuId] = state;
            }
            else
            {
                state.Menu = menu;
            }

            return state;
        }

        private static void EnsureOverlayCreated(OverlayState state)
        {
            if (state.OverlayRoot != null && state.ScrollList != null)
            {
                return;
            }

            Transform? top = ModUiRoot.GetTop();
            if (top == null)
            {
                throw new System.InvalidOperationException("UI top layer unavailable.");
            }

            GameObject overlayRoot = ModUiRoot.CreateUiRoot(top, OverlayRootName);

            RectTransform panel = ModUiLayout.CreateBand(
                overlayRoot.transform,
                "RightPanel",
                PanelMinX,
                PanelMinY,
                PanelMaxX,
                PanelMaxY);
            panel.offsetMin = new Vector2(-PanelLeftExpandPx, PanelBottomInsetPx);

            RectTransform inviteBand = ModUiLayout.CreateBand(
                panel,
                "InviteBand",
                0f,
                1f,
                1f,
                1f);

            RectTransform scrollBand = ModUiLayout.CreateBand(
                panel,
                "PlayerScrollBand",
                0f,
                0f,
                1f,
                1f);

            ModScrollList scrollList = ModScrollList.Create(scrollBand);

            state.OverlayRoot = overlayRoot;
            state.Panel = panel;
            state.InviteBand = inviteBand;
            state.ScrollBand = scrollBand;
            state.ScrollList = scrollList;
            overlayRoot.SetActive(false);
        }

        private static void MountVanillaWidgets(UIPrefab_InGameMenu menu, OverlayState state)
        {
            if (state.ScrollList == null || state.InviteBand == null)
            {
                return;
            }

            MountInviteCode(menu, state);
            LayoutBandsForInvite(state);
            MountPlayerRows(menu, state);
            state.ScrollList.ScrollToTop();
        }

        private static void MountInviteCode(UIPrefab_InGameMenu menu, OverlayState state)
        {
            if (menu.UE_InviteRinkCopy == null || state.InviteBand == null)
            {
                return;
            }

            RectTransform invite = menu.UE_InviteRinkCopy.GetComponent<RectTransform>();
            if (!state.InviteMounted)
            {
                state.InviteOriginalParent = invite.parent;
                state.InviteOriginalSiblingIndex = invite.GetSiblingIndex();
                state.InviteOriginalAnchorMin = invite.anchorMin;
                state.InviteOriginalAnchorMax = invite.anchorMax;
                state.InviteOriginalPivot = invite.pivot;
                state.InviteOriginalAnchoredPosition = invite.anchoredPosition;
                state.InviteOriginalSizeDelta = invite.sizeDelta;
                state.InviteMounted = true;
            }

            invite.SetParent(state.InviteBand, false);
            ApplyInviteOverlayLayout(invite, state);
        }

        private static void ApplyInviteOverlayLayout(RectTransform invite, OverlayState state)
        {
            float inviteHeight = MeasureInviteHeight(invite, state);

            invite.anchorMin = new Vector2(0f, 1f);
            invite.anchorMax = new Vector2(1f, 1f);
            invite.pivot = new Vector2(0.5f, 1f);
            invite.anchoredPosition = Vector2.zero;
            invite.sizeDelta = new Vector2(0f, inviteHeight);
            LayoutRebuilder.ForceRebuildLayoutImmediate(invite);
        }

        private static float MeasureInviteHeight(RectTransform invite, OverlayState state)
        {
            if (state.InviteOriginalSizeDelta.y >= 1f)
            {
                return state.InviteOriginalSizeDelta.y;
            }

            LayoutElement? layoutElement = invite.GetComponent<LayoutElement>();
            if (layoutElement != null && layoutElement.preferredHeight >= 1f)
            {
                return layoutElement.preferredHeight;
            }

            if (invite.rect.height >= 1f)
            {
                return invite.rect.height;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(invite);
            if (invite.rect.height >= 1f)
            {
                return invite.rect.height;
            }

            return 56f;
        }

        private static void LayoutBandsForInvite(OverlayState state)
        {
            if (state.Panel == null
                || state.InviteBand == null
                || state.ScrollBand == null
                || state.Menu == null
                || state.Menu.UE_InviteRinkCopy == null)
            {
                return;
            }

            RectTransform invite = state.Menu.UE_InviteRinkCopy.GetComponent<RectTransform>();
            ApplyInviteOverlayLayout(invite, state);

            float inviteHeight = Mathf.Max(invite.rect.height, MeasureInviteHeight(invite, state));
            float topReserved = inviteHeight + InviteScrollGapPx;

            state.InviteBand.anchorMin = new Vector2(0f, 1f);
            state.InviteBand.anchorMax = new Vector2(1f, 1f);
            state.InviteBand.pivot = new Vector2(0.5f, 1f);
            state.InviteBand.anchoredPosition = Vector2.zero;
            state.InviteBand.sizeDelta = new Vector2(0f, inviteHeight);
            state.InviteBand.SetAsLastSibling();

            invite.anchorMin = Vector2.zero;
            invite.anchorMax = Vector2.one;
            invite.pivot = new Vector2(0.5f, 0.5f);
            invite.offsetMin = Vector2.zero;
            invite.offsetMax = Vector2.zero;
            LayoutRebuilder.ForceRebuildLayoutImmediate(invite);

            state.ScrollBand.anchorMin = Vector2.zero;
            state.ScrollBand.anchorMax = Vector2.one;
            state.ScrollBand.offsetMin = Vector2.zero;
            state.ScrollBand.offsetMax = new Vector2(0f, -topReserved);
        }

        private static void MountPlayerRows(UIPrefab_InGameMenu menu, OverlayState state)
        {
            if (state.ScrollList == null)
            {
                return;
            }

            float rowHeight = InGameMenuPlayerListRows.MeasureRowHeight(menu);
            InGameMenuPlayerListRows.ApplyRowLayoutElements(menu, rowHeight);

            RectTransform content = state.ScrollList.Content;
            float contentHeight = rowHeight * menu.playerUIElements.Count;
            content.sizeDelta = new Vector2(0f, contentHeight);

            for (int i = 0; i < menu.playerUIElements.Count; i++)
            {
                UIPrefab_InGameMenu.PlayerUIElement element = menu.playerUIElements[i];
                if (element.container?.GetComponent<RectTransform>() is not RectTransform rowRect)
                {
                    continue;
                }

                if (!state.RowSnapshots.TryGetValue(rowRect, out RowSnapshot snapshot))
                {
                    snapshot = new RowSnapshot
                    {
                        OriginalParent = rowRect.parent,
                        OriginalSiblingIndex = rowRect.GetSiblingIndex(),
                        AnchorMin = rowRect.anchorMin,
                        AnchorMax = rowRect.anchorMax,
                        Pivot = rowRect.pivot,
                        AnchoredPosition = rowRect.anchoredPosition,
                        SizeDelta = rowRect.sizeDelta,
                    };
                    state.RowSnapshots[rowRect] = snapshot;
                }

                rowRect.SetParent(content, false);
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.anchoredPosition = new Vector2(0f, -rowHeight * i);
                rowRect.sizeDelta = new Vector2(0f, rowHeight);
            }
        }

        private static void RestoreVanillaWidgets(OverlayState state)
        {
            if (state.Menu == null)
            {
                return;
            }

            if (state.InviteMounted)
            {
                RectTransform invite = state.Menu.UE_InviteRinkCopy.GetComponent<RectTransform>();
                invite.SetParent(state.InviteOriginalParent, false);
                invite.SetSiblingIndex(state.InviteOriginalSiblingIndex);
                invite.anchorMin = state.InviteOriginalAnchorMin;
                invite.anchorMax = state.InviteOriginalAnchorMax;
                invite.pivot = state.InviteOriginalPivot;
                invite.anchoredPosition = state.InviteOriginalAnchoredPosition;
                invite.sizeDelta = state.InviteOriginalSizeDelta;
                state.InviteMounted = false;
            }

            foreach ((RectTransform row, RowSnapshot snapshot) in state.RowSnapshots.ToList())
            {
                if (row == null)
                {
                    continue;
                }

                row.SetParent(snapshot.OriginalParent, false);
                row.SetSiblingIndex(snapshot.OriginalSiblingIndex);
                row.anchorMin = snapshot.AnchorMin;
                row.anchorMax = snapshot.AnchorMax;
                row.pivot = snapshot.Pivot;
                row.anchoredPosition = snapshot.AnchoredPosition;
                row.sizeDelta = snapshot.SizeDelta;
            }

            state.RowSnapshots.Clear();
        }

        private static void CleanupLegacyLayout(UIPrefab_InGameMenu menu)
        {
            foreach (Transform child in menu.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != LegacyScrollName)
                {
                    continue;
                }

                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private sealed class OverlayState
        {
            internal UIPrefab_InGameMenu? Menu;
            internal GameObject? OverlayRoot;
            internal RectTransform? Panel;
            internal RectTransform? InviteBand;
            internal RectTransform? ScrollBand;
            internal ModScrollList? ScrollList;
            internal bool InviteMounted;
            internal Transform? InviteOriginalParent;
            internal int InviteOriginalSiblingIndex;
            internal Vector2 InviteOriginalAnchorMin;
            internal Vector2 InviteOriginalAnchorMax;
            internal Vector2 InviteOriginalPivot;
            internal Vector2 InviteOriginalAnchoredPosition;
            internal Vector2 InviteOriginalSizeDelta;
            internal Dictionary<RectTransform, RowSnapshot> RowSnapshots = [];
        }

        private sealed class RowSnapshot
        {
            internal Transform? OriginalParent;
            internal int OriginalSiblingIndex;
            internal Vector2 AnchorMin;
            internal Vector2 AnchorMax;
            internal Vector2 Pivot;
            internal Vector2 AnchoredPosition;
            internal Vector2 SizeDelta;
        }
    }
}
