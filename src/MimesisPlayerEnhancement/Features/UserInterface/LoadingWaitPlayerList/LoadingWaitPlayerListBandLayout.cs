using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList
{
    /// <summary>
    /// Bottom roster strip metrics at a 1920×1080 design resolution.
    /// </summary>
    internal static class LoadingWaitPlayerListBandLayout
    {
        internal const float DesignWidth = 1920f;
        internal const float DesignHeight = 1080f;
        internal const float BottomInsetPx = 10f;
        internal const float HeightPx = 70f;
        internal const float HorizontalInsetPx = 10f;

        internal static readonly Color ShadeColor = new(0f, 0f, 0f, 0.7f);

        internal static float ResolveHorizontalInset(float boundsWidth) =>
            boundsWidth > 0.001f
                ? HorizontalInsetPx * (boundsWidth / DesignWidth)
                : 0f;

        internal static float ResolveBandHeight(float boundsHeight) =>
            boundsHeight > 0.001f
                ? HeightPx * (boundsHeight / DesignHeight)
                : 0f;

        internal static float ResolveBottomInset(float boundsHeight) =>
            boundsHeight > 0.001f
                ? BottomInsetPx * (boundsHeight / DesignHeight)
                : 0f;

        internal static void ResolveBand(
            float boundsHeight,
            out float bandBottomY,
            out float bandHeight)
        {
            bandBottomY = ResolveBottomInset(boundsHeight);
            bandHeight = ResolveBandHeight(boundsHeight);
        }

        internal static void ApplyShadeRect(RectTransform shadeRect, float boundsWidth, float boundsHeight)
        {
            ResolveBand(boundsHeight, out float bandBottomY, out float bandHeight);
            float horizontalInset = ResolveHorizontalInset(boundsWidth);

            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.zero;
            shadeRect.pivot = Vector2.zero;
            shadeRect.anchoredPosition = new Vector2(horizontalInset, bandBottomY);
            shadeRect.sizeDelta = new Vector2(
                Mathf.Max(boundsWidth - (2f * horizontalInset), 0f),
                bandHeight);
            shadeRect.localScale = Vector3.one;
        }
    }
}
