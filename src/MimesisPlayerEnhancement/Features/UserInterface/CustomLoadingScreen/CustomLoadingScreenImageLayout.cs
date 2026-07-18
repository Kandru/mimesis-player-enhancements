using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal enum CustomLoadingScreenScaleMode
    {
        Cover,
        FitHeight,
    }

    internal static class CustomLoadingScreenImageLayout
    {
        private const float AspectEpsilon = 0.001f;

        internal const float FallbackImageAspect = 16f / 9f;

        internal static CustomLoadingScreenScaleMode ResolveMode(float screenAspect) =>
            screenAspect >= CustomLoadingScreenConstants.UltrawideAspectThreshold
                ? CustomLoadingScreenScaleMode.FitHeight
                : CustomLoadingScreenScaleMode.Cover;

        internal static float GetScreenAspect(RectTransform parentRect)
        {
            Rect rect = parentRect.rect;
            if (rect.height > AspectEpsilon)
            {
                return rect.width / rect.height;
            }

            return Screen.width / Mathf.Max(Screen.height, 1);
        }

        internal static float GetImageAspect(Texture texture) =>
            texture.width / (float)Mathf.Max(texture.height, 1);

        internal static bool TryResolveImageAspect(Transform loadingRoot, out float aspect)
        {
            aspect = FallbackImageAspect;
            if (loadingRoot == null)
            {
                return false;
            }

            Transform? overlay = loadingRoot.Find(CustomLoadingScreenConstants.OverlayObjectName);
            if (overlay == null)
            {
                return false;
            }

            Transform? imageTransform = overlay.Find(CustomLoadingScreenConstants.OverlayImageObjectName);
            if (imageTransform == null)
            {
                return false;
            }

            RawImage? rawImage = imageTransform.GetComponent<RawImage>();
            if (rawImage?.texture == null)
            {
                return false;
            }

            aspect = GetImageAspect(rawImage.texture);
            return true;
        }

        internal static void ApplyContentBoundsInset(RectTransform target, RectTransform parent, float imageAspect)
        {
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = Vector2.zero;
            target.localScale = Vector3.one;

            float screenAspect = GetScreenAspect(parent);
            if (ResolveMode(screenAspect) == CustomLoadingScreenScaleMode.Cover)
            {
                target.offsetMin = new Vector2(0f, 0f);
                target.offsetMax = new Vector2(0f, 0f);
                return;
            }

            float parentWidth = parent.rect.width;
            float parentHeight = parent.rect.height;
            float targetWidth = parentHeight * imageAspect;
            float horizontalInset = Mathf.Max((parentWidth - targetWidth) * 0.5f, 0f);
            target.offsetMin = new Vector2(horizontalInset, 0f);
            target.offsetMax = new Vector2(-horizontalInset, 0f);
        }

        internal static Rect ComputeCoverUvRect(float imageAspect, float screenAspect)
        {
            if (Mathf.Abs(imageAspect - screenAspect) <= AspectEpsilon)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            if (imageAspect > screenAspect)
            {
                float uvWidth = screenAspect / imageAspect;
                return new Rect((1f - uvWidth) * 0.5f, 0f, uvWidth, 1f);
            }

            float uvHeight = imageAspect / screenAspect;
            return new Rect(0f, (1f - uvHeight) * 0.5f, 1f, uvHeight);
        }

        internal static Rect ComputePanZoomUvRect(Rect baseUvRect, float zoom, float cycleT)
        {
            float zoomFactor = Mathf.Max(zoom, 1f);
            float size = 1f / zoomFactor;
            float windowWidth = baseUvRect.width * size;
            float windowHeight = baseUvRect.height * size;
            float maxPanX = Mathf.Max(baseUvRect.width - windowWidth, 0f);
            float maxPanY = Mathf.Max(baseUvRect.height - windowHeight, 0f);
            float centerX = maxPanX * 0.5f;
            float centerY = maxPanY * 0.5f;
            float panX = centerX + Mathf.Sin(cycleT * Mathf.PI * 2f) * maxPanX * 0.04f;
            float panY = centerY + Mathf.Cos(cycleT * Mathf.PI * 2f) * maxPanY * 0.03f;
            return new Rect(baseUvRect.x + panX, baseUvRect.y + panY, windowWidth, windowHeight);
        }

        internal static void StretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        internal static void Apply(RawImage image, Texture? texture, RectTransform parentRect)
        {
            if (texture == null)
            {
                return;
            }

            RectTransform contentRect = image.rectTransform;
            float screenAspect = GetScreenAspect(parentRect);
            float imageAspect = GetImageAspect(texture);

            if (ResolveMode(screenAspect) == CustomLoadingScreenScaleMode.Cover)
            {
                StretchRect(contentRect);
                image.uvRect = ComputeCoverUvRect(imageAspect, screenAspect);
                return;
            }

            ApplyFitHeightRect(contentRect, imageAspect, parentRect);
            image.uvRect = new Rect(0f, 0f, 1f, 1f);
        }

        private static void ApplyFitHeightRect(RectTransform contentRect, float imageAspect, RectTransform parentRect)
        {
            float parentHeight = parentRect.rect.height;
            float targetWidth = parentHeight * imageAspect;

            contentRect.anchorMin = new Vector2(0.5f, 0f);
            contentRect.anchorMax = new Vector2(0.5f, 1f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(targetWidth, 0f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.localScale = Vector3.one;
        }
    }
}
