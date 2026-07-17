namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal static class CustomLoadingScreenConstants
    {
        internal const string Feature = "Ui";
        internal const string AssetFolder = "CustomLoadingScreen";
        internal const string OverlayObjectName = "MPE_CustomLoadingOverlay";

        internal const string LoadingTextKey = "STRING_LOADING";
        internal const string WaitTextKey = "STRING_LOADING_WAIT";

        internal const string LoadingImageFile = "loading.png";
        internal const string WaitImageFile = "wait.png";
        internal const string BackgroundImageFile = "background.png";

        internal static readonly string[] KnownContextKeys =
        [
            "FirstEnter",
            "Maintenance",
            "InTramWaiting",
            "DeathMatch",
            "Default",
        ];
    }
}
