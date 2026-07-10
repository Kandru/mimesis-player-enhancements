using UnityEngine;
using UnityEngine.EventSystems;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class VanillaMainMenuWiring
    {
        internal static void Restore(MainMenu menu, UIPrefab_MainMenu mainMenuUi)
        {
            UIPrefab_LoadTram? loadTram = SaveSlotGameAccess.TryFindHiddenLoadTram();
            UIPrefab_NewTram? newTram = SaveSlotGameAccess.TryFindHiddenNewTram();
            if (loadTram == null || newTram == null)
            {
                return;
            }

            UIManager? uiManager = SaveSlotGameAccess.TryGetUiManager();

            Action<string> hostHandler = _ =>
            {
                EventSystem.current?.SetSelectedGameObject(null);
                SaveSlotGameAccess.TryGetPdata()?.SaveSlotID = -1;
                newTram.InitSaveInfoList();
                if (uiManager != null && !uiManager.ui_escapeStack.Contains(newTram))
                {
                    uiManager.ui_escapeStack.Add(newTram);
                }

                newTram.Show();
            };

            Action<string> loadHandler = _ =>
            {
                EventSystem.current?.SetSelectedGameObject(null);
                loadTram.InitSaveInfoList();
                if (uiManager != null && !uiManager.ui_escapeStack.Contains(loadTram))
                {
                    uiManager.ui_escapeStack.Add(loadTram);
                }

                loadTram.Show();
            };

            mainMenuUi.OnHostButton = hostHandler;
            mainMenuUi.OnLoadButton = loadHandler;

            MainMenuButtonWiring.RegisterHandler(
                mainMenuUi,
                mainMenuUi.UE_HostButton,
                hostHandler,
                UIPrefab_MainMenu.UEID_HostButton);

            MainMenuButtonWiring.RegisterHandler(
                mainMenuUi,
                mainMenuUi.UE_LoadButton,
                loadHandler,
                UIPrefab_MainMenu.UEID_LoadButton);

            ApplyButtonLabel(
                mainMenuUi.UE_HostButton.gameObject,
                SaveSlotGameAccess.GetL10NText("UI_PREFAB_MAIN_MENU_NEW_TRAM"));
            ApplyButtonLabel(
                mainMenuUi.UE_LoadButton.gameObject,
                SaveSlotGameAccess.GetL10NText("UI_PREFAB_MAIN_MENU_LOAD_TRAM"));

            // Load button visibility/position is restored by the menu mirror.
        }

        private static void ApplyButtonLabel(GameObject buttonRoot, string label)
        {
            Component? text = Ui.ModUiText.FindTextComponent(buttonRoot);
            Ui.ModUiText.SetText(text, label);
        }
    }
}
