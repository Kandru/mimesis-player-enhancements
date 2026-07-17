using UnityEngine;

namespace MimesisPlayerEnhancement.Ui
{
    /// <summary>
    /// Styled uGUI button with TMP label and vanilla hover/click SFX wiring.
    /// </summary>
    internal static class ModButton
    {
        private static readonly Color ButtonFallbackColor = new(0.22f, 0.18f, 0.10f, 1f);

        internal static Button Create(
            Transform parent,
            ModUiAssets assets,
            string label,
            bool expandWidth,
            UnityEngine.Events.UnityAction? onClick)
        {
            GameObject go = ModUiLayout.CreateChild("FooterButton", parent);
            RectTransform rect = go.GetComponent<RectTransform>();
            ModUiLayout.PrepareLayoutGroupChild(rect);

            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.minHeight = assets.ButtonHeight;
            layout.preferredHeight = assets.ButtonHeight;
            layout.flexibleWidth = expandWidth ? 1f : 0f;
            if (!expandWidth)
            {
                layout.preferredWidth = Mathf.Min(assets.ButtonWidth * 1.4f, 320f);
            }

            Image image = go.AddComponent<Image>();
            ModUiFactory.ApplySprite(image, assets.ButtonSprite, assets.ButtonImageType, ButtonFallbackColor);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            WireFeedback(button, assets);

            GameObject labelGo = ModUiLayout.CreateChild("Label", go.transform);
            ModUiLayout.Stretch(labelGo.GetComponent<RectTransform>());
            Component text = ModUiFactory.AddText(labelGo, assets, label, 22f, ModUiFontStyle.Normal);
            ModUiText.SetColor(text, assets.TextColor);
            ModUiText.SetMiddleCenterAlignment(text);
            ModUiText.ConfigureTextLayout(text, wordWrap: false, ModUiText.OverflowEllipsis);

            return button;
        }

        internal static void SetEnabled(Button button, bool enabled, Color enabledColor, Color disabledColor)
        {
            button.interactable = enabled;
            Component? text = ModUiText.FindTextComponent(button.gameObject);
            ModUiText.SetColor(text, enabled ? enabledColor : disabledColor);
        }

        private static void WireFeedback(Button button, ModUiAssets assets)
        {
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>()
                ?? button.gameObject.AddComponent<EventTrigger>();

            ModUiFactory.AddTrigger(trigger, EventTriggerType.PointerEnter, () =>
            {
                ModUiFactory.PlaySfx(assets.ButtonHoverSfxId);
                Component? text = ModUiText.FindTextComponent(button.gameObject);
                if (button.interactable)
                {
                    ModUiText.SetColor(text, assets.HoverTextColor);
                }
            });

            ModUiFactory.AddTrigger(trigger, EventTriggerType.PointerExit, () =>
            {
                Component? text = ModUiText.FindTextComponent(button.gameObject);
                ModUiText.SetColor(
                    text,
                    button.interactable ? assets.TextColor : assets.DisabledTextColor);
            });

            button.onClick.AddListener(() => ModUiFactory.PlaySfx(assets.ButtonClickSfxId));
        }
    }
}
