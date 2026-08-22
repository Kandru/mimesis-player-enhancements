using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList
{
    internal sealed class LoadingWaitPlayerListOverlay
    {
        private const string RootObjectName = "MPE_LoadingWaitPlayerList";
        private const int CanvasSortOrder = 32001;

        internal GameObject? Root { get; private set; }
        internal CanvasGroup? CanvasGroup { get; private set; }
        internal LoadingWaitPlayerListGrid.GridState? GridState { get; private set; }

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
            ModUiLayout.Stretch(Root.AddComponent<RectTransform>());

            Canvas canvas = Root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = CanvasSortOrder;

            CanvasGroup = Root.AddComponent<CanvasGroup>();
            CanvasGroup.alpha = 1f;
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;

            RectTransform boundsRect = ModUiLayout.CreateChild(
                    "MPE_LoadingWaitPlayerListBounds",
                    Root.transform)
                .GetComponent<RectTransform>();
            ModUiLayout.Stretch(boundsRect);

            RectTransform shadeRect = ModUiLayout.CreateChild(
                    "MPE_LoadingWaitPlayerListShade",
                    boundsRect)
                .GetComponent<RectTransform>();
            Image shadeImage = shadeRect.gameObject.AddComponent<Image>();
            shadeImage.color = LoadingWaitPlayerListBandLayout.ShadeColor;
            shadeImage.raycastTarget = false;

            RectTransform flowRect = ModUiLayout.CreateChild(
                    "MPE_LoadingWaitPlayerListFlow",
                    boundsRect)
                .GetComponent<RectTransform>();
            flowRect.anchorMin = Vector2.zero;
            flowRect.anchorMax = Vector2.one;
            flowRect.offsetMin = Vector2.zero;
            flowRect.offsetMax = Vector2.zero;
            flowRect.pivot = Vector2.zero;

            if (!LoadingWaitPlayerListGrid.TryInitialize(
                    listView,
                    boundsRect,
                    flowRect,
                    shadeRect,
                    out LoadingWaitPlayerListGrid.GridState? gridState))
            {
                UnityEngine.Object.Destroy(Root);
                Root = null;
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
    }
}
