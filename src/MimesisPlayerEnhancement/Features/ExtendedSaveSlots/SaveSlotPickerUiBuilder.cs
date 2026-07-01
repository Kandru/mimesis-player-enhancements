using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal sealed class SaveSlotPickerLayoutBands
    {
        internal RectTransform TitleBand { get; set; } = null!;
        internal RectTransform ScrollBand { get; set; } = null!;
        internal RectTransform ActionBand { get; set; } = null!;
        internal RectTransform BackBand { get; set; } = null!;
    }

    internal static class SaveSlotPickerUiBuilder
    {
        private static readonly Type? TmpTextType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");

        private const float HorizontalInset = 0.04f;
        private const float TitleTop = 0.98f;
        private const float TitleBottom = 0.90f;
        private const float ScrollTop = 0.86f;
        private const float ScrollBottom = 0.24f;
        private const float ActionTop = 0.22f;
        private const float ActionBottom = 0.14f;
        private const float BackTop = 0.12f;
        private const float BackBottom = 0.05f;

        private static readonly Color SlotFallbackColor = new(0.18f, 0.15f, 0.08f, 0.92f);
        private static readonly Color ButtonFallbackColor = new(0.22f, 0.18f, 0.10f, 1f);

        private const int TmpOverflowOverflow = 0;
        private const int TmpOverflowEllipsis = 1;

        internal static Transform? GetUiTopParent()
        {
            UIManager? uiManager = SaveSlotGameAccess.TryGetUiManager();
            if (uiManager == null)
            {
                return null;
            }

            FieldInfo? nodesField = typeof(UIManager).GetField(
                "nodes",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (nodesField?.GetValue(uiManager) is not Transform[] nodes || nodes.Length == 0)
            {
                return null;
            }

            int topIndex = (int)Enum.Parse(typeof(eUIHeight), "Top");
            if (topIndex < 0 || topIndex >= nodes.Length)
            {
                topIndex = Math.Min(1, nodes.Length - 1);
            }

            return nodes[topIndex];
        }

        internal static GameObject CreateUiRoot(Transform parent, string name)
        {
            GameObject root = new(name);
            RectTransform rect = root.AddComponent<RectTransform>();
            rect.SetParent(parent, worldPositionStays: false);
            Stretch(rect);
            return root;
        }

        internal static GameObject CreateFullScreenPanel(Transform parent, SaveSlotPickerUiAssets assets)
        {
            GameObject go = CreateChild("Panel", parent);
            Stretch(go.GetComponent<RectTransform>());

            Image bg = go.AddComponent<Image>();
            bg.color = assets.PanelBackdropColor;
            bg.raycastTarget = true;
            return go;
        }

        internal static SaveSlotPickerLayoutBands CreateLayoutBands(Transform panel)
        {
            return new SaveSlotPickerLayoutBands
            {
                TitleBand = CreateBand(panel, "TitleBand", HorizontalInset, TitleBottom, 1f - HorizontalInset, TitleTop),
                ScrollBand = CreateBand(panel, "ScrollBand", HorizontalInset, ScrollBottom, 1f - HorizontalInset, ScrollTop),
                ActionBand = CreateBand(panel, "ActionBand", HorizontalInset, ActionBottom, 1f - HorizontalInset, ActionTop),
                BackBand = CreateBand(panel, "BackBand", HorizontalInset, BackBottom, 1f - HorizontalInset, BackTop),
            };
        }

        internal static Component CreateTitle(RectTransform band, SaveSlotPickerUiAssets assets, string text)
        {
            GameObject go = CreateChild("Title", band);
            Stretch(go.GetComponent<RectTransform>());

            Component label = AddText(go, assets, text, 34f, FontStyles.Bold);
            SaveSlotTextHelper.SetColor(label, assets.TitleTextColor);
            SaveSlotTextHelper.SetMiddleCenterAlignment(label);
            SaveSlotTextHelper.ConfigureTextLayout(label, wordWrap: true, TmpOverflowEllipsis);
            return label;
        }

        internal static RectTransform CreateScrollView(RectTransform band, out ScrollRect scrollRect)
        {
            const float scrollbarWidth = 14f;

            GameObject scrollGo = CreateChild("ScrollView", band);
            Stretch(scrollGo.GetComponent<RectTransform>());

            Image scrollHitTarget = scrollGo.AddComponent<Image>();
            scrollHitTarget.color = Color.clear;
            scrollHitTarget.raycastTarget = true;

            scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 35f;
            scrollRect.inertia = true;

            Scrollbar verticalScrollbar = CreateVerticalScrollbar(scrollGo.transform, scrollbarWidth);
            scrollRect.verticalScrollbar = verticalScrollbar;
            scrollRect.verticalScrollbarSpacing = 6f;

            GameObject viewportGo = CreateChild("Viewport", scrollGo.transform);
            RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
            Stretch(viewportRect);
            viewportRect.offsetMax = new Vector2(-(scrollbarWidth + 6f), 0f);
            viewportGo.AddComponent<RectMask2D>();

            GameObject contentGo = CreateChild("Content", viewportGo.transform);
            RectTransform content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = contentGo.AddComponent<VerticalLayoutGroup>();
            SetEnumProperty(layout, "childAlignment", 1);
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
            return content;
        }

        private static Scrollbar CreateVerticalScrollbar(Transform parent, float width)
        {
            GameObject scrollbarGo = CreateChild("Scrollbar Vertical", parent);
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

            GameObject slidingAreaGo = CreateChild("Sliding Area", scrollbarGo.transform);
            RectTransform slidingAreaRect = slidingAreaGo.GetComponent<RectTransform>();
            Stretch(slidingAreaRect);
            slidingAreaRect.offsetMin = new Vector2(2f, 4f);
            slidingAreaRect.offsetMax = new Vector2(-2f, -4f);

            GameObject handleGo = CreateChild("Handle", slidingAreaGo.transform);
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

        internal static RectTransform CreateActionButtonRow(RectTransform band)
        {
            GameObject rowGo = CreateChild("ActionButtonRow", band);
            Stretch(rowGo.GetComponent<RectTransform>());

            HorizontalLayoutGroup layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            SetEnumProperty(layout, "childAlignment", 4);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(0, 0, 4, 4);
            return rowGo.GetComponent<RectTransform>();
        }

        internal static RectTransform CreateBackButtonRow(RectTransform band)
        {
            GameObject rowGo = CreateChild("BackButtonRow", band);
            Stretch(rowGo.GetComponent<RectTransform>());

            HorizontalLayoutGroup layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            SetEnumProperty(layout, "childAlignment", 4);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(0, 0, 4, 4);
            return rowGo.GetComponent<RectTransform>();
        }

        internal static Button CreateFooterButton(
            Transform parent,
            SaveSlotPickerUiAssets assets,
            string label,
            bool expandWidth,
            UnityEngine.Events.UnityAction? onClick)
        {
            GameObject go = CreateChild("FooterButton", parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            PrepareLayoutGroupChild(rect);

            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.minHeight = assets.FooterButtonHeight;
            layout.preferredHeight = assets.FooterButtonHeight;
            layout.flexibleWidth = expandWidth ? 1f : 0f;
            if (!expandWidth)
            {
                layout.preferredWidth = Mathf.Min(assets.FooterButtonWidth * 1.4f, 320f);
            }

            Image image = go.AddComponent<Image>();
            ApplySprite(image, assets.FooterButtonSprite, assets.FooterButtonImageType, ButtonFallbackColor);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            WireButtonFeedback(button, assets);

            GameObject labelGo = CreateChild("Label", go.transform);
            Stretch(labelGo.GetComponent<RectTransform>());
            Component text = AddText(labelGo, assets, label, 22f, FontStyles.Normal);
            SaveSlotTextHelper.SetColor(text, assets.SlotTextColor);
            SaveSlotTextHelper.SetMiddleCenterAlignment(text);
            SaveSlotTextHelper.ConfigureTextLayout(text, wordWrap: false, TmpOverflowEllipsis);

            return button;
        }

        internal static SaveSlotPickerRow CreateSlotRow(
            Transform parent,
            SaveSlotPickerUiAssets assets,
            SaveSlotEntry entry,
            Action<SaveSlotPickerRow> onSelected,
            Action<SaveSlotPickerRow> onDoubleClicked)
        {
            float rowHeight = SaveSlotPickerExtraStats.ComputeRowHeight();

            GameObject rowGo = CreateChild("SaveSlotRow", parent);
            RectTransform rowRect = rowGo.GetComponent<RectTransform>();
            PrepareListRowLayout(rowRect, rowHeight);

            LayoutElement layout = rowGo.AddComponent<LayoutElement>();
            layout.preferredHeight = rowHeight;
            layout.minHeight = rowHeight;
            layout.flexibleWidth = 1f;

            Image bg = rowGo.AddComponent<Image>();
            bg.sprite = null;
            bg.type = Image.Type.Simple;
            bg.color = new Color(0.10f, 0.09f, 0.08f, 0.92f);
            bg.raycastTarget = true;

            Outline border = rowGo.AddComponent<Outline>();
            border.effectColor = new Color(0.55f, 0.45f, 0.22f, 0.55f);
            border.effectDistance = new Vector2(1f, -1f);

            Button button = rowGo.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;

            GameObject textRoot = CreateChild("TextRoot", rowGo.transform);
            RectTransform textRect = textRoot.GetComponent<RectTransform>();
            Stretch(textRect);
            textRect.offsetMin = new Vector2(16f, 10f);
            textRect.offsetMax = new Vector2(-16f, -10f);

            Component label = AddText(textRoot, assets, SaveSlotPickerRowText.Compose(entry), 17f, FontStyles.Normal);
            SaveSlotTextHelper.SetColor(label, assets.SlotTextColor);
            SaveSlotTextHelper.SetAlignment(label, upperLeft: true);
            SaveSlotTextHelper.ConfigureTextLayout(label, wordWrap: true, TmpOverflowOverflow);
            SaveSlotTextHelper.EnableRichText(label);

            SaveSlotPickerRow row = rowGo.AddComponent<SaveSlotPickerRow>();
            row.Initialize(entry, bg, label, assets, onSelected, onDoubleClicked);
            rowGo.AddComponent<SaveSlotPickerScrollForwarder>();
            WireRowFeedback(button, row, assets);
            return row;
        }

        internal static Component CreateEmptyLabel(Transform parent, SaveSlotPickerUiAssets assets, string text)
        {
            GameObject go = CreateChild("EmptyLabel", parent);
            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.preferredHeight = 80f;
            Component label = AddText(go, assets, text, 22f, FontStyles.Normal);
            SaveSlotTextHelper.SetColor(label, assets.SlotTextColor);
            SaveSlotTextHelper.SetMiddleCenterAlignment(label);
            return label;
        }

        private static void PrepareListRowLayout(RectTransform rect, float height)
        {
            rect.localScale = Vector3.one;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, height);
        }

        private static void PrepareLayoutGroupChild(RectTransform rect)
        {
            rect.localScale = Vector3.one;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static RectTransform CreateBand(
            Transform parent,
            string name,
            float minX,
            float minY,
            float maxX,
            float maxY)
        {
            GameObject go = CreateChild(name, parent);
            SetAnchors(go.GetComponent<RectTransform>(), minX, minY, maxX, maxY);
            return go.GetComponent<RectTransform>();
        }

        private static void WireButtonFeedback(Button button, SaveSlotPickerUiAssets assets)
        {
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>()
                ?? button.gameObject.AddComponent<EventTrigger>();

            AddTrigger(trigger, EventTriggerType.PointerEnter, () =>
            {
                PlaySfx(assets.ButtonHoverSfxId);
                Component? text = SaveSlotTextHelper.FindTextComponent(button.gameObject);
                if (button.interactable)
                {
                    SaveSlotTextHelper.SetColor(text, assets.HoverTextColor);
                }
            });

            AddTrigger(trigger, EventTriggerType.PointerExit, () =>
            {
                Component? text = SaveSlotTextHelper.FindTextComponent(button.gameObject);
                SaveSlotTextHelper.SetColor(
                    text,
                    button.interactable ? assets.SlotTextColor : assets.DisabledTextColor);
            });

            button.onClick.AddListener(() => PlaySfx(assets.ButtonClickSfxId));
        }

        private static void WireRowFeedback(Button button, SaveSlotPickerRow row, SaveSlotPickerUiAssets assets)
        {
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>()
                ?? button.gameObject.AddComponent<EventTrigger>();

            AddTrigger(trigger, EventTriggerType.PointerEnter, () =>
            {
                PlaySfx(assets.ButtonHoverSfxId);
                row.SetHovered(true);
            });

            AddTrigger(trigger, EventTriggerType.PointerExit, () =>
            {
                row.SetHovered(false);
            });
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action callback)
        {
            EventTrigger.Entry entry = new() { eventID = type };
            entry.callback.AddListener(_ => callback());
            trigger.triggers.Add(entry);
        }

        private static void PlaySfx(string sfxId) => SaveSlotGameAccess.TryPlaySfx(sfxId);

        private static Component AddText(
            GameObject go,
            SaveSlotPickerUiAssets assets,
            string text,
            float fontSize,
            FontStyles fontStyle)
        {
            Component? label = TmpTextType != null ? go.AddComponent(TmpTextType) as Component : null;
            if (label == null)
            {
                Text fallback = go.AddComponent<Text>();
                fallback.text = text;
                fallback.fontSize = (int)fontSize;
                fallback.color = assets.SlotTextColor;
                return fallback;
            }

            assets.ApplyFont(label);
            SaveSlotTextHelper.SetText(label, text);
            PropertyInfo? sizeProp = label.GetType().GetProperty("fontSize", BindingFlags.Instance | BindingFlags.Public);
            sizeProp?.SetValue(label, fontSize);
            PropertyInfo? styleProp = label.GetType().GetProperty("fontStyle", BindingFlags.Instance | BindingFlags.Public);
            if (styleProp != null && styleProp.PropertyType.IsEnum)
            {
                styleProp.SetValue(label, Enum.ToObject(styleProp.PropertyType, (int)fontStyle));
            }

            return label;
        }

        private static void ApplySprite(Image image, Sprite? sprite, Image.Type imageType, Color fallbackColor)
        {
            image.preserveAspect = false;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = imageType;
                image.color = Color.white;
                return;
            }

            image.sprite = null;
            image.type = Image.Type.Sliced;
            image.color = fallbackColor;
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            GameObject go = new(name);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.SetParent(parent, worldPositionStays: false);
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetEnumProperty(object target, string propertyName, int enumValue)
        {
            PropertyInfo? property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            if (property == null || !property.PropertyType.IsEnum)
            {
                return;
            }

            property.SetValue(target, Enum.ToObject(property.PropertyType, enumValue));
        }

        private enum FontStyles
        {
            Normal = 0,
            Bold = 1,
        }
    }
}
