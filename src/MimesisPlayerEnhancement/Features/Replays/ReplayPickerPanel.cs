using MimesisPlayerEnhancement.Ui;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MimesisPlayerEnhancement.Features.Replays
{
    internal sealed class ReplayPickerPanel
    {
        private const string Feature = "Replays";

        private readonly UIPrefab_MainMenu _mainMenuUi;
        private ReplayPickerUi? _ui;
        private readonly List<ReplayLibraryEntry> _entries = [];
        private ReplayPickerRow? _selectedRow;

        internal ReplayPickerPanel(UIPrefab_MainMenu mainMenuUi)
        {
            _mainMenuUi = mainMenuUi;
        }

        internal bool TryOpen()
        {
            Transform? parent = ModUiRoot.GetTop();
            if (parent == null)
            {
                ModLog.Warn(Feature, "UIManager Top layer unavailable; cannot show replay picker.");
                return false;
            }

            if (_ui == null)
            {
                _ui = ReplayPickerUi.Create(parent, _mainMenuUi);
                if (_ui == null)
                {
                    ModLog.Warn(Feature, "Failed to create replay picker UI.");
                    return false;
                }

                WireUiHandlers(_ui);
            }

            ClearSelection();
            ReplayPickerController.SetPickerOpen(true);
            RefreshReplayList();
            UpdateActionButtons();

            EventSystem.current?.SetSelectedGameObject(null);
            ReplayPickerChrome.ApplyMainMenuDimming(_mainMenuUi);
            _ui.Show();
            return _ui.IsVisible;
        }

        internal void Close()
        {
            ClearSelection();
            ReplayPickerChrome.RestoreMainMenuDimming(_mainMenuUi);
            _ui?.Hide();
            ReplayPickerController.SetPickerOpen(false);
        }

        internal void Dispose()
        {
            Close();

            if (_ui != null)
            {
                UnityEngine.Object.Destroy(_ui.gameObject);
                _ui = null;
            }

            _entries.Clear();
            _selectedRow = null;
        }

        private void WireUiHandlers(ReplayPickerUi ui)
        {
            ui.BackClicked += Close;
            ui.DeleteClicked += HandleDeleteSelected;
            ui.PlayClicked += HandlePlaySelected;
            ui.RowSelected += SelectRow;
            ui.RowDoubleClicked += HandlePlayRow;
        }

        private void RefreshReplayList()
        {
            _entries.Clear();
            _entries.AddRange(ReplayLibrary.ListEntries());
            _ui?.RebuildRows(_entries);
            UpdateActionButtons();
        }

        private void SelectRow(ReplayPickerRow row)
        {
            _selectedRow = row;
            _ui?.SetSelection(row);
            UpdateActionButtons();
        }

        private void HandlePlaySelected()
        {
            if (_selectedRow == null || _selectedRow.Entry == null)
            {
                return;
            }

            HandlePlayRow(_selectedRow);
        }

        private void HandlePlayRow(ReplayPickerRow row)
        {
            ReplayLibraryEntry? entry = row.Entry;
            if (entry == null)
            {
                return;
            }

            Close();
            ReplayPlaybackEngine.BeginPlayback(entry.PlayFilePath);
        }

        private void HandleDeleteSelected()
        {
            if (_selectedRow == null || _selectedRow.Entry == null)
            {
                return;
            }

            if (ReplayLibrary.TryDelete(_selectedRow.Entry))
            {
                ClearSelection();
                RefreshReplayList();
            }
        }

        private void UpdateActionButtons()
        {
            bool hasSelection = _selectedRow != null && _selectedRow.Entry != null;
            _ui?.SetActionButtons(playEnabled: hasSelection, deleteEnabled: hasSelection);
        }

        private void ClearSelection()
        {
            _selectedRow = null;
            _ui?.SetSelection(null);
        }
    }
}
