using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    internal static class InGameMenuExtendedSlots
    {
        private const string Feature = "MorePlayers";
        private const int VanillaPlayerRows = 4;

        private static readonly MethodInfo OnClickSpeakButtonMethod =
            AccessTools.Method(typeof(UIPrefab_InGameMenu), "OnClickSpeakButton");

        private static readonly MethodInfo OnClickPlayerInfoButtonMethod =
            AccessTools.Method(typeof(UIPrefab_InGameMenu), "OnClickPlayerInfoButton");

        private static readonly MethodInfo OpenKickPlayerPopupMethod =
            AccessTools.Method(typeof(UIPrefab_InGameMenu), "OpenKickPlayerPopup");

        internal static void SyncFromConfig()
        {
            UIPrefab_InGameMenu? menu = JoinAnytimeHub.GetInGameMenu();
            if (menu != null)
            {
                ApplyCapToMenu(menu);
            }
        }

        internal static void ApplyCapToMenu(UIPrefab_InGameMenu menu)
        {
            int targetSlots = ModConfig.EnableMorePlayers.Value
                ? MorePlayersPatchHelpers.GetMaxPlayers()
                : VanillaPlayerRows;

            TrimExtendedSlots(menu, targetSlots);
            ResizeTempVolumeList(menu, targetSlots);
        }

        internal static void EnsureExtendedSlots(UIPrefab_InGameMenu menu)
        {
            if (!ModConfig.EnableMorePlayers.Value)
            {
                return;
            }

            int targetSlots = MorePlayersPatchHelpers.GetMaxPlayers();
            if (targetSlots <= VanillaPlayerRows
                || menu.playerUIElements == null
                || menu.playerUIElements.Count < VanillaPlayerRows)
            {
                return;
            }

            UIPrefab_InGameMenu.PlayerUIElement template = menu.playerUIElements[1];
            if (template.container == null)
            {
                return;
            }

            Transform rowParent = template.container.transform.parent;
            int startIndex = menu.playerUIElements.Count;
            for (int slotIndex = startIndex; slotIndex < targetSlots; slotIndex++)
            {
                GameObject cloneContainer = UnityEngine.Object.Instantiate(template.container, rowParent);
                cloneContainer.name = $"MorePlayersPlayer{slotIndex + 1}";
                cloneContainer.SetActive(false);

                UIPrefab_InGameMenu.PlayerUIElement element = BindPlayerElement(cloneContainer, template);
                menu.playerUIElements.Add(element);
            }

            if (startIndex < targetSlots)
            {
                RewireExtendedPlayerButtons(menu);
                ModLog.Debug(Feature, $"InGameMenu extended to {targetSlots} player row slots.");
            }
        }

        private static UIPrefab_InGameMenu.PlayerUIElement BindPlayerElement(
            GameObject cloneContainer,
            UIPrefab_InGameMenu.PlayerUIElement template)
        {
            UIPrefab_InGameMenu.PlayerUIElement element = new()
            {
                container = cloneContainer,
                avatarButton = FindTwin(template.avatarButton, cloneContainer),
                volumeSlider = FindTwin(template.volumeSlider, cloneContainer),
                speakButton = FindTwin(template.speakButton, cloneContainer),
                infoButton = FindTwin(template.infoButton, cloneContainer),
                kickButton = FindTwin(template.kickButton, cloneContainer),
                pingImage = FindTwin(template.pingImage, cloneContainer),
            };

            FieldInfo? nickNameField = typeof(UIPrefab_InGameMenu.PlayerUIElement).GetField("nickNameText");
            Component? templateNickName = nickNameField?.GetValue(template) as Component;
            if (nickNameField != null && templateNickName != null)
            {
                nickNameField.SetValue(element, FindTwinComponent(templateNickName, cloneContainer));
            }

            return element;
        }

        private static Component? FindTwinComponent(Component? templateComponent, GameObject cloneRoot)
        {
            if (templateComponent == null)
            {
                return null;
            }

            string targetName = templateComponent.gameObject.name;
            foreach (Component candidate in cloneRoot.GetComponentsInChildren<Component>(true))
            {
                if (candidate.gameObject.name == targetName && candidate.GetType() == templateComponent.GetType())
                {
                    return candidate;
                }
            }

            return null;
        }

        private static T? FindTwin<T>(T? templateComponent, GameObject cloneRoot)
            where T : Component
        {
            if (templateComponent == null)
            {
                return null;
            }

            string targetName = templateComponent.gameObject.name;
            foreach (T candidate in cloneRoot.GetComponentsInChildren<T>(true))
            {
                if (candidate.gameObject.name == targetName)
                {
                    return candidate;
                }
            }

            return null;
        }

        internal static void RewireExtendedPlayerButtons(UIPrefab_InGameMenu menu)
        {
            if (menu.playerUIElements == null)
            {
                return;
            }

            for (int index = VanillaPlayerRows; index < menu.playerUIElements.Count; index++)
            {
                WirePlayerRowButtons(menu, menu.playerUIElements[index], index);
            }
        }

        private static void WirePlayerRowButtons(
            UIPrefab_InGameMenu menu,
            UIPrefab_InGameMenu.PlayerUIElement element,
            int index)
        {
            WireButton(element.speakButton, () =>
            {
                _ = OnClickSpeakButtonMethod?.Invoke(menu, [index]);
            });

            WireButton(element.infoButton, () =>
            {
                _ = OnClickPlayerInfoButtonMethod?.Invoke(menu, [index]);
            });

            WireButton(element.avatarButton, () =>
            {
                _ = OnClickPlayerInfoButtonMethod?.Invoke(menu, [index]);
            });

            WireButton(element.kickButton, () =>
            {
                _ = OpenKickPlayerPopupMethod?.Invoke(menu, [index]);
            });
        }

        private static void WireButton(Button? button, Action handler)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => handler());
        }

        internal static void ResizeTempVolumeList(UIPrefab_InGameMenu menu)
        {
            int cap = ModConfig.EnableMorePlayers.Value
                ? MorePlayersPatchHelpers.GetMaxPlayers()
                : VanillaPlayerRows;
            ResizeTempVolumeList(menu, cap);
        }

        private static void ResizeTempVolumeList(UIPrefab_InGameMenu menu, int cap)
        {
            FieldInfo? field = AccessTools.Field(typeof(UIPrefab_InGameMenu), "tempVolumeList");
            if (field?.GetValue(menu) is not List<float> tempVolumeList)
            {
                return;
            }

            while (tempVolumeList.Count < cap)
            {
                tempVolumeList.Add(0f);
            }

            if (tempVolumeList.Count > cap)
            {
                tempVolumeList.RemoveRange(cap, tempVolumeList.Count - cap);
            }
        }

        private static void TrimExtendedSlots(UIPrefab_InGameMenu menu, int targetSlots)
        {
            if (menu.playerUIElements == null || menu.playerUIElements.Count <= targetSlots)
            {
                return;
            }

            while (menu.playerUIElements.Count > targetSlots)
            {
                int lastIndex = menu.playerUIElements.Count - 1;
                UIPrefab_InGameMenu.PlayerUIElement element = menu.playerUIElements[lastIndex];
                menu.playerUIElements.RemoveAt(lastIndex);
                if (element.container != null)
                {
                    UnityEngine.Object.Destroy(element.container);
                }
            }
        }
    }
}
