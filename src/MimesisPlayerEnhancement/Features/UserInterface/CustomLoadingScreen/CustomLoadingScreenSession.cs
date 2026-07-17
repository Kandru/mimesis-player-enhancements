namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal static class CustomLoadingScreenSession
    {
        internal static bool IsActive { get; private set; }
        internal static CustomLoadingScreenContext Context { get; private set; }
        internal static string Theme { get; private set; } = "";
        internal static CustomLoadingScreenPhase Phase { get; private set; }

        /// <summary>Last context seen on any scene transition, kept across sessions to
        /// distinguish dungeon→tram from maintenance→tram.</summary>
        internal static CustomLoadingScreenContext? LastTransitionContext { get; private set; }

        /// <summary>While true, ignore vanilla <c>Hide</c>/<c>EndSceneLoading</c>. The exiting
        /// cutscene calls <c>EndSceneLoading</c> ~0.1s in, which would otherwise tear down the
        /// custom screen before <c>Hub.LoadScene</c>.</summary>
        internal static bool HoldThroughDeparture { get; private set; }

        /// <summary>True while the overlay is fading out into the loaded scene.</summary>
        internal static bool IsDismissing { get; private set; }

        internal static void TrackTransition(CustomLoadingScreenContext context)
        {
            LastTransitionContext = context;
        }

        internal static void Begin(CustomLoadingScreenContext context, string theme, bool holdThroughDeparture = false)
        {
            IsActive = true;
            IsDismissing = false;
            Context = context;
            Theme = theme;
            Phase = CustomLoadingScreenPhase.Loading;
            if (holdThroughDeparture)
            {
                HoldThroughDeparture = true;
            }
        }

        internal static void BeginDismiss()
        {
            IsDismissing = true;
            HoldThroughDeparture = false;
        }

        internal static void RequestDepartureHold()
        {
            if (IsActive)
            {
                HoldThroughDeparture = true;
            }
        }

        internal static void ReleaseDepartureHold()
        {
            HoldThroughDeparture = false;
        }

        internal static void SetPhase(CustomLoadingScreenPhase phase)
        {
            if (IsActive)
            {
                Phase = phase;
            }
        }

        internal static void Clear()
        {
            IsActive = false;
            IsDismissing = false;
            Theme = "";
            Phase = CustomLoadingScreenPhase.Loading;
            HoldThroughDeparture = false;
        }
    }
}
