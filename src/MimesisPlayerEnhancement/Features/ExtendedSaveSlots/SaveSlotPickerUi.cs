using UnityEngine;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal sealed class SaveSlotPickerUi : MonoBehaviour
    {
        private ModUiAssets _assets = ModUiAssets.Fallback;
        private ModScrollList _scrollList = null!;
        private GameObject? _emptyLabel;
        private readonly List<SaveSlotPickerRow> _rows = [];
        private SaveSlotPickerRow? _selectedRow;

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

        internal void RebuildRows(IReadOnlyList<SaveSlotEntry> entries)
        {
            ClearRows();

            if (entries.Count == 0)
            {
                if (_emptyLabel == null)
                {
                    _emptyLabel = _scrollList
                        .CreatePlaceholderLabel(_assets, ModL10n.Get("saveslots.empty_list"))
                        .gameObject;
                }

                _emptyLabel.SetActive(true);
                return;
            }

            if (_emptyLabel != null)
            {
                _emptyLabel.SetActive(false);
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

        private void Build(Transform root, UIPrefab_LoadTram loadTram)
        {
            ModPage page = ModPage.Create(root, _assets);
            page.ContentBand.SetAsLastSibling();

            string loadLabel = SaveSlotGameAccess.GetL10NText("UI_PREFAB_MAIN_MENU_LOAD_TRAM");
            string newLabel = SaveSlotGameAccess.GetL10NText("UI_PREFAB_MAIN_MENU_NEW_TRAM");
            page.CreateTitle(_assets, loadLabel + " / " + newLabel);

            _scrollList = ModScrollList.Create(page.ContentBand);

            RectTransform actionRow = page.CreateActionButtonRow();
            NewTramButton = ModButton.Create(
                actionRow,
                _assets,
                newLabel,
                expandWidth: true,
                () => NewTramClicked?.Invoke());
            DeleteButton = ModButton.Create(
                actionRow,
                _assets,
                ModL10n.Get("saveslots.delete"),
                expandWidth: true,
                () => DeleteClicked?.Invoke());
            LoadButton = ModButton.Create(
                actionRow,
                _assets,
                loadLabel,
                expandWidth: true,
                () => LoadClicked?.Invoke());

            RectTransform backRow = page.CreateBackButtonRow();
            string backLabel = ReadButtonLabel(loadTram.UE_ButtonClose.gameObject) ?? "Back";
            BackButton = ModButton.Create(
                backRow,
                _assets,
                backLabel,
                expandWidth: false,
                () => BackClicked?.Invoke());
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

        private static string? ReadButtonLabel(GameObject buttonRoot)
        {
            Component? text = ModUiText.FindTextComponent(buttonRoot);
            return ModUiText.GetText(text);
        }

        private void OnDestroy() => ClearRows();
    }
}
