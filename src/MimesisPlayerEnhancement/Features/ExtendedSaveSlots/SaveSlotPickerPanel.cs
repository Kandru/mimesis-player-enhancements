using UnityEngine;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal sealed class SaveSlotPickerPanel
    {
        private const string Feature = "ExtendedSaveSlots";

        private readonly MainMenu _mainMenu;
        private readonly UIPrefab_MainMenu _mainMenuUi;
        private readonly UIPrefab_LoadTram _loadTram;
        private readonly UIPrefab_NewTram _newTram;
        private readonly UIPrefab_NewTramPopUp _newTramPopUp;

        private SaveSlotPickerUi? _ui;
        private readonly Dictionary<int, MMSaveGameData> _saveCache = new();
        private readonly Dictionary<int, SaveSlotEntry> _entriesBySlot = new();
        private SaveSlotPickerRow? _selectedRow;
        private int _firstFreeManualSlot = -1;

        internal SaveSlotPickerPanel(
            MainMenu mainMenu,
            UIPrefab_MainMenu mainMenuUi,
            UIPrefab_LoadTram loadTram,
            UIPrefab_NewTram newTram,
            UIPrefab_NewTramPopUp newTramPopUp)
        {
            _mainMenu = mainMenu;
            _mainMenuUi = mainMenuUi;
            _loadTram = loadTram;
            _newTram = newTram;
            _newTramPopUp = newTramPopUp;
        }

        internal bool IsOpen => _ui != null && _ui.IsVisible;

        internal bool TryGetCachedSave(int slotId, out MMSaveGameData? data) =>
            _saveCache.TryGetValue(slotId, out data);

        internal bool TryGetEntry(int slotId, out SaveSlotEntry? entry) =>
            _entriesBySlot.TryGetValue(slotId, out entry);

        internal bool TryOpen()
        {
            Transform? parent = ModUiRoot.GetTop();
            if (parent == null)
            {
                ModLog.Warn(Feature, "UIManager Top layer unavailable; cannot show save picker.");
                return false;
            }

            if (_ui == null)
            {
                _ui = SaveSlotPickerUi.Create(parent, _mainMenuUi, _loadTram);
                if (_ui == null)
                {
                    ModLog.Warn(Feature, "Failed to create save picker UI.");
                    return false;
                }

                WireUiHandlers(_ui);
            }

            ClearSelection();
            TramSavePickerController.SetSavePickerOpen(true);
            RefreshSaveList();
            UpdateActionButtons();

            EventSystem.current?.SetSelectedGameObject(null);
            SaveSlotGameAccess.TryGetPdata()?.SaveSlotID = -1;
            SaveSlotPickerChrome.ApplyMainMenuDimming(_mainMenuUi);
            _ui.Show();
            return _ui.IsVisible;
        }

        internal void Close()
        {
            ClearSelection();
            SaveSlotPickerChrome.RestoreMainMenuDimming(_mainMenuUi);

            if (_ui != null)
            {
                _ui.Hide();
            }

            TramSavePickerController.SetSavePickerOpen(false);
        }

        internal void Dispose()
        {
            Close();

            if (_ui != null)
            {
                UnityEngine.Object.Destroy(_ui.gameObject);
                _ui = null;
            }

            _saveCache.Clear();
            _entriesBySlot.Clear();
            _selectedRow = null;
        }

        internal void SelectRow(SaveSlotPickerRow row)
        {
            _selectedRow = row;
            _ui?.SetSelection(row);
            UpdateActionButtons();
        }

        internal void HandleLoadSelected()
        {
            if (_selectedRow == null)
            {
                return;
            }

            HandleLoadRow(_selectedRow);
        }

        internal void HandleLoadRow(SaveSlotPickerRow row)
        {
            SaveSlotEntry entry = row.Entry;
            if (!entry.Display.IsVersionCompatible)
            {
                ModLog.Debug(Feature, $"Save slot {entry.SlotId} blocked by version mismatch.");
                row.BlinkVersionWarning();
                return;
            }

            SelectRow(row);
            MainMenuSessionBridge.TryLoadSaveAndCreateRoom(_mainMenu, _loadTram, entry.SlotId);
        }

        internal void HandleDeleteSelected()
        {
            if (_selectedRow == null)
            {
                return;
            }

            int slotId = _selectedRow.SlotId;
            if (!SaveSlotDeleteService.TryDeleteSave(_mainMenu, slotId))
            {
                ModLog.Warn(Feature, $"Failed to delete save slot {slotId}.");
                return;
            }

            ClearSelection();
            RefreshSaveList();
            UpdateActionButtons();
        }

        internal void HandleNewTram()
        {
            int slotId = _firstFreeManualSlot;
            if (slotId < 0)
            {
                ModLog.Warn(Feature, "All manual save slots are full.");
                return;
            }

            MainMenuSessionBridge.HandleNewGameSlotSelection(_mainMenu, _newTram, _newTramPopUp, slotId);
        }

        internal void RefreshSaveList()
        {
            if (_ui == null)
            {
                return;
            }

            _saveCache.Clear();
            _entriesBySlot.Clear();

            List<SaveSlotEntry> entries = SaveSlotRoomListMapper.BuildSaveEntries();
            SaveSlotPickerExtraStats.PopulateLine3Text(entries);
            _firstFreeManualSlot = SaveSlotDiscovery.FindFirstFreeManualSlot();
            foreach (SaveSlotEntry entry in entries)
            {
                _saveCache[entry.SlotId] = entry.Data;
                _entriesBySlot[entry.SlotId] = entry;
            }

            _ui.RebuildRows(entries);
        }

        private void WireUiHandlers(SaveSlotPickerUi ui)
        {
            ui.BackClicked += () =>
            {
                Close();
                EventSystem.current?.SetSelectedGameObject(null);
            };
            ui.NewTramClicked += HandleNewTram;
            ui.DeleteClicked += HandleDeleteSelected;
            ui.LoadClicked += HandleLoadSelected;
            ui.RowSelected += SelectRow;
            ui.RowDoubleClicked += HandleLoadRow;
        }

        private void UpdateActionButtons()
        {
            if (_ui == null)
            {
                return;
            }

            bool hasSelection = _selectedRow != null;
            _ui.SetActionButtons(
                loadEnabled: hasSelection,
                deleteEnabled: hasSelection,
                newTramEnabled: _firstFreeManualSlot >= 0);
        }

        private void ClearSelection()
        {
            _selectedRow = null;
            _ui?.SetSelection(null);
        }
    }
}
