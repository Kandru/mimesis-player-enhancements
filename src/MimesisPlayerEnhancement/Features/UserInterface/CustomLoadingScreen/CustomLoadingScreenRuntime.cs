namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal static class CustomLoadingScreenRuntime
    {
        internal static void RefreshFromConfig()
        {
            CustomLoadingScreenResolver.InvalidateCatalog();
            CustomLoadingScreenTextureCache.Clear();
        }
    }
}
