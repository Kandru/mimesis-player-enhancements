using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList
{
    internal sealed class LoadingWaitPlayerListOverlay
    {
        internal const string RootObjectName = "MPE_LoadingWaitPlayerList";
        internal const string BoundsObjectName = "MPE_LoadingWaitPlayerListBounds";
        internal const string FlowObjectName = "MPE_LoadingWaitPlayerListFlow";

        private const int CanvasSortOrder = CustomLoadingScreenConstants.OverlayCanvasSortOrder + 1;

        internal GameObject? Root { get; private set; }
        internal CanvasGroup? CanvasGroup { get; private set; }
        internal LoadingWaitPlayerListGrid.GridState? GridState { get; private set; }
        internal RectTransform? BoundsRect { get; private set; }

        internal bool TryEnsure(Transform parent)
        {
            if (parent == null)
            {
                return false;
            }

            if (Root != null && GridState != null)
            {
                Root.transform.SetParent(parent, worldPositionStays: false);
                Root.SetActive(true);
                ApplyAlpha(1f);
                return true;
            }

            UIPrefab_Spectator_PlayerListView? listView = FindSpectatorListTemplate();
            if (listView == null)
            {
                return false;
            }

            Root = new GameObject(RootObjectName);
            Root.transform.SetParent(parent, worldPositionStays: false);
            RectTransform rootRect = Root.AddComponent<RectTransform>();
            StretchRect(rootRect);

            Canvas canvas = Root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = CanvasSortOrder;

            Root.AddComponent<GraphicRaycaster>();

            CanvasGroup = Root.AddComponent<CanvasGroup>();
            CanvasGroup.alpha = 1f;
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;

            GameObject boundsObject = new(BoundsObjectName);
            boundsObject.transform.SetParent(Root.transform, worldPositionStays: false);
            BoundsRect = boundsObject.AddComponent<RectTransform>();
            StretchRect(BoundsRect);

            GameObject flowObject = new(FlowObjectName);
            flowObject.transform.SetParent(BoundsRect, worldPositionStays: false);
            RectTransform flowRect = flowObject.AddComponent<RectTransform>();
            AnchorBottomStretch(flowRect);

            if (!LoadingWaitPlayerListGrid.TryInitialize(
                    listView,
                    BoundsRect,
                    flowRect,
                    out LoadingWaitPlayerListGrid.GridState? gridState))
            {
                UnityEngine.Object.Destroy(Root);
                Root = null;
                BoundsRect = null;
                CanvasGroup = null;
                return false;
            }

            GridState = gridState;
            return true;
        }

        internal void ApplyAlpha(float alpha)
        {
            if (CanvasGroup != null)
            {
                CanvasGroup.alpha = Mathf.Clamp01(alpha);
            }
        }

        internal void Destroy()
        {
            if (GridState != null)
            {
                LoadingWaitPlayerListGrid.Destroy(GridState);
                GridState = null;
            }

            if (Root != null)
            {
                UnityEngine.Object.Destroy(Root);
                Root = null;
            }

            BoundsRect = null;
            CanvasGroup = null;
        }

        private static UIPrefab_Spectator_PlayerListView? FindSpectatorListTemplate()
        {
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
                if (rows is { Length: > 0 })
                {
                    return view;
                }
            }

            return null;
        }

        private static void StretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void AnchorBottomStretch(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }
    }
}
