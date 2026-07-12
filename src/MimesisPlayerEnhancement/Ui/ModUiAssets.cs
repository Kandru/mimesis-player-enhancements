using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Ui
{
    /// <summary>
    /// Sprites, font template, colors and SFX ids cloned from vanilla UI prefabs, with
    /// solid-color fallbacks when capture fails. Capture once per menu session and pass
    /// the instance to the widget factories.
    /// </summary>
    internal sealed class ModUiAssets
    {
        /// <summary>Warm parchment tone matching the vanilla save-slot text.</summary>
        internal static readonly Color DefaultTextColor = new Color32(255, 240, 194, 255);

        internal static readonly ModUiAssets Fallback = new();

        internal static ModUiAssets FromTextSource(GameObject? source)
        {
            ModUiAssets assets = new();
            if (source != null)
            {
                assets.FontTemplate = ModUiText.FindTextComponent(source);
            }

            return assets;
        }

        internal Sprite? RowSprite { get; private set; }
        internal Image.Type RowImageType { get; private set; } = Image.Type.Sliced;
        internal Sprite? ButtonSprite { get; private set; }
        internal Image.Type ButtonImageType { get; private set; } = Image.Type.Sliced;
        internal Sprite? RowHighlightSprite { get; private set; }
        internal Image.Type RowHighlightImageType { get; private set; } = Image.Type.Sliced;
        internal Component? FontTemplate { get; private set; }
        internal Color TextColor { get; private set; } = DefaultTextColor;
        internal Color TitleTextColor { get; private set; } = Color.white;
        internal Color HoverTextColor { get; private set; } = Color.white;
        internal Color DisabledTextColor { get; private set; } = Color.gray;
        internal Color PanelBackdropColor { get; private set; } = new(0.06f, 0.06f, 0.08f, 0.94f);
        internal Color DimOverlayColor { get; private set; } = new(0f, 0f, 0f, 0.45f);
        internal string ButtonClickSfxId { get; private set; } = "ButtonClick";
        internal string ButtonHoverSfxId { get; private set; } = "ButtonHover";
        internal float RowHeight { get; private set; } = 112f;
        internal float ButtonWidth { get; private set; } = 200f;
        internal float ButtonHeight { get; private set; } = 48f;

        internal static bool TryCaptureFromMainMenu(
            UIPrefab_MainMenu mainMenu,
            UIPrefab_LoadTram loadTram,
            out ModUiAssets assets)
        {
            assets = new ModUiAssets();

            CaptureImage(loadTram.UE_SavedFile1?.GetComponent<Image>(), out Sprite? rowSprite, out Image.Type rowType);
            assets.RowSprite = rowSprite;
            assets.RowImageType = rowType;

            CaptureImage(
                loadTram.UE_ButtonClose?.GetComponent<Image>() ?? mainMenu.UE_HostButton?.GetComponent<Image>(),
                out Sprite? buttonSprite,
                out Image.Type buttonType);
            assets.ButtonSprite = buttonSprite;
            assets.ButtonImageType = buttonType;

            UiPrefab_RoomCard? roomCard = FindRoomCardTemplate();
            if (roomCard != null)
            {
                CaptureImage(roomCard.UE_mouseover, out Sprite? highlightSprite, out Image.Type highlightType);
                assets.RowHighlightSprite = highlightSprite;
                assets.RowHighlightImageType = highlightType;
            }

            if (mainMenu.UE_HostButton != null)
            {
                assets.FontTemplate = ModUiText.FindTextComponent(mainMenu.UE_HostButton.gameObject);
            }

            if (assets.FontTemplate == null && loadTram.UE_SavedFile1 != null)
            {
                assets.FontTemplate = ModUiText.FindTextComponent(loadTram.UE_SavedFile1.gameObject);
            }

            RectTransform? rowRect = loadTram.UE_SavedFile1?.GetComponent<RectTransform>();
            if (rowRect != null && rowRect.rect.height > 1f)
            {
                assets.RowHeight = Mathf.Max(rowRect.rect.height, 104f);
            }

            RectTransform? buttonRect = loadTram.UE_ButtonClose?.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                if (buttonRect.rect.width > 1f)
                {
                    assets.ButtonWidth = buttonRect.rect.width;
                }

                if (buttonRect.rect.height > 1f)
                {
                    assets.ButtonHeight = buttonRect.rect.height;
                }
            }

            UIManager? uiManager = ModUiGameAccess.TryGetUiManager();
            if (uiManager != null)
            {
                PropertyInfo? hoverColorProp = typeof(UIManager).GetProperty(
                    "mouseOverTextColor",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (hoverColorProp?.GetValue(uiManager) is Color hoverColor)
                {
                    assets.HoverTextColor = hoverColor;
                }
            }

            return assets.FontTemplate != null || assets.RowSprite != null;
        }

        internal void ApplyFont(Component? textComponent)
        {
            if (textComponent == null || FontTemplate == null)
            {
                return;
            }

            System.Type textType = textComponent.GetType();
            CopyProperty(FontTemplate, textComponent, "font");
            CopyProperty(FontTemplate, textComponent, "fontSharedMaterial");
            CopyProperty(FontTemplate, textComponent, "fontMaterial");

            PropertyInfo? extraPaddingProp = textType.GetProperty(
                "extraPadding",
                BindingFlags.Instance | BindingFlags.Public);
            extraPaddingProp?.SetValue(textComponent, true, null);
        }

        private static void CopyProperty(Component source, Component target, string propertyName)
        {
            PropertyInfo? targetProp = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo? sourceProp = source.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            if (targetProp == null || sourceProp == null)
            {
                return;
            }

            object? value = sourceProp.GetValue(source);
            if (value != null)
            {
                targetProp.SetValue(target, value);
            }
        }

        private static void CaptureImage(Image? image, out Sprite? sprite, out Image.Type imageType)
        {
            sprite = image?.sprite;
            imageType = image?.type ?? Image.Type.Sliced;
        }

#pragma warning disable CS0618
        private static UiPrefab_RoomCard? FindRoomCardTemplate()
        {
            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll(typeof(UiPrefab_RoomCard)))
            {
                if (obj is UiPrefab_RoomCard card)
                {
                    return card;
                }
            }

            return null;
        }
#pragma warning restore CS0618
    }
}
