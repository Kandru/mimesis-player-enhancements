using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    internal static class InGameMenuPlayerGrid
    {
        private const string Feature = "MorePlayers";
        private const int VanillaPlayerRows = 4;
        private const float DefaultRowHeight = 56f;

        private static readonly MethodInfo OnClickSpeakButtonMethod =
            AccessTools.Method(typeof(UIPrefab_InGameMenu), "OnClickSpeakButton");

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
                WireSpeakButton(menu, element, slotIndex);
            }

            if (startIndex < targetSlots)
            {
                ModLog.Debug(Feature, $"InGameMenu extended to {targetSlots} player row slots.");
            }

            InGameMenuPlayerListLayout.Apply(menu);
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

        private static void WireSpeakButton(
            UIPrefab_InGameMenu menu,
            UIPrefab_InGameMenu.PlayerUIElement element,
            int index)
        {
            if (element.speakButton == null || OnClickSpeakButtonMethod == null)
            {
                return;
            }

            element.speakButton.onClick.AddListener(() =>
            {
                _ = OnClickSpeakButtonMethod.Invoke(menu, [index]);
            });
        }

        internal static void ApplyRowLayoutElements(UIPrefab_InGameMenu menu, float rowHeight)
        {
            foreach (UIPrefab_InGameMenu.PlayerUIElement row in menu.playerUIElements)
            {
                ApplyRowLayoutElement(row, rowHeight);
            }
        }

        private static void ApplyRowLayoutElement(UIPrefab_InGameMenu.PlayerUIElement row, float rowHeight)
        {
            if (row.container == null)
            {
                return;
            }

            LayoutElement layoutElement = row.container.GetComponent<LayoutElement>()
                ?? row.container.AddComponent<LayoutElement>();
            layoutElement.minHeight = rowHeight;
            layoutElement.preferredHeight = rowHeight;
        }

        internal static float MeasureRowHeight(UIPrefab_InGameMenu menu)
        {
            if (menu.playerUIElements.Count >= 2)
            {
                RectTransform? first = menu.playerUIElements[0].container.GetComponent<RectTransform>();
                RectTransform? second = menu.playerUIElements[1].container.GetComponent<RectTransform>();
                if (first != null && second != null)
                {
                    float step = Mathf.Abs(first.anchoredPosition.y - second.anchoredPosition.y);
                    if (step >= 1f)
                    {
                        return step;
                    }

                    if (first.rect.height >= 1f)
                    {
                        return first.rect.height;
                    }
                }
            }

            return DefaultRowHeight;
        }

        internal static void ResizeTempVolumeList(UIPrefab_InGameMenu menu)
        {
            if (!ModConfig.EnableMorePlayers.Value)
            {
                return;
            }

            FieldInfo? field = AccessTools.Field(typeof(UIPrefab_InGameMenu), "tempVolumeList");
            if (field?.GetValue(menu) is not List<float> tempVolumeList)
            {
                return;
            }

            int cap = MorePlayersPatchHelpers.GetMaxPlayers();
            while (tempVolumeList.Count < cap)
            {
                tempVolumeList.Add(0f);
            }

            if (tempVolumeList.Count > cap)
            {
                tempVolumeList.RemoveRange(cap, tempVolumeList.Count - cap);
            }
        }
    }
}
