using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal sealed class SaveSlotPickerButtons
    {
        internal Button Back { get; set; } = null!;
        internal Button NewTram { get; set; } = null!;
        internal Button Delete { get; set; } = null!;
        internal Button Load { get; set; } = null!;
    }

    internal static class SaveSlotUiShellCustomizer
    {
        private const float DimAlpha = 0.45f;
        private const float ButtonSpacing = 12f;
        private static readonly Color PanelBackdropColor = new(0.06f, 0.06f, 0.08f, 0.94f);

        private static Image? _rootImage;
        private static Color _originalColor;
        private static bool _originalRaycastTarget;

        internal static SaveSlotPickerButtons CreateFooterButtons(UIPrefab_PublicRoomList list)
        {
            EnsureInteractiveElementsVisible(list);

            Button back = list.UE_ButtonBack;
            Button newTram = CloneFooterButton(back, "SavePicker_NewTram");
            Button delete = CloneFooterButton(back, "SavePicker_Delete");
            Button load = CloneFooterButton(back, "SavePicker_Load");

            ApplyButtonLabel(newTram.gameObject, SaveSlotGameAccess.GetL10NText("UI_PREFAB_MAIN_MENU_NEW_TRAM"));
            ApplyButtonLabel(delete.gameObject, "Delete");
            ApplyButtonLabel(load.gameObject, SaveSlotGameAccess.GetL10NText("UI_PREFAB_MAIN_MENU_LOAD_TRAM"));

            return new SaveSlotPickerButtons
            {
                Back = back,
                NewTram = newTram,
                Delete = delete,
                Load = load,
            };
        }

        internal static void ApplyVisualCustomization(UIPrefab_PublicRoomList list, SaveSlotPickerButtons? buttons)
        {
            ActivateHierarchy(list.gameObject);
            StripBakedTitleBackground(list);
            HideUnusedChrome(list);
            ApplyCustomTitle(list);
            EnsureInteractiveElementsVisible(list);

            if (buttons != null)
            {
                EnsureFooterButtonsVisible(buttons);
                ArrangeFooterButtons([buttons.Back, buttons.NewTram, buttons.Delete, buttons.Load]);
            }
        }

        internal static void ApplyMainMenuDimming(UIPrefab_MainMenu mainMenuUi)
        {
            _rootImage = mainMenuUi.UE_rootNode;
            _originalColor = _rootImage.color;
            _originalRaycastTarget = _rootImage.raycastTarget;

            Color dimmed = _originalColor;
            dimmed.a = DimAlpha;
            _rootImage.color = dimmed;
            _rootImage.raycastTarget = false;
        }

        internal static void RestoreMainMenuDimming(UIPrefab_MainMenu mainMenuUi)
        {
            if (_rootImage == null)
            {
                return;
            }

            _rootImage.color = _originalColor;
            _rootImage.raycastTarget = _originalRaycastTarget;
            _rootImage = null;
        }

        internal static void SetButtonEnabled(Button button, bool enabled)
        {
            button.interactable = enabled;
            Component? text = SaveSlotTextHelper.FindTextComponent(button.gameObject);
            SaveSlotTextHelper.SetColor(text, enabled ? Color.white : Color.gray);
        }

        private static void StripBakedTitleBackground(UIPrefab_PublicRoomList list)
        {
            Component? title = GetTextComponent(list, "UE_Title");
            if (title != null)
            {
                Transform? current = title.transform.parent;
                while (current != null && current != list.transform)
                {
                    if (current.GetComponent<Image>() is Image ancestorImage
                        && !ShouldPreserveImage(ancestorImage))
                    {
                        ReplaceBannerImage(ancestorImage);
                    }

                    current = current.parent;
                }
            }

            foreach (Image image in list.GetComponentsInChildren<Image>(true))
            {
                if (ShouldPreserveImage(image))
                {
                    continue;
                }

                if (!IsLikelyTitleBanner(image))
                {
                    continue;
                }

                ReplaceBannerImage(image);
            }
        }

        private static void ReplaceBannerImage(Image image)
        {
            image.sprite = null;
            image.color = PanelBackdropColor;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }

        private static bool ShouldPreserveImage(Image image)
        {
            if (image.GetComponent<Button>() != null)
            {
                return true;
            }

            if (image.GetComponentInParent<ScrollRect>(true) != null)
            {
                return true;
            }

            string name = image.gameObject.name;
            if (name.Contains("mouse", StringComparison.OrdinalIgnoreCase)
                || name.Contains("flag", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Lock", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Status", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Repair", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Start", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            RectTransform rect = image.rectTransform;
            return rect.rect.width < 180f && rect.rect.height < 80f;
        }

        private static bool IsLikelyTitleBanner(Image image)
        {
            RectTransform rect = image.rectTransform;
            return rect.rect.width >= 300f || rect.rect.height >= 120f;
        }

        private static void ApplyCustomTitle(UIPrefab_PublicRoomList list)
        {
            Component? title = GetTextComponent(list, "UE_Title");
            if (title == null)
            {
                return;
            }

            title.gameObject.SetActive(true);
            ActivateHierarchy(title.gameObject);

            string loadLabel = SaveSlotGameAccess.GetL10NText("UI_PREFAB_MAIN_MENU_LOAD_TRAM");
            string newLabel = SaveSlotGameAccess.GetL10NText("UI_PREFAB_MAIN_MENU_NEW_TRAM");
            SaveSlotTextHelper.SetText(title, loadLabel + " / " + newLabel);
            SaveSlotTextHelper.SetColor(title, Color.white);
            title.transform.SetAsLastSibling();
        }

        private static void EnsureInteractiveElementsVisible(UIPrefab_PublicRoomList list)
        {
            ActivateHierarchy(list.gameObject);
            ActivateHierarchy(list.UE_Content.gameObject);
            ActivateHierarchy(list.UE_ButtonBack.gameObject);
            SetElementActive(list, "UE_Title", true);

            ScrollRect? scrollRect = list.UE_Content.GetComponentInParent<ScrollRect>(true);
            if (scrollRect != null)
            {
                ActivateHierarchy(scrollRect.gameObject);
            }
        }

        private static void EnsureFooterButtonsVisible(SaveSlotPickerButtons buttons)
        {
            ActivateHierarchy(buttons.Back.gameObject);
            ActivateHierarchy(buttons.NewTram.gameObject);
            ActivateHierarchy(buttons.Delete.gameObject);
            ActivateHierarchy(buttons.Load.gameObject);
        }

        private static void ActivateHierarchy(GameObject target)
        {
            Transform? current = target.transform;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                }

                current = current.parent;
            }
        }

        private static void HideUnusedChrome(UIPrefab_PublicRoomList list)
        {
            list.UE_ButtonRefresh.gameObject.SetActive(false);
            list.UE_popup.gameObject.SetActive(false);
            SetElementActive(list, "UE_SubTxt", false);
            SetElementActive(list, "UE_RecentCreatorCode", false);
            list.UE_confirm.gameObject.SetActive(false);
            list.UE_cancel.gameObject.SetActive(false);
        }

        private static Button CloneFooterButton(Button template, string name)
        {
            ActivateHierarchy(template.gameObject);

            Button clone = UnityEngine.Object.Instantiate(template, template.transform.parent);
            clone.gameObject.name = name;
            clone.transform.SetAsLastSibling();
            clone.onClick = new Button.ButtonClickedEvent();
            clone.gameObject.SetActive(true);
            return clone;
        }

        private static void ArrangeFooterButtons(IReadOnlyList<Button> buttons)
        {
            if (buttons.Count == 0)
            {
                return;
            }

            RectTransform anchor = buttons[0].GetComponent<RectTransform>();

            float step = Mathf.Max(anchor.rect.width, 140f) + ButtonSpacing;
            Vector2 origin = anchor.anchoredPosition;

            for (int i = 0; i < buttons.Count; i++)
            {
                RectTransform rect = buttons[i].GetComponent<RectTransform>();
                rect.anchoredPosition = origin + new Vector2(step * i, 0f);
                buttons[i].gameObject.SetActive(true);
            }
        }

        private static void ApplyButtonLabel(GameObject buttonRoot, string label)
        {
            Component? text = SaveSlotTextHelper.FindTextComponent(buttonRoot);
            SaveSlotTextHelper.SetText(text, label);
        }

        private static Component? GetTextComponent(UIPrefab_PublicRoomList list, string propertyName) =>
            typeof(UIPrefab_PublicRoomList).GetProperty(propertyName)?.GetValue(list) as Component;

        private static void SetElementActive(UIPrefab_PublicRoomList list, string propertyName, bool active)
        {
            if (GetTextComponent(list, propertyName) is Component component)
            {
                component.gameObject.SetActive(active);
            }
        }
    }
}
