using MimesisPlayerEnhancement.Ui;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    /// <summary>
    /// Builds the save-slot list rows for the picker. Generic widgets come from
    /// <c>MimesisPlayerEnhancement.Ui</c>; only the row composition is feature-specific.
    /// </summary>
    internal static class SaveSlotRowFactory
    {
        internal static SaveSlotPickerRow CreateSlotRow(
            Transform parent,
            ModUiAssets assets,
            SaveSlotEntry entry,
            Action<SaveSlotPickerRow> onSelected,
            Action<SaveSlotPickerRow> onDoubleClicked)
        {
            float rowHeight = SaveSlotPickerExtraStats.ComputeRowHeight();

            GameObject rowGo = ModUiLayout.CreateChild("SaveSlotRow", parent);
            RectTransform rowRect = rowGo.GetComponent<RectTransform>();
            ModUiLayout.PrepareListRowLayout(rowRect, rowHeight);

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

            GameObject textRoot = ModUiLayout.CreateChild("TextRoot", rowGo.transform);
            RectTransform textRect = textRoot.GetComponent<RectTransform>();
            ModUiLayout.Stretch(textRect);
            textRect.offsetMin = new Vector2(16f, 10f);
            textRect.offsetMax = new Vector2(-16f, -10f);

            Component label = ModUiFactory.AddText(
                textRoot,
                assets,
                SaveSlotPickerRowText.Compose(entry),
                17f,
                ModUiFontStyle.Normal);
            ModUiText.SetColor(label, assets.TextColor);
            ModUiText.SetAlignment(label, upperLeft: true);
            ModUiText.ConfigureTextLayout(label, wordWrap: true, ModUiText.OverflowOverflow);
            ModUiText.EnableRichText(label);

            SaveSlotPickerRow row = rowGo.AddComponent<SaveSlotPickerRow>();
            row.Initialize(entry, bg, label, assets, onSelected, onDoubleClicked);
            rowGo.AddComponent<ModUiScrollForwarder>();
            WireRowFeedback(button, row, assets);
            return row;
        }

        private static void WireRowFeedback(Button button, SaveSlotPickerRow row, ModUiAssets assets)
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
