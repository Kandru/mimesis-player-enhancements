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

        private static readonly HashSet<int> ScrollLayoutApplied = [];
        private static readonly MethodInfo OnClickSpeakButtonMethod =
            AccessTools.Method(typeof(UIPrefab_InGameMenu), "OnClickSpeakButton");

        internal static void EnsureExtendedSlots(UIPrefab_InGameMenu menu)
        {
            if (!ModConfig.EnableMorePlayers.Value)
            {
                return;
            }

            int targetSlots = MorePlayersPatches.GetMaxPlayers();
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

            ApplyScrollLayout(menu);
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

        private static void ApplyScrollLayout(UIPrefab_InGameMenu menu)
        {
            if (menu.playerUIElements.Count <= VanillaPlayerRows)
            {
                return;
            }

            int menuId = menu.GetInstanceID();
            float rowHeight = MeasureRowHeight(menu);
            if (ScrollLayoutApplied.Contains(menuId))
            {
                ApplyRowLayoutElements(menu, rowHeight);
                return;
            }

            Transform firstRow = menu.playerUIElements[0].container.transform;
            Transform contentHost = firstRow.parent;
            if (contentHost is not RectTransform contentRect)
            {
                return;
            }

            float viewportHeight = rowHeight * VanillaPlayerRows;
            float contentHeight = rowHeight * menu.playerUIElements.Count;
            if (contentHeight <= viewportHeight + 1f)
            {
                LayoutRowsWithoutScroll(menu, rowHeight);
                ScrollLayoutApplied.Add(menuId);
                return;
            }

            Transform originalParent = contentHost.parent;
            int siblingIndex = contentHost.GetSiblingIndex();
            Vector2 anchoredPos = contentRect.anchoredPosition;
            Vector2 sizeDelta = contentRect.sizeDelta;
            Vector2 anchorMin = contentRect.anchorMin;
            Vector2 anchorMax = contentRect.anchorMax;
            Vector2 pivot = contentRect.pivot;

            GameObject scrollRootGo = new("MorePlayersPlayerListScroll", typeof(RectTransform));
            RectTransform scrollRoot = scrollRootGo.GetComponent<RectTransform>();
            scrollRoot.SetParent(originalParent, false);
            scrollRoot.SetSiblingIndex(siblingIndex);
            scrollRoot.anchorMin = anchorMin;
            scrollRoot.anchorMax = anchorMax;
            scrollRoot.pivot = pivot;
            scrollRoot.anchoredPosition = anchoredPos;
            scrollRoot.sizeDelta = new Vector2(sizeDelta.x, viewportHeight);

            GameObject viewportGo = new("Viewport", typeof(RectTransform), typeof(RectMask2D));
            RectTransform viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.SetParent(scrollRoot, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            contentHost.SetParent(viewportRect, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, contentHeight);

            VerticalLayoutGroup layout = contentHost.gameObject.GetComponent<VerticalLayoutGroup>()
                ?? contentHost.gameObject.AddComponent<VerticalLayoutGroup>();
            PropertyInfo? alignmentProperty = typeof(VerticalLayoutGroup).GetProperty("childAlignment");
            if (alignmentProperty != null)
            {
                alignmentProperty.SetValue(layout, Enum.ToObject(alignmentProperty.PropertyType, 1)); // UpperCenter
            }
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 0f;

            ContentSizeFitter fitter = contentHost.gameObject.GetComponent<ContentSizeFitter>()
                ?? contentHost.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (UIPrefab_InGameMenu.PlayerUIElement row in menu.playerUIElements)
            {
                ApplyRowLayoutElement(row, rowHeight);
            }

            ScrollRect scrollRect = scrollRootGo.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 20f;

            ScrollLayoutApplied.Add(menuId);
            ModLog.Debug(Feature, $"InGameMenu player list scroll enabled ({menu.playerUIElements.Count} rows).");
        }

        private static void LayoutRowsWithoutScroll(UIPrefab_InGameMenu menu, float rowHeight)
        {
            if (menu.playerUIElements.Count < 2)
            {
                return;
            }

            RectTransform? anchorRow = menu.playerUIElements[0].container.GetComponent<RectTransform>();
            RectTransform? nextRow = menu.playerUIElements[1].container.GetComponent<RectTransform>();
            if (anchorRow == null || nextRow == null)
            {
                return;
            }

            float step = Mathf.Abs(anchorRow.anchoredPosition.y - nextRow.anchoredPosition.y);
            if (step < 1f)
            {
                step = rowHeight;
            }

            Transform? parent = anchorRow.parent;
            for (int i = VanillaPlayerRows; i < menu.playerUIElements.Count; i++)
            {
                if (menu.playerUIElements[i].container.transform is not RectTransform rowRect || parent == null)
                {
                    continue;
                }

                rowRect.SetParent(parent, false);
                rowRect.anchorMin = anchorRow.anchorMin;
                rowRect.anchorMax = anchorRow.anchorMax;
                rowRect.pivot = anchorRow.pivot;
                rowRect.sizeDelta = anchorRow.sizeDelta;
                rowRect.anchoredPosition = anchorRow.anchoredPosition - new Vector2(0f, step * i);
            }
        }

        private static void ApplyRowLayoutElements(UIPrefab_InGameMenu menu, float rowHeight)
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

        private static float MeasureRowHeight(UIPrefab_InGameMenu menu)
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

            int cap = MorePlayersPatches.GetMaxPlayers();
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
