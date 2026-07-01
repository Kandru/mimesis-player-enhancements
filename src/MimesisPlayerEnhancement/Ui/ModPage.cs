using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Ui
{
    /// <summary>
    /// Full-screen overlay page: opaque backdrop with a title band, a content band,
    /// an action button band and a back/close band. Features compose widgets into the
    /// bands; destroy <see cref="Root"/> to tear the page down.
    /// </summary>
    internal sealed class ModPage
    {
        private const float HorizontalInset = 0.04f;
        private const float TitleTop = 0.98f;
        private const float TitleBottom = 0.90f;
        private const float ContentTop = 0.86f;
        private const float ContentBottom = 0.24f;
        private const float ActionTop = 0.22f;
        private const float ActionBottom = 0.14f;
        private const float BackTop = 0.12f;
        private const float BackBottom = 0.05f;

        internal GameObject Root { get; }
        internal RectTransform TitleBand { get; }
        internal RectTransform ContentBand { get; }
        internal RectTransform ActionBand { get; }
        internal RectTransform BackBand { get; }

        private ModPage(
            GameObject root,
            RectTransform titleBand,
            RectTransform contentBand,
            RectTransform actionBand,
            RectTransform backBand)
        {
            Root = root;
            TitleBand = titleBand;
            ContentBand = contentBand;
            ActionBand = actionBand;
            BackBand = backBand;
        }

        internal static ModPage Create(Transform parent, ModUiAssets assets)
        {
            GameObject panel = ModUiLayout.CreateChild("Panel", parent);
            ModUiLayout.Stretch(panel.GetComponent<RectTransform>());

            Image bg = panel.AddComponent<Image>();
            bg.color = assets.PanelBackdropColor;
            bg.raycastTarget = true;

            Transform panelTransform = panel.transform;
            return new ModPage(
                panel,
                ModUiLayout.CreateBand(panelTransform, "TitleBand", HorizontalInset, TitleBottom, 1f - HorizontalInset, TitleTop),
                ModUiLayout.CreateBand(panelTransform, "ScrollBand", HorizontalInset, ContentBottom, 1f - HorizontalInset, ContentTop),
                ModUiLayout.CreateBand(panelTransform, "ActionBand", HorizontalInset, ActionBottom, 1f - HorizontalInset, ActionTop),
                ModUiLayout.CreateBand(panelTransform, "BackBand", HorizontalInset, BackBottom, 1f - HorizontalInset, BackTop));
        }

        internal Component CreateTitle(ModUiAssets assets, string text)
        {
            GameObject go = ModUiLayout.CreateChild("Title", TitleBand);
            ModUiLayout.Stretch(go.GetComponent<RectTransform>());

            Component label = ModUiFactory.AddText(go, assets, text, 34f, ModUiFontStyle.Bold);
            ModUiText.SetColor(label, assets.TitleTextColor);
            ModUiText.SetMiddleCenterAlignment(label);
            ModUiText.ConfigureTextLayout(label, wordWrap: true, ModUiText.OverflowEllipsis);
            return label;
        }

        internal RectTransform CreateActionButtonRow()
        {
            GameObject rowGo = ModUiLayout.CreateChild("ActionButtonRow", ActionBand);
            ModUiLayout.Stretch(rowGo.GetComponent<RectTransform>());

            HorizontalLayoutGroup layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            ModUiLayout.SetEnumProperty(layout, "childAlignment", 4);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(0, 0, 4, 4);
            return rowGo.GetComponent<RectTransform>();
        }

        internal RectTransform CreateBackButtonRow()
        {
            GameObject rowGo = ModUiLayout.CreateChild("BackButtonRow", BackBand);
            ModUiLayout.Stretch(rowGo.GetComponent<RectTransform>());

            HorizontalLayoutGroup layout = rowGo.AddComponent<HorizontalLayoutGroup>();
            ModUiLayout.SetEnumProperty(layout, "childAlignment", 4);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.padding = new RectOffset(0, 0, 4, 4);
            return rowGo.GetComponent<RectTransform>();
        }
    }
}
