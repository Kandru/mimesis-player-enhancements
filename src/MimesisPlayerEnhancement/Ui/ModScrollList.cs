using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Ui
{
    /// <summary>
    /// Vertical scroll view with clamped movement, custom scrollbar and a
    /// vertically-growing content column. Rows are added by the caller into
    /// <see cref="Content"/> (typically via a row factory).
    /// </summary>
    internal sealed class ModScrollList
    {
        internal const float DefaultScrollbarWidth = 14f;
        internal const float DefaultScrollbarSpacing = 6f;

        internal RectTransform Content { get; }
        internal ScrollRect ScrollRect { get; }

        private ModScrollList(RectTransform content, ScrollRect scrollRect)
        {
            Content = content;
            ScrollRect = scrollRect;
        }

        internal static ModScrollList Create(RectTransform band)
        {
            GameObject scrollGo = ModUiLayout.CreateChild("ScrollView", band);
            ModUiLayout.Stretch(scrollGo.GetComponent<RectTransform>());

            Image scrollHitTarget = scrollGo.AddComponent<Image>();
            scrollHitTarget.color = Color.clear;
            scrollHitTarget.raycastTarget = true;

            ScrollRect scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 35f;
            scrollRect.inertia = true;

            Scrollbar verticalScrollbar = CreateVerticalScrollbar(scrollGo.transform, DefaultScrollbarWidth);
            scrollRect.verticalScrollbar = verticalScrollbar;
            scrollRect.verticalScrollbarSpacing = DefaultScrollbarSpacing;

            GameObject viewportGo = ModUiLayout.CreateChild("Viewport", scrollGo.transform);
            RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
            ModUiLayout.Stretch(viewportRect);
            viewportRect.offsetMax = new Vector2(-(DefaultScrollbarWidth + DefaultScrollbarSpacing), 0f);
            viewportGo.AddComponent<RectMask2D>();

            GameObject contentGo = ModUiLayout.CreateChild("Content", viewportGo.transform);
            RectTransform content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = contentGo.AddComponent<VerticalLayoutGroup>();
            ModUiLayout.SetEnumProperty(layout, "childAlignment", 1);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 10f;
            layout.padding = new RectOffset(0, 0, 0, 0);

            ContentSizeFitter fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = content;
            return new ModScrollList(content, scrollRect);
        }

        internal void ScrollToTop()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
            ScrollRect.verticalNormalizedPosition = 1f;
        }

        internal Component CreatePlaceholderLabel(ModUiAssets assets, string text)
        {
            GameObject go = ModUiLayout.CreateChild("EmptyLabel", Content);
            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 80f;
            Component label = ModUiFactory.AddText(go, assets, text, 22f, ModUiFontStyle.Normal);
            ModUiText.SetColor(label, assets.TextColor);
            ModUiText.SetMiddleCenterAlignment(label);
            return label;
        }

        internal static Scrollbar CreateVerticalScrollbar(Transform parent, float width)
        {
            GameObject scrollbarGo = ModUiLayout.CreateChild("Scrollbar Vertical", parent);
            RectTransform scrollbarRect = scrollbarGo.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 1f);
            scrollbarRect.sizeDelta = new Vector2(width, 0f);
            scrollbarRect.anchoredPosition = Vector2.zero;

            Image track = scrollbarGo.AddComponent<Image>();
            track.color = new Color(0.08f, 0.07f, 0.06f, 0.95f);
            track.raycastTarget = true;

            Scrollbar scrollbar = scrollbarGo.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            GameObject slidingAreaGo = ModUiLayout.CreateChild("Sliding Area", scrollbarGo.transform);
            RectTransform slidingAreaRect = slidingAreaGo.GetComponent<RectTransform>();
            ModUiLayout.Stretch(slidingAreaRect);
            slidingAreaRect.offsetMin = new Vector2(2f, 4f);
            slidingAreaRect.offsetMax = new Vector2(-2f, -4f);

            GameObject handleGo = ModUiLayout.CreateChild("Handle", slidingAreaGo.transform);
            RectTransform handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(1f, 0f);
            handleRect.pivot = new Vector2(0.5f, 0f);
            handleRect.sizeDelta = new Vector2(0f, 24f);

            Image handleImage = handleGo.AddComponent<Image>();
            handleImage.color = new Color(0.48f, 0.40f, 0.24f, 0.98f);
            handleImage.raycastTarget = true;

            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;

            return scrollbar;
        }
    }
}
