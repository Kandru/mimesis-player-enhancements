using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    /// <summary>Shared normalization helpers for minimap layout math.</summary>
    internal static class WebDashboardMinimapMath
    {
        internal const float BoundsPadding = 0.05f;

        internal static float Normalize(float value, float min, float span)
        {
            return span <= 0f ? 0.5f : Mathf.Clamp01((value - min) / span);
        }
    }
}
