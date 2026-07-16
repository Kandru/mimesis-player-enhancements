using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.Replays
{
    internal sealed class ReplayPickerRow : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Color RowNormalColor = new(0.10f, 0.09f, 0.08f, 0.92f);
        private static readonly Color RowHoverColor = new(0.16f, 0.14f, 0.11f, 0.98f);
        private static readonly Color RowSelectedColor = new(0.30f, 0.26f, 0.17f, 1f);

        internal ReplayLibraryEntry? Entry { get; private set; }

        private Image? _background;
        private Outline? _outline;
        private Action<ReplayPickerRow>? _onSelected;
        private Action<ReplayPickerRow>? _onDoubleClicked;
        private bool _selected;
        private bool _hovered;

        internal void Initialize(
            ReplayLibraryEntry entry,
            Image background,
            Outline outline,
            Action<ReplayPickerRow> onSelected,
            Action<ReplayPickerRow> onDoubleClicked)
        {
            Entry = entry;
            _background = background;
            _outline = outline;
            _onSelected = onSelected;
            _onDoubleClicked = onDoubleClicked;
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
                _onDoubleClicked?.Invoke(this);
                return;
            }

            if (eventData.clickCount == 1)
            {
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

        private void ApplyVisualState()
        {
            if (_background != null)
            {
                _background.color = _selected
                    ? RowSelectedColor
                    : _hovered ? RowHoverColor : RowNormalColor;
            }

            if (_outline != null)
            {
                _outline.enabled = _selected;
            }
        }
    }
}
