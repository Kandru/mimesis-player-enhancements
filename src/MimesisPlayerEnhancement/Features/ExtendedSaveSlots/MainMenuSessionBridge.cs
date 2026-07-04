using System.Reflection;
using UnityEngine.EventSystems;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class MainMenuSessionBridge
    {
        private const string Feature = "ExtendedSaveSlots";

        private static MethodInfo? _tryLoadSaveAndCreateRoom;
        private static MethodInfo? _handleNewGameSlotSelection;

        internal static void TryLoadSaveAndCreateRoom(
            MainMenu menu,
            UIPrefab_LoadTram loadTram,
            int slotId)
        {
            MethodInfo? method = GetTryLoadSaveAndCreateRoom();
            if (method == null)
            {
                ModLog.Warn(Feature, "TryLoadSaveAndCreateRoom not found.");
                return;
            }

            TramSavePickerController.Panel?.Close();

            loadTram.InitSaveInfoList();
            _ = method.Invoke(menu, [loadTram, slotId]);
        }

        internal static void HandleNewGameSlotSelection(
            MainMenu menu,
            UIPrefab_NewTram newTram,
            UIPrefab_NewTramPopUp newTramPopUp,
            int slotId)
        {
            MethodInfo? method = GetHandleNewGameSlotSelection();
            if (method == null)
            {
                ModLog.Warn(Feature, "HandleNewGameSlotSelection not found.");
                return;
            }

            TramSavePickerController.Panel?.Close();
            EventSystem.current?.SetSelectedGameObject(null);
            PrepareNewTramSession(newTram, newTramPopUp);
            _ = method.Invoke(menu, [newTram, newTramPopUp, slotId]);
        }

        private static void PrepareNewTramSession(UIPrefab_NewTram newTram, UIPrefab_NewTramPopUp newTramPopUp)
        {
            SaveSlotGameAccess.TryGetPdata()?.SaveSlotID = -1;
            newTram.InitSaveInfoList();
            newTram.Hide();
            newTramPopUp.Hide();

            UIManager? uiManager = SaveSlotGameAccess.TryGetUiManager();
            if (uiManager == null)
            {
                return;
            }

            uiManager.ui_escapeStack.Remove(newTram);
            uiManager.ui_escapeStack.Remove(newTramPopUp);
        }

        private static MethodInfo? GetTryLoadSaveAndCreateRoom()
        {
            _tryLoadSaveAndCreateRoom ??= AccessTools.Method(
                typeof(MainMenu),
                "TryLoadSaveAndCreateRoom",
                [typeof(UIPrefab_LoadTram), typeof(int)]);

            return _tryLoadSaveAndCreateRoom;
        }

        private static MethodInfo? GetHandleNewGameSlotSelection()
        {
            _handleNewGameSlotSelection ??= AccessTools.Method(
                typeof(MainMenu),
                "HandleNewGameSlotSelection",
                [typeof(UIPrefab_NewTram), typeof(UIPrefab_NewTramPopUp), typeof(int)]);

            return _handleNewGameSlotSelection;
        }
    }
}
