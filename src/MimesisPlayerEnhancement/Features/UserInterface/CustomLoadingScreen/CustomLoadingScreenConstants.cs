namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal static class CustomLoadingScreenConstants
    {
        internal const string Feature = "Ui";
        internal const string AssetFolder = "CustomLoadingScreen";
        internal const string OverlayObjectName = "MPE_CustomLoadingOverlay";
        internal const string OverlayBackgroundObjectName = "MPE_CustomLoadingBackground";
        internal const string OverlayImageObjectName = "MPE_CustomLoadingImage";
        internal const string OverlayCrossfadeObjectName = "MPE_CustomLoadingCrossfade";

        internal const string WaitTextKey = "STRING_LOADING_WAIT";

        internal const string LoadingImageFile = "loading.png";
        internal const string WaitImageFile = "wait.png";
        internal const string BackgroundImageFile = "background.png";
        internal const string ThemeManifestFile = "theme.json";

        internal const float DefaultFrameRate = 4f;
        internal const float MinFrameRate = 1f;
        internal const float MaxFrameRate = 30f;
        internal const float DefaultMotionZoom = 1.08f;
        internal const float DefaultMotionCycleSeconds = 20f;
        internal const float DefaultDepartureFadeSeconds = 1f;
        internal const float DefaultArrivalFadeSeconds = 1f;
        internal const float DefaultPhaseCrossfadeSeconds = 0.75f;

        /// <summary>Nested overlay canvas sort order — above the game's fade/video layers.</summary>
        internal const int OverlayCanvasSortOrder = 32000;
    }
}
