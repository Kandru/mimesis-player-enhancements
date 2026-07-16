using MimesisPlayerEnhancement.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.Replays
{
    internal sealed class ReplayPickerUi : MonoBehaviour
    {
        private ModUiAssets _assets = ModUiAssets.Fallback;
        private ModScrollList _scrollList = null!;
        private GameObject? _emptyLabel;
        private readonly List<ReplayPickerRow> _rows = [];
        private ReplayPickerRow? _selectedRow;
        private Button _playButton = null!;
        private Button _deleteButton = null!;

        internal event Action<ReplayPickerRow>? RowSelected;
        internal event Action<ReplayPickerRow>? RowDoubleClicked;
        internal event Action? BackClicked;
        internal event Action? DeleteClicked;
        internal event Action? PlayClicked;

        internal static ReplayPickerUi? Create(Transform parent, UIPrefab_MainMenu mainMenu)
        {
            ModUiAssets assets = ModUiAssets.Fallback;
            UIPrefab_LoadTram? loadTram = SaveSlotGameAccess.TryFindHiddenLoadTram();
            if (loadTram != null && ModUiAssets.TryCaptureFromMainMenu(mainMenu, loadTram, out ModUiAssets captured))
            {
                assets = captured;
            }

            GameObject rootGo = ModUiRoot.CreateUiRoot(parent, "ReplayPickerUi");
            ReplayPickerUi ui = rootGo.AddComponent<ReplayPickerUi>();
            ui._assets = assets;
            ui.Build(rootGo.transform);
            rootGo.SetActive(false);
            return ui;
        }

        internal bool IsVisible => gameObject.activeInHierarchy;

        internal void Show() => gameObject.SetActive(true);

        internal void Hide() => gameObject.SetActive(false);

        internal void RebuildRows(IReadOnlyList<ReplayLibraryEntry> entries)
        {
            ClearRows();

            if (entries.Count == 0)
            {
                if (_emptyLabel == null)
                {
                    _emptyLabel = _scrollList
                        .CreatePlaceholderLabel(_assets, "No replays found. Enable Replays and finish a dungeon run as host.")
                        .gameObject;
                }

                _emptyLabel.SetActive(true);
                return;
            }

            _emptyLabel?.SetActive(false);

            foreach (ReplayLibraryEntry entry in entries)
            {
                ReplayPickerRow row = ReplayPickerRowFactory.CreateRow(
                    _scrollList.Content,
                    _assets,
                    entry,
                    OnRowSelected,
                    OnRowDoubleClicked);
                _rows.Add(row);
            }

            _scrollList.ScrollToTop();
        }

        internal void SetSelection(ReplayPickerRow? row)
        {
            if (_selectedRow != null)
            {
                _selectedRow.SetSelected(selected: false);
            }

            _selectedRow = row;
            _selectedRow?.SetSelected(selected: true);
        }

        internal void SetActionButtons(bool playEnabled, bool deleteEnabled)
        {
            ModButton.SetEnabled(_playButton, playEnabled, _assets.TextColor, _assets.DisabledTextColor);
            ModButton.SetEnabled(_deleteButton, deleteEnabled, _assets.TextColor, _assets.DisabledTextColor);
        }

        private void Build(Transform root)
        {
            ModPage page = ModPage.Create(root, _assets);
            page.ContentBand.SetAsLastSibling();
            page.CreateTitle(_assets, "Replays");

            _scrollList = ModScrollList.Create(page.ContentBand);

            RectTransform actionRow = page.CreateActionButtonRow();
            _playButton = ModButton.Create(
                actionRow,
                _assets,
                "Play",
                expandWidth: true,
                () => PlayClicked?.Invoke());
            _deleteButton = ModButton.Create(
                actionRow,
                _assets,
                "Delete",
                expandWidth: true,
                () => DeleteClicked?.Invoke());

            RectTransform backRow = page.CreateBackButtonRow();
            ModButton.Create(
                backRow,
                _assets,
                "Back",
                expandWidth: false,
                () => BackClicked?.Invoke());
        }

        private void OnRowSelected(ReplayPickerRow row)
        {
            RowSelected?.Invoke(row);
        }

        private void OnRowDoubleClicked(ReplayPickerRow row)
        {
            RowDoubleClicked?.Invoke(row);
        }

        private void ClearRows()
        {
            foreach (ReplayPickerRow row in _rows)
            {
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            _rows.Clear();
            _selectedRow = null;
        }

        private void OnDestroy() => ClearRows();
    }
}
