using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal struct CustomLoadingScreenMotionSettings
    {
        internal CustomLoadingScreenMotionMode Mode;
        internal float Zoom;
        internal float CycleSeconds;

        // Without a theme.json the image stays perfectly still; pan/zoom is opt-in via manifest.
        internal static CustomLoadingScreenMotionSettings Default => new()
        {
            Mode = CustomLoadingScreenMotionMode.None,
            Zoom = CustomLoadingScreenConstants.DefaultMotionZoom,
            CycleSeconds = CustomLoadingScreenConstants.DefaultMotionCycleSeconds,
        };

        internal bool IsPanZoomEnabled(bool globalMotionEnabled) =>
            globalMotionEnabled && Mode == CustomLoadingScreenMotionMode.PanZoom;
    }

    internal sealed class CustomLoadingScreenResolvedPhase
    {
        internal IReadOnlyList<string> ImagePaths = [];
        internal float FrameRate = CustomLoadingScreenConstants.DefaultFrameRate;
        internal CustomLoadingScreenLoopMode Loop = CustomLoadingScreenLoopMode.Loop;
        internal CustomLoadingScreenMotionSettings Motion = CustomLoadingScreenMotionSettings.Default;
        internal Color BackgroundColor = Color.black;
    }
}
