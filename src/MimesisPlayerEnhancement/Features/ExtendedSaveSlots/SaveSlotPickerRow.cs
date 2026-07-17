using System.Collections;
using MimesisPlayerEnhancement.Ui;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal sealed class SaveSlotPickerRow : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Color RowNormalColor = new(0.10f, 0.09f, 0.08f, 0.92f);
        private static readonly Color RowHoverColor = new(0.16f, 0.14f, 0.11f, 0.98f);
        private static readonly Color RowSelectedColor = new(0.30f, 0.26f, 0.17f, 1f);

        private SaveSlotEntry _entry = null!;
        private Image _background = null!;
        private Component _label = null!;
        private ModUiAssets _assets = ModUiAssets.Fallback;
        private Action<SaveSlotPickerRow>? _onSelected;
        private Action<SaveSlotPickerRow>? _onDoubleClicked;
        private Coroutine? _blinkCoroutine;
        private bool _selected;
        private bool _hovered;

        internal SaveSlotEntry Entry => _entry;
        internal int SlotId => _entry.SlotId;

        internal void Initialize(
            SaveSlotEntry entry,
            Image background,
            Component label,
            ModUiAssets assets,
            Action<SaveSlotPickerRow> onSelected,
            Action<SaveSlotPickerRow> onDoubleClicked)
        {
            _entry = entry;
            _background = background;
            _label = label;
            _assets = assets;
            _onSelected = onSelected;
            _onDoubleClicked = onDoubleClicked;
            RefreshText();
            ApplyVisualState();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (eventData.clickCount >= 2)
            {
                SaveSlotGameAccess.TryPlaySfx(_assets.ButtonClickSfxId);
                _onDoubleClicked?.Invoke(this);
                return;
            }

            if (eventData.clickCount == 1)
            {
                SaveSlotGameAccess.TryPlaySfx(_assets.ButtonClickSfxId);
                _onSelected?.Invoke(this);
            }
        }

        internal void SetSelected(bool selected)
        {
            _selected = selected;
            ApplyVisualState();
        }

        internal void SetHovered(bool hovered)
        {
            _hovered = hovered;
            ApplyVisualState();
        }

        internal void RefreshText()
        {
            ModUiText.SetText(
                _label,
                SaveSlotPickerRowText.Compose(_entry, SaveSlotPickerRowText.Line2Style.Normal));
        }

        internal void BlinkVersionWarning()
        {
            if (_blinkCoroutine != null)
            {
                StopCoroutine(_blinkCoroutine);
            }

            _blinkCoroutine = StartCoroutine(BlinkVersionWarningCoroutine());
        }

        private IEnumerator BlinkVersionWarningCoroutine()
        {
            const float halfPeriod = 0.125f;

            for (int blink = 0; blink < 4; blink++)
            {
                ModUiText.SetText(
                    _label,
                    SaveSlotPickerRowText.Compose(_entry, SaveSlotPickerRowText.Line2Style.VersionBlinkHidden));
                yield return new WaitForSeconds(halfPeriod);
                ModUiText.SetText(
                    _label,
                    SaveSlotPickerRowText.Compose(_entry, SaveSlotPickerRowText.Line2Style.VersionBlink));
                yield return new WaitForSeconds(halfPeriod);
            }

            ModUiText.SetText(
                _label,
                SaveSlotPickerRowText.Compose(_entry, SaveSlotPickerRowText.Line2Style.VersionBlink));
            _blinkCoroutine = null;
        }

        private void ApplyVisualState()
        {
            if (_background != null)
            {
                _background.color = _selected
                    ? RowSelectedColor
                    : _hovered
                        ? RowHoverColor
                        : RowNormalColor;
            }

            ModUiText.SetColor(_label, _assets.TextColor);
        }
    }
}
