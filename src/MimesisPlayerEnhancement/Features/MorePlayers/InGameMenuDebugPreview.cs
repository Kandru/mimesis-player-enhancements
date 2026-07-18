using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    internal static class InGameMenuDebugPreview
    {
        private const string Feature = "MorePlayers";

        private static readonly FieldInfo? NickNameTextField =
            AccessTools.Field(typeof(UIPrefab_InGameMenu.PlayerUIElement), "nickNameText");

        private static bool _active;

        internal static bool Show(IReadOnlyList<string> fakeNames)
        {
            UIManager? uiManager = ModUiGameAccess.TryGetUiManager();
            if (uiManager == null)
            {
                return false;
            }

            uiManager.OpenInGameMenu();
            UIPrefab_InGameMenu? menu = uiManager.inGameMenu;
            if (menu?.playerUIElements == null || menu.playerUIElements.Count == 0)
            {
                return false;
            }

            InGameMenuExtendedSlots.EnsureExtendedSlots(menu);

            for (int i = 0; i < menu.playerUIElements.Count; i++)
            {
                UIPrefab_InGameMenu.PlayerUIElement element = menu.playerUIElements[i];
                bool active = i < fakeNames.Count;
                element.SetActive(active);
                if (!active)
                {
                    continue;
                }

                SetNickName(element, fakeNames[i]);

                if (element.kickButton != null)
                {
                    element.kickButton.gameObject.SetActive(false);
                }

                if (element.speakButton != null)
                {
                    element.speakButton.interactable = false;
                }

                if (element.infoButton != null)
                {
                    element.infoButton.interactable = false;
                }

                if (element.avatarButton != null)
                {
                    element.avatarButton.interactable = false;
                }

                if (element.volumeSlider != null)
                {
                    element.volumeSlider.interactable = false;
                }
            }

            if (!InGameMenuPlayerListOverlay.ShowForDebug(menu))
            {
                ModLog.Debug(Feature, "In-game menu debug preview — extended overlay unavailable");
            }

            _active = true;
            return true;
        }

        internal static void Hide()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            ModUiGameAccess.TryGetUiManager()?.CloseInGameMenu();
        }

        internal static void OnSessionEnded()
        {
            if (_active)
            {
                Hide();
            }
        }

        private static void SetNickName(UIPrefab_InGameMenu.PlayerUIElement element, string name)
        {
            if (NickNameTextField?.GetValue(element) is not Component nickNameComponent)
            {
                return;
            }

            MethodInfo? setText = nickNameComponent.GetType().GetMethod("SetText", [typeof(string)]);
            if (setText != null)
            {
                setText.Invoke(nickNameComponent, [name]);
                return;
            }

            PropertyInfo? textProperty = nickNameComponent.GetType().GetProperty("text");
            textProperty?.SetValue(nickNameComponent, name);
        }
    }
}
