using System;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;
using ReluProtocol;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class TramSavePickerController
    {
        private const string Feature = "ExtendedSaveSlots";

        private static readonly Action<string> HostButtonHandler = static _ => { };

        private static MainMenu? _mainMenu;
        private static UIPrefab_MainMenu? _mainMenuUi;
        private static SaveSlotPickerPanel? _panel;
        private static UIPrefab_PublicRoomList? _activeSavePickerList;

        internal static bool IsActive => ModConfig.EnableExtendedSaveSlots.Value;

        internal static bool IsSavePickerOpen { get; private set; }

        internal static UIPrefab_PublicRoomList? ActiveSavePickerList => _activeSavePickerList;

        internal static SaveSlotPickerPanel? Panel => _panel;

        internal static bool IsSavePickerList(UIPrefab_PublicRoomList? list) =>
            list != null && Panel?.List != null && ReferenceEquals(list, Panel.List);

        internal static void SetSavePickerOpen(bool open, UIPrefab_PublicRoomList? list)
        {
            IsSavePickerOpen = open;
            _activeSavePickerList = open ? list : null;
        }

        internal static bool TryGetCachedSave(int slotId, out MMSaveGameData? data)
        {
            if (_panel != null && _panel.TryGetCachedSave(slotId, out data))
            {
                return true;
            }

            data = null;
            return false;
        }

        internal static bool TryGetRowContext(Steamworks.CSteamID rowKey, out SaveSlotRowContext? context)
        {
            if (_panel != null && _panel.TryGetRowContext(rowKey, out context))
            {
                return true;
            }

            context = null;
            return false;
        }

        internal static void OnMainMenuStarted(MainMenu mainMenu, UIPrefab_MainMenu mainMenuUi)
        {
            _mainMenu = mainMenu;
            _mainMenuUi = mainMenuUi;
            RestoreMainMenuRoot();
            RefreshMenuMode();
        }

        internal static void OnMainMenuShown(UIPrefab_MainMenu mainMenuUi)
        {
            _mainMenuUi = mainMenuUi;
            RestoreMainMenuRoot();
            RefreshMenuMode();
        }

        internal static void ApplyExtendedMode()
        {
            if (_mainMenuUi == null)
            {
                return;
            }

            RestoreMainMenuRoot();

            if (!TryEnsurePanelReady())
            {
                ModLog.Warn(Feature, "Failed to initialize save picker.");
                return;
            }

            ConfigureExtendedMainMenuButtons();
            ApplyFunnyHostButtonLabel();
            HideLoadButton();
        }

        internal static void ApplyVanillaMode()
        {
            if (_panel?.IsOpen == true)
            {
                _panel.Close();
            }

            if (_mainMenuUi != null)
            {
                SaveSlotUiShellCustomizer.RestoreMainMenuDimming(_mainMenuUi);
            }

            if (_mainMenu != null && _mainMenuUi != null)
            {
                VanillaMainMenuWiring.Restore(_mainMenu, _mainMenuUi);
            }
            else if (_mainMenuUi != null)
            {
                _mainMenuUi.UE_LoadButton.gameObject.SetActive(true);
            }
        }

        internal static void TryHandleHostButtonClick(UIPrefab_MainMenu mainMenuUi)
        {
            if (!IsActive)
            {
                return;
            }

            _mainMenuUi = mainMenuUi;
            ConfigureExtendedMainMenuButtons();

            if (!TryEnsurePanelReady())
            {
                ModLog.Warn(Feature, "Save picker is not ready; falling back to vanilla New Tram.");
                TryOpenVanillaNewTram();
                return;
            }

            if (_panel!.TryOpen())
            {
                ModLog.Info(Feature, "Save picker opened.");
                return;
            }

            ModLog.Warn(Feature, "Save picker failed to open; falling back to vanilla New Tram.");
            TryOpenVanillaNewTram();
        }

        internal static void OpenPicker()
        {
            if (!IsActive)
            {
                return;
            }

            if (!TryEnsurePanelReady())
            {
                ModLog.Warn(Feature, "Save picker is not ready; could not open.");
                return;
            }

            if (_panel!.TryOpen())
            {
                ModLog.Info(Feature, "Save picker opened.");
            }
        }

        private static void RefreshMenuMode()
        {
            if (IsActive)
            {
                ApplyExtendedMode();
            }
            else
            {
                ApplyVanillaMode();
            }
        }

        private static void TryOpenVanillaNewTram()
        {
            Type? newTramType = AccessTools.TypeByName("UIPrefab_NewTram");
            if (newTramType == null)
            {
                ModLog.Warn(Feature, "Vanilla New Tram UI type not found.");
                return;
            }

            UIPrefabScript? newTram = FindUiInstance(newTramType);
            if (newTram == null)
            {
                ModLog.Warn(Feature, "Vanilla New Tram UI instance not found.");
                return;
            }

            UIManager? uiManager = SaveSlotGameAccess.TryGetUiManager();
            if (uiManager == null)
            {
                return;
            }

            EventSystem.current?.SetSelectedGameObject(null);
            SaveSlotGameAccess.TryGetPdata()?.SaveSlotID = -1;

            AccessTools.Method(newTramType, "InitSaveInfoList")?.Invoke(newTram, null);

            if (!uiManager.ui_escapeStack.Contains(newTram))
            {
                uiManager.ui_escapeStack.Add(newTram);
            }

            newTram.Show();
        }

        private static UIPrefabScript? FindUiInstance(Type uiType)
        {
            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(uiType))
            {
                if (obj is UIPrefabScript script)
                {
                    return script;
                }
            }

            return null;
        }

        private static bool TryEnsurePanelReady()
        {
            if (_panel != null)
            {
                return true;
            }

            if (_mainMenuUi == null)
            {
                ModLog.Warn(Feature, "Main menu UI reference is missing.");
                return false;
            }

            _mainMenu ??= FindMainMenu();
            UIPrefab_LoadTram? loadTram = SaveSlotGameAccess.TryFindHiddenLoadTram();
            UIPrefab_NewTram? newTram = SaveSlotGameAccess.TryFindHiddenNewTram();
            UIPrefab_NewTramPopUp? newTramPopUp = SaveSlotGameAccess.TryFindHiddenNewTramPopUp();

            if (_mainMenu == null || loadTram == null || newTram == null || newTramPopUp == null)
            {
                ModLog.Warn(Feature, "MainMenu or hidden tram UI references not found.");
                return false;
            }

            loadTram.Hide();
            newTram.Hide();
            newTramPopUp.Hide();

            _panel = new SaveSlotPickerPanel(_mainMenu, _mainMenuUi, loadTram, newTram, newTramPopUp);
            return true;
        }

        private static void ConfigureExtendedMainMenuButtons()
        {
            if (_mainMenuUi == null)
            {
                return;
            }

            _mainMenuUi.OnHostButton = HostButtonHandler;
            MainMenuButtonWiring.RegisterHandler(
                _mainMenuUi,
                _mainMenuUi.UE_HostButton,
                HostButtonHandler,
                UIPrefab_MainMenu.UEID_HostButton);

            MainMenuButtonWiring.RegisterHandler(
                _mainMenuUi,
                _mainMenuUi.UE_LoadButton,
                static _ => { },
                UIPrefab_MainMenu.UEID_LoadButton);
        }

        private static void HideLoadButton()
        {
            if (_mainMenuUi?.UE_LoadButton != null)
            {
                _mainMenuUi.UE_LoadButton.gameObject.SetActive(false);
            }
        }

        private static void RestoreMainMenuRoot()
        {
            if (_mainMenuUi?.UE_rootNode != null)
            {
                _mainMenuUi.UE_rootNode.gameObject.SetActive(true);
            }
        }

        private static void ApplyFunnyHostButtonLabel()
        {
            if (_mainMenuUi?.UE_HostButton == null)
            {
                return;
            }

            ApplyLabelToButton(_mainMenuUi.UE_HostButton.gameObject, FunnyTramMenuLabels.PickRandom());
        }

        private static void ApplyLabelToButton(GameObject buttonRoot, string label)
        {
            Component? text = SaveSlotTextHelper.FindTextComponent(buttonRoot);
            if (text == null)
            {
                ModLog.Warn(Feature, "Could not find button label text to replace.");
                return;
            }

            SaveSlotTextHelper.SetText(text, label);
        }

#pragma warning disable CS0618
        private static MainMenu? FindMainMenu() => UnityEngine.Object.FindObjectOfType<MainMenu>();
#pragma warning restore CS0618
    }
}
