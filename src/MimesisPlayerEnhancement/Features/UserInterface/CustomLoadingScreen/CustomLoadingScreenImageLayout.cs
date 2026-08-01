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

        /// <summary>Reference resolution for wait.png roster strip design pixels.</summary>
        internal const float WaitPlayerBandDesignWidth = 1920f;
        internal const float WaitPlayerBandDesignHeight = 1080f;
        internal const float WaitPlayerBandBottomInsetPx = 10f;
        internal const float WaitPlayerBandHeightPx = 70f;
        internal const float WaitPlayerBandHorizontalInsetPx = 10f;

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
            if (!TryGetOverlayImage(loadingRoot, out RawImage rawImage, out _))
            {
                return false;
            }

            aspect = GetImageAspect(rawImage.texture);
            return true;
        }

        private static bool TryGetOverlayImage(
            Transform loadingRoot,
            out RawImage rawImage,
            out RectTransform overlayRect)
        {
            rawImage = null!;
            overlayRect = null!;
            if (loadingRoot == null)
            {
                return false;
            }

            Transform? overlay = loadingRoot.Find(CustomLoadingScreenConstants.OverlayObjectName);
            if (overlay == null)
            {
                return false;
            }

            overlayRect = overlay as RectTransform ?? overlay.GetComponent<RectTransform>();
            if (overlayRect == null)
            {
                return false;
            }

            Transform? imageTransform = overlay.Find(CustomLoadingScreenConstants.OverlayImageObjectName);
            if (imageTransform == null)
            {
                return false;
            }

            rawImage = imageTransform.GetComponent<RawImage>();
            return rawImage?.texture != null;
        }

        private static bool TryResolveVisibleUvRect(Transform loadingRoot, out Rect visibleUv)
        {
            visibleUv = new Rect(0f, 0f, 1f, 1f);
            if (!TryGetOverlayImage(loadingRoot, out RawImage rawImage, out RectTransform overlayRect))
            {
                return false;
            }

            float screenAspect = GetScreenAspect(overlayRect);
            if (ResolveMode(screenAspect) == CustomLoadingScreenScaleMode.FitHeight)
            {
                return true;
            }

            visibleUv = rawImage.uvRect;
            if (visibleUv.width <= AspectEpsilon || visibleUv.height <= AspectEpsilon)
            {
                visibleUv = ComputeCoverUvRect(GetImageAspect(rawImage.texture), screenAspect);
            }

            return true;
        }

        /// <summary>
        /// Design strip in normalized image Y: [bottomInset, bottomInset + height] / 1080.
        /// </summary>
        internal static float WaitPlayerBandDesignUvMin =>
            WaitPlayerBandBottomInsetPx / WaitPlayerBandDesignHeight;

        internal static float WaitPlayerBandDesignUvMax =>
            (WaitPlayerBandBottomInsetPx + WaitPlayerBandHeightPx) / WaitPlayerBandDesignHeight;

        internal static float ResolveWaitPlayerBandHorizontalInset(float boundsWidth) =>
            boundsWidth > AspectEpsilon
                ? WaitPlayerBandHorizontalInsetPx * (boundsWidth / WaitPlayerBandDesignWidth)
                : 0f;

        internal static float ResolveWaitPlayerBandFallbackHeight(float boundsHeight) =>
            boundsHeight > AspectEpsilon
                ? WaitPlayerBandHeightPx * (boundsHeight / WaitPlayerBandDesignHeight)
                : 0f;

        internal static void ResolveWaitPlayerBand(
            float boundsHeight,
            Rect visibleUv,
            out float bandBottomY,
            out float bandHeight)
        {
            bandBottomY = 0f;
            bandHeight = 0f;
            if (boundsHeight <= AspectEpsilon || visibleUv.height <= AspectEpsilon)
            {
                return;
            }

            float designMin = WaitPlayerBandDesignUvMin;
            float designMax = WaitPlayerBandDesignUvMax;
            float visibleMin = visibleUv.y;
            float visibleMax = visibleUv.y + visibleUv.height;
            float intersectMin = Mathf.Max(designMin, visibleMin);
            float intersectMax = Mathf.Min(designMax, visibleMax);
            if (intersectMax <= intersectMin + AspectEpsilon)
            {
                return;
            }

            bandBottomY = (intersectMin - visibleMin) / visibleUv.height * boundsHeight;
            bandHeight = (intersectMax - intersectMin) / visibleUv.height * boundsHeight;
        }

        internal static void ResolveWaitPlayerBand(
            RectTransform boundsRect,
            Transform loadingRoot,
            out float bandBottomY,
            out float bandHeight)
        {
            float boundsHeight = boundsRect.rect.height > 1f ? boundsRect.rect.height : Screen.height;
            if (TryResolveVisibleUvRect(loadingRoot, out Rect visibleUv))
            {
                ResolveWaitPlayerBand(boundsHeight, visibleUv, out bandBottomY, out bandHeight);
                return;
            }

            float screenAspect = boundsRect.parent is RectTransform parentRect
                ? GetScreenAspect(parentRect)
                : Screen.width / (float)Mathf.Max(Screen.height, 1);
            float imageAspect = FallbackImageAspect;
            TryResolveImageAspect(loadingRoot, out imageAspect);
            Rect fallbackUv = ResolveMode(screenAspect) == CustomLoadingScreenScaleMode.FitHeight
                ? new Rect(0f, 0f, 1f, 1f)
                : ComputeCoverUvRect(imageAspect, screenAspect);
            ResolveWaitPlayerBand(boundsHeight, fallbackUv, out bandBottomY, out bandHeight);
        }

        internal static void ApplyContentBoundsInset(RectTransform target, RectTransform parent, float imageAspect)
        {
            target.anchorMin = Vector2.zero;
            target.anchorMax = Vector2.one;
            target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = Vector2.zero;
            target.localScale = Vector3.one;

            float contentWidth = ResolveContentWidth(parent.rect.width, parent.rect.height, imageAspect);
            float horizontalInset = Mathf.Max((parent.rect.width - contentWidth) * 0.5f, 0f);
            target.offsetMin = new Vector2(horizontalInset, 0f);
            target.offsetMax = new Vector2(-horizontalInset, 0f);
        }

        internal static float ResolveContentWidth(float parentWidth, float parentHeight, float imageAspect)
        {
            if (parentWidth <= AspectEpsilon || parentHeight <= AspectEpsilon)
            {
                return 0f;
            }

            float screenAspect = parentWidth / parentHeight;
            return ResolveMode(screenAspect) == CustomLoadingScreenScaleMode.FitHeight
                ? parentHeight * imageAspect
                : parentWidth;
        }

        internal static bool TryResolveContentWidth(
            Transform loadingRoot,
            RectTransform referenceRect,
            out float contentWidth)
        {
            contentWidth = 0f;
            RectTransform? parentRect = referenceRect.parent as RectTransform ?? referenceRect;
            Canvas.ForceUpdateCanvases();

            if (TryGetOverlayImage(loadingRoot, out RawImage rawImage, out _))
            {
                float imageWidth = rawImage.rectTransform.rect.width;
                if (imageWidth > AspectEpsilon)
                {
                    contentWidth = imageWidth;
                    return true;
                }
            }

            float imageAspect = FallbackImageAspect;
            TryResolveImageAspect(loadingRoot, out imageAspect);
            contentWidth = ResolveContentWidth(parentRect.rect.width, parentRect.rect.height, imageAspect);
            return contentWidth > AspectEpsilon;
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
