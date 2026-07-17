using System.Reflection;
using HarmonyLib;
using MimesisPlayerEnhancement.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.Replays
{
    internal static class ReplayMenuSession
    {
        private static readonly FieldInfo? CurrentBgField =
            AccessTools.Field(typeof(UIManager), "currnetBG");

        internal static void HideForPlayback()
        {
            ReplayPickerController.CloseIfOpen();
            ClearBlockingOverlays();
        }

        internal static void HideLiveMainMenu()
        {
            ClearBlockingOverlays();
        }

        internal static void EnforceHiddenDuringPlayback()
        {
            ClearBlockingOverlays();
        }

        internal static void Clear()
        {
        }

        internal static void ClearBlockingOverlays()
        {
            ClearTitleBackground();
            DestroyMainMenuInstances();
            HideSceneLoading();
        }

        private static void ClearTitleBackground()
        {
            UIManager? uiManager = ModUiGameAccess.TryGetUiManager();
            if (uiManager != null && CurrentBgField?.GetValue(uiManager) is GameObject currentBg && currentBg != null)
            {
                UnityEngine.Object.Destroy(currentBg);
                CurrentBgField.SetValue(uiManager, null);
            }

#pragma warning disable CS0618
            UIPrefab_titleBG[] titleBackgrounds =
                UnityEngine.Object.FindObjectsOfType<UIPrefab_titleBG>();
#pragma warning restore CS0618
            foreach (UIPrefab_titleBG titleBackground in titleBackgrounds)
            {
                if (titleBackground != null)
                {
                    UnityEngine.Object.Destroy(titleBackground.gameObject);
                }
            }
        }

        private static void DestroyMainMenuInstances()
        {
#pragma warning disable CS0618
            UIPrefab_MainMenu[] menus = UnityEngine.Object.FindObjectsOfType<UIPrefab_MainMenu>();
#pragma warning restore CS0618
            foreach (UIPrefab_MainMenu menu in menus)
            {
                DestroyMainMenu(menu);
            }
        }

        private static void DestroyMainMenu(UIPrefab_MainMenu? menu)
        {
            if (menu == null)
            {
                return;
            }

            Image? root = menu.UE_rootNode;
            if (root != null)
            {
                Color color = root.color;
                color.a = 0f;
                root.color = color;
                root.raycastTarget = false;
            }

            UnityEngine.Object.Destroy(menu.gameObject);
        }

        private static void HideSceneLoading()
        {
            UIManager? uiManager = ModUiGameAccess.TryGetUiManager();
            if (uiManager?.ui_sceneloading != null)
            {
                uiManager.ui_sceneloading.Hide();
            }
        }
    }
}
