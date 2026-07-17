namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal static class CustomLoadingScreenSession
    {
        internal static bool IsActive { get; private set; }
        internal static CustomLoadingScreenContext Context { get; private set; }
        internal static string Theme { get; private set; } = "";
        internal static CustomLoadingScreenPhase Phase { get; private set; }

        internal static void Begin(CustomLoadingScreenContext context, string theme)
        {
            IsActive = true;
            Context = context;
            Theme = theme;
            Phase = CustomLoadingScreenPhase.Loading;
        }

        internal static void SetPhase(CustomLoadingScreenPhase phase)
        {
            if (!IsActive)
            {
                return;
            }

            Phase = phase;
        }

        internal static void Clear()
        {
            IsActive = false;
            Theme = "";
            Phase = CustomLoadingScreenPhase.Loading;
        }
    }
}
