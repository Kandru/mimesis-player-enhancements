using MimesisPlayerEnhancement.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.Replays
{
    internal static class ReplayPickerRowFactory
    {
        private const float VerticalPadding = 10f;
        private const float Line1Height = 24f;
        private const float Line2Height = 22f;
        private const float Line3Height = 18f;

        internal static float ComputeRowHeight() =>
            VerticalPadding + Line1Height + Line2Height + Line3Height + VerticalPadding;

        internal static ReplayPickerRow CreateRow(
            Transform parent,
            ModUiAssets assets,
            ReplayLibraryEntry entry,
            Action<ReplayPickerRow> onSelected,
            Action<ReplayPickerRow> onDoubleClicked)
        {
            float rowHeight = ComputeRowHeight();

            GameObject rowGo = ModUiLayout.CreateChild("ReplayPickerRow", parent);
            RectTransform rowRect = rowGo.GetComponent<RectTransform>();
            ModUiLayout.PrepareListRowLayout(rowRect, rowHeight);

            LayoutElement layout = rowGo.AddComponent<LayoutElement>();
            layout.preferredHeight = rowHeight;
            layout.minHeight = rowHeight;
            layout.flexibleWidth = 1f;

            Image background = rowGo.AddComponent<Image>();
            background.sprite = null;
            background.type = Image.Type.Simple;
            background.color = new Color(0.10f, 0.09f, 0.08f, 0.92f);
            background.raycastTarget = true;

            Outline outline = rowGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.55f, 0.45f, 0.22f, 0.55f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.enabled = false;

            Button button = rowGo.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;

            GameObject textRoot = ModUiLayout.CreateChild("TextRoot", rowGo.transform);
            RectTransform textRect = textRoot.GetComponent<RectTransform>();
            ModUiLayout.Stretch(textRect);
            textRect.offsetMin = new Vector2(16f, 10f);
            textRect.offsetMax = new Vector2(-16f, -10f);

            Component text = ModUiFactory.AddText(
                textRoot,
                assets,
                ReplayPickerRowText.Compose(entry),
                17f,
                ModUiFontStyle.Normal);
            ModUiText.SetColor(text, assets.TextColor);
            ModUiText.SetAlignment(text, upperLeft: true);
            ModUiText.ConfigureTextLayout(text, wordWrap: true, ModUiText.OverflowOverflow);
            ModUiText.EnableRichText(text);

            ReplayPickerRow row = rowGo.AddComponent<ReplayPickerRow>();
            row.Initialize(entry, background, outline, onSelected, onDoubleClicked);
            rowGo.AddComponent<ModUiScrollForwarder>();
            WireRowFeedback(button, row, assets);
            return row;
        }

        private static void WireRowFeedback(Button button, ReplayPickerRow row, ModUiAssets assets)
        {
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>()
                ?? button.gameObject.AddComponent<EventTrigger>();

            ModUiFactory.AddTrigger(trigger, EventTriggerType.PointerEnter, () =>
            {
                ModUiFactory.PlaySfx(assets.ButtonHoverSfxId);
                row.SetHovered(true);
            });

            ModUiFactory.AddTrigger(trigger, EventTriggerType.PointerExit, () =>
            {
                row.SetHovered(false);
            });
        }
    }
}
