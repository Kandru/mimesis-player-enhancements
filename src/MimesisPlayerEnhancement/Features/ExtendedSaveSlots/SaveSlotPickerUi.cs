using System.Collections;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal sealed class SaveSlotPickerUi : MonoBehaviour
    {
        private ModUiAssets _assets = ModUiAssets.Fallback;
        private ModScrollList _scrollList = null!;
        private Component? _emptyLabel;
        private Component? _titleLabel;
        private GameObject? _backLabelSource;
        private readonly List<SaveSlotPickerRow> _rows = [];
        private SaveSlotPickerRow? _selectedRow;
        private Coroutine? _line3PopulateCoroutine;

        internal Button BackButton { get; private set; } = null!;
        internal Button NewTramButton { get; private set; } = null!;
        internal Button DeleteButton { get; private set; } = null!;
        internal Button LoadButton { get; private set; } = null!;

        internal event Action<SaveSlotPickerRow>? RowSelected;
        internal event Action<SaveSlotPickerRow>? RowDoubleClicked;
        internal event Action? BackClicked;
        internal event Action? NewTramClicked;
        internal event Action? DeleteClicked;
        internal event Action? LoadClicked;

        internal static SaveSlotPickerUi? Create(
            Transform parent,
            UIPrefab_MainMenu mainMenu,
            UIPrefab_LoadTram loadTram)
        {
            if (!ModUiAssets.TryCaptureFromMainMenu(mainMenu, loadTram, out ModUiAssets assets))
            {
                assets = ModUiAssets.Fallback;
            }

            GameObject rootGo = ModUiRoot.CreateUiRoot(parent, "SaveSlotPickerUi");
            SaveSlotPickerUi ui = rootGo.AddComponent<SaveSlotPickerUi>();
            ui._assets = assets;
            ui.Build(rootGo.transform, loadTram);
            rootGo.SetActive(false);
            return ui;
        }

        internal bool IsVisible => gameObject.activeInHierarchy;

        internal void Show() => gameObject.SetActive(true);

        internal void Hide() => gameObject.SetActive(false);

        internal SaveSlotPickerRow? GetSelectedRow() => _selectedRow;

        internal void RebuildRows(IReadOnlyList<SaveSlotEntry> entries, bool populateLine3Lazily = false)
        {
            StopLine3Populate();
            ClearRows();

            if (entries.Count == 0)
            {
                EnsureEmptyLabel();
                _emptyLabel!.gameObject.SetActive(true);
                return;
            }

            if (_emptyLabel != null)
            {
                _emptyLabel.gameObject.SetActive(false);
            }

            foreach (SaveSlotEntry entry in entries)
            {
                SaveSlotPickerRow row = SaveSlotRowFactory.CreateSlotRow(
                    _scrollList.Content,
                    _assets,
                    entry,
                    OnRowSelected,
                    OnRowDoubleClicked);
                _rows.Add(row);
            }

            _scrollList.ScrollToTop();

            if (populateLine3Lazily)
            {
                _line3PopulateCoroutine = StartCoroutine(PopulateLine3Coroutine(entries));
            }
            else
            {
                SaveSlotPickerExtraStats.PopulateLine3Text(entries);
                foreach (SaveSlotPickerRow row in _rows)
                {
                    row.RefreshText();
                }
            }
        }

        internal void SetSelection(SaveSlotPickerRow? row)
        {
            if (_selectedRow != null)
            {
                _selectedRow.SetSelected(selected: false);
            }

            _selectedRow = row;
            if (_selectedRow != null)
            {
                _selectedRow.SetSelected(selected: true);
            }
        }

        internal void SetActionButtons(bool loadEnabled, bool deleteEnabled, bool newTramEnabled)
        {
            ModButton.SetEnabled(LoadButton, loadEnabled, _assets.TextColor, _assets.DisabledTextColor);
            ModButton.SetEnabled(DeleteButton, deleteEnabled, _assets.TextColor, _assets.DisabledTextColor);
            ModButton.SetEnabled(NewTramButton, newTramEnabled, _assets.TextColor, _assets.DisabledTextColor);
        }

        internal void RefreshLocalizedLabels()
        {
            string loadLabel = SaveSlotGameAccess.GetL10NText("UI_PREFAB_MAIN_MENU_LOAD_TRAM");
            string newLabel = SaveSlotGameAccess.GetL10NText("UI_PREFAB_MAIN_MENU_NEW_TRAM");
            ModUiText.SetText(_titleLabel, loadLabel + " / " + newLabel);
            SetButtonLabel(NewTramButton, newLabel);
            SetButtonLabel(DeleteButton, ModL10n.Get("saveslots.delete"));
            SetButtonLabel(LoadButton, loadLabel);
            SetButtonLabel(BackButton, ReadButtonLabel(_backLabelSource) ?? "Back");

            if (_emptyLabel != null)
            {
                ModUiText.SetText(_emptyLabel, ModL10n.Get("saveslots.empty_list"));
            }

            foreach (SaveSlotPickerRow row in _rows)
            {
                if (row == null)
                {
                    continue;
                }

                row.Entry.Line3Text = SaveSlotPickerExtraStats.FormatLine3(row.SlotId);
                row.RefreshText();
            }
        }

        private IEnumerator PopulateLine3Coroutine(IReadOnlyList<SaveSlotEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                SaveSlotEntry entry = entries[i];
                entry.Line3Text = SaveSlotPickerExtraStats.FormatLine3(entry.SlotId);

                if (i < _rows.Count && _rows[i] != null && _rows[i].SlotId == entry.SlotId)
                {
                    _rows[i].RefreshText();
                }
                else
                {
                    foreach (SaveSlotPickerRow row in _rows)
                    {
                        if (row != null && row.SlotId == entry.SlotId)
                        {
                            row.RefreshText();
                            break;
                        }
                    }
                }

                yield return null;
            }

            _line3PopulateCoroutine = null;
        }

        private void Build(Transform root, UIPrefab_LoadTram loadTram)
        {
            _backLabelSource = loadTram.UE_ButtonClose.gameObject;
            ModPage page = ModPage.Create(root, _assets);
            page.ContentBand.SetAsLastSibling();

            _titleLabel = page.CreateTitle(_assets, string.Empty);
            _scrollList = ModScrollList.Create(page.ContentBand);

            RectTransform actionRow = page.CreateActionButtonRow();
            NewTramButton = ModButton.Create(actionRow, _assets, string.Empty, expandWidth: true, () => NewTramClicked?.Invoke());
            DeleteButton = ModButton.Create(actionRow, _assets, string.Empty, expandWidth: true, () => DeleteClicked?.Invoke());
            LoadButton = ModButton.Create(actionRow, _assets, string.Empty, expandWidth: true, () => LoadClicked?.Invoke());

            RectTransform backRow = page.CreateBackButtonRow();
            BackButton = ModButton.Create(backRow, _assets, string.Empty, expandWidth: false, () => BackClicked?.Invoke());

            RefreshLocalizedLabels();
        }

        private void EnsureEmptyLabel()
        {
            if (_emptyLabel != null)
            {
                return;
            }

            _emptyLabel = _scrollList.CreatePlaceholderLabel(_assets, ModL10n.Get("saveslots.empty_list"));
        }

        private static void SetButtonLabel(Button button, string label)
        {
            ModUiText.SetText(ModUiText.FindTextComponent(button.gameObject), label);
        }

        private void OnRowSelected(SaveSlotPickerRow row)
        {
            SetSelection(row);
            RowSelected?.Invoke(row);
        }

        private void OnRowDoubleClicked(SaveSlotPickerRow row)
        {
            SetSelection(row);
            RowDoubleClicked?.Invoke(row);
        }

        private void ClearRows()
        {
            foreach (SaveSlotPickerRow row in _rows)
            {
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            _rows.Clear();
            _selectedRow = null;
        }

        private void StopLine3Populate()
        {
            if (_line3PopulateCoroutine == null)
            {
                return;
            }

            StopCoroutine(_line3PopulateCoroutine);
            _line3PopulateCoroutine = null;
        }

        private static string? ReadButtonLabel(GameObject? buttonRoot)
        {
            if (buttonRoot == null)
            {
                return null;
            }

            return ModUiText.GetText(ModUiText.FindTextComponent(buttonRoot));
        }

        private void OnDestroy()
        {
            StopLine3Populate();
            ClearRows();
        }
    }
}
