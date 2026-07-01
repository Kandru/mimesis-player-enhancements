using System.Collections.Generic;
using System.Reflection;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;
using Steamworks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal sealed class SaveSlotPickerPanel
    {
        private const string Feature = "ExtendedSaveSlots";
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? RoomListDataField =
            typeof(UIPrefab_PublicRoomList).GetField("roomListData", InstanceFlags);

        private static readonly MethodInfo? SetRoomListUiMethod =
            typeof(UIPrefab_PublicRoomList).GetMethod("SetRoomListUI", InstanceFlags);

        private static readonly FieldInfo? JoinTramUiField =
            typeof(UIPrefab_PublicRoomList).GetField("joinTramUI", InstanceFlags);

        private readonly MainMenu _mainMenu;
        private readonly UIPrefab_MainMenu _mainMenuUi;
        private readonly UIPrefab_LoadTram _loadTram;
        private readonly UIPrefab_NewTram _newTram;
        private readonly UIPrefab_NewTramPopUp _newTramPopUp;

        private UIPrefab_PublicRoomList? _list;
        private SaveSlotPickerButtons? _buttons;
        private readonly Dictionary<int, MMSaveGameData> _saveCache = new();
        private Dictionary<CSteamID, SaveSlotRowContext> _rowContexts = new();
        private CSteamID? _selectedRowKey;
        private UiPrefab_RoomCard? _selectedCard;

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

        internal bool IsOpen => _list != null && _list.gameObject.activeInHierarchy;

        internal UIPrefab_PublicRoomList? List => _list;

        internal bool TryGetCachedSave(int slotId, out MMSaveGameData? data) =>
            _saveCache.TryGetValue(slotId, out data);

        internal bool TryGetRowContext(CSteamID rowKey, out SaveSlotRowContext? context) =>
            _rowContexts.TryGetValue(rowKey, out context);

        internal bool TryOpen()
        {
            UIManager? uiManager = SaveSlotGameAccess.TryGetUiManager();
            if (uiManager == null)
            {
                ModLog.Warn(Feature, "UIManager unavailable; cannot show save picker.");
                return false;
            }

            if (_list == null)
            {
                _list = SaveSlotGameAccess.CreateSavePickerShell(uiManager);
                if (_list == null)
                {
                    ModLog.Warn(Feature, "Failed to create save picker shell from public room list prefab.");
                    return false;
                }

                JoinTramUiField?.SetValue(_list, null);
                _buttons = SaveSlotUiShellCustomizer.CreateFooterButtons(_list);
                ConfigureButtonHandlers(_buttons);
            }

            ClearSelection();
            TramSavePickerController.SetSavePickerOpen(true, _list);
            SaveSlotSteamListGuard.DetachSavePickerFromSteam(_list);
            RefreshSaveList();
            UpdateActionButtons();

            EventSystem.current?.SetSelectedGameObject(null);
            SaveSlotGameAccess.TryGetPdata()?.SaveSlotID = -1;
            SaveSlotUiShellCustomizer.ApplyMainMenuDimming(_mainMenuUi);

            if (!uiManager.ui_escapeStack.Contains(_list))
            {
                uiManager.ui_escapeStack.Add(_list);
            }

            _list.Show();
            SaveSlotUiShellCustomizer.ApplyVisualCustomization(_list, _buttons);
            return _list.gameObject.activeInHierarchy;
        }

        internal void Close()
        {
            ClearSelection();
            SaveSlotUiShellCustomizer.RestoreMainMenuDimming(_mainMenuUi);

            if (_list == null)
            {
                TramSavePickerController.SetSavePickerOpen(false, null);
                return;
            }

            UIManager? uiManager = SaveSlotGameAccess.TryGetUiManager();
            uiManager?.ui_escapeStack.Remove(_list);
            _list.Hide();
            TramSavePickerController.SetSavePickerOpen(false, _list);
        }

        internal void SelectRow(CSteamID rowKey, UiPrefab_RoomCard card)
        {
            if (_selectedCard != null)
            {
                SetCardSelected(_selectedCard, selected: false);
            }

            _selectedRowKey = rowKey;
            _selectedCard = card;
            SetCardSelected(card, selected: true);
            UpdateActionButtons();
        }

        internal void HandleLoadSelected()
        {
            if (_selectedRowKey == null
                || !_rowContexts.TryGetValue(_selectedRowKey.Value, out SaveSlotRowContext? context))
            {
                return;
            }

            if (!context.Entry.Display.IsVersionCompatible)
            {
                ModLog.Debug(Feature, $"Save slot {context.SlotId} blocked by version mismatch.");
                if (context.SlotId <= 3)
                {
                    _loadTram.InitSaveInfoList();
                    _loadTram.CanNotLoadSaveData(context.SlotId);
                }

                return;
            }

            MainMenuSessionBridge.TryLoadSaveAndCreateRoom(_mainMenu, _list!, _loadTram, context.SlotId);
        }

        internal void HandleDeleteSelected()
        {
            if (_selectedRowKey == null
                || !_rowContexts.TryGetValue(_selectedRowKey.Value, out SaveSlotRowContext? context))
            {
                return;
            }

            if (!SaveSlotDeleteService.TryDeleteSave(_mainMenu, context.SlotId))
            {
                ModLog.Warn(Feature, $"Failed to delete save slot {context.SlotId}.");
                return;
            }

            ClearSelection();
            RefreshSaveList();
            UpdateActionButtons();
        }

        internal void HandleNewTram()
        {
            int slotId = SaveSlotDiscovery.FindFirstFreeManualSlot();
            if (slotId < 0)
            {
                ModLog.Warn(Feature, "All manual save slots are full.");
                return;
            }

            MainMenuSessionBridge.HandleNewGameSlotSelection(_mainMenu, _newTram, _newTramPopUp, slotId);
        }

        internal void RefreshSaveList()
        {
            if (_list == null || RoomListDataField?.GetValue(_list) is not List<PublicRoomListData> roomListData)
            {
                return;
            }

            _saveCache.Clear();
            SaveSlotEntry? autosave = SaveSlotDiscovery.TryLoadAutosave();
            if (autosave != null)
            {
                _saveCache[autosave.SlotId] = autosave.Data;
            }

            foreach (SaveSlotEntry entry in SaveSlotDiscovery.GetManualSaves())
            {
                _saveCache[entry.SlotId] = entry.Data;
            }

            List<PublicRoomListData> rows = SaveSlotRoomListMapper.BuildRoomListData(out _rowContexts);
            roomListData.Clear();
            roomListData.AddRange(rows);

            if (rows.Count == 0)
            {
                _list.SetEmptyListText();
                SetListElementActive(_list, "UE_EmptyListText", true);
                return;
            }

            SetListElementActive(_list, "UE_EmptyListText", false);
            SetRoomListUiMethod?.Invoke(_list, null);
        }

        private void ConfigureButtonHandlers(SaveSlotPickerButtons buttons)
        {
            buttons.Back.onClick.AddListener(() =>
            {
                Close();
                EventSystem.current?.SetSelectedGameObject(null);
            });
            buttons.NewTram.onClick.AddListener(HandleNewTram);
            buttons.Delete.onClick.AddListener(HandleDeleteSelected);
            buttons.Load.onClick.AddListener(HandleLoadSelected);
        }

        private void UpdateActionButtons()
        {
            if (_buttons == null)
            {
                return;
            }

            bool hasSelection = _selectedRowKey != null;
            SaveSlotUiShellCustomizer.SetButtonEnabled(_buttons.Delete, hasSelection);
            SaveSlotUiShellCustomizer.SetButtonEnabled(_buttons.Load, hasSelection);
            SaveSlotUiShellCustomizer.SetButtonEnabled(
                _buttons.NewTram,
                SaveSlotDiscovery.FindFirstFreeManualSlot() >= 0);
        }

        private void ClearSelection()
        {
            if (_selectedCard != null)
            {
                SetCardSelected(_selectedCard, selected: false);
            }

            _selectedRowKey = null;
            _selectedCard = null;
        }

        private static void SetCardSelected(UiPrefab_RoomCard card, bool selected)
        {
            card.UE_mouseover.color = selected
                ? new Color(1f, 1f, 1f, 1f)
                : new Color(0f, 0f, 0f, 0f);
        }

        private static void SetListElementActive(UIPrefab_PublicRoomList list, string propertyName, bool active)
        {
            if (typeof(UIPrefab_PublicRoomList).GetProperty(propertyName)?.GetValue(list) is Component component)
            {
                component.gameObject.SetActive(active);
            }
        }
    }
}
