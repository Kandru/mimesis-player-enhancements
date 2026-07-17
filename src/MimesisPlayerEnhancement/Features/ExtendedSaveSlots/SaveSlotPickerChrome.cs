using UnityEngine;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class SaveSlotPickerChrome
    {
        private const float DimAlpha = 0.45f;

        private static Image? _rootImage;
        private static Color _originalColor;
        private static bool _originalRaycastTarget;

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
            if (_rootImage != null)
            {
                _rootImage.color = _originalColor;
                _rootImage.raycastTarget = _originalRaycastTarget;
                _rootImage = null;
            }

            ForceRestoreMainMenuDimming(mainMenuUi);
        }

        internal static void ForceRestoreMainMenuDimming(UIPrefab_MainMenu? mainMenuUi)
        {
            if (mainMenuUi?.UE_rootNode == null)
            {
                return;
            }

            Image root = mainMenuUi.UE_rootNode;
            Color color = root.color;
            color.a = 1f;
            root.color = color;
            root.raycastTarget = true;
        }
    }
}
