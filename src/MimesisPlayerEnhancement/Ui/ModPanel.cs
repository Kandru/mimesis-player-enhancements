using UnityEngine;
using UnityEngine.UI;

namespace MimesisPlayerEnhancement.Ui
{
    /// <summary>
    /// Dim overlay with a centered panel — for small dialogs that should not cover
    /// the whole screen the way <see cref="ModPage"/> does.
    /// </summary>
    internal sealed class ModPanel
    {
        internal GameObject Overlay { get; }
        internal RectTransform Panel { get; }

        private ModPanel(GameObject overlay, RectTransform panel)
        {
            Overlay = overlay;
            Panel = panel;
        }

        internal static ModPanel Create(
            Transform parent,
            ModUiAssets assets,
            float widthFraction = 0.5f,
            float heightFraction = 0.4f)
        {
            GameObject overlayGo = ModUiLayout.CreateChild("DimOverlay", parent);
            ModUiLayout.Stretch(overlayGo.GetComponent<RectTransform>());

            Image dim = overlayGo.AddComponent<Image>();
            dim.color = assets.DimOverlayColor;
            dim.raycastTarget = true;

            float halfWidth = Mathf.Clamp01(widthFraction) * 0.5f;
            float halfHeight = Mathf.Clamp01(heightFraction) * 0.5f;
            RectTransform panel = ModUiLayout.CreateBand(
                overlayGo.transform,
                "Panel",
                0.5f - halfWidth,
                0.5f - halfHeight,
                0.5f + halfWidth,
                0.5f + halfHeight);

            Image bg = panel.gameObject.AddComponent<Image>();
            bg.color = assets.PanelBackdropColor;
            bg.raycastTarget = true;

            return new ModPanel(overlayGo, panel);
        }
    }
}
