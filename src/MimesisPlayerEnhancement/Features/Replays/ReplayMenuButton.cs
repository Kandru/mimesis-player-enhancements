namespace MimesisPlayerEnhancement.Features.Replays
{
    internal static class ReplayMenuButton
    {
        private const string Feature = "Replays";
        private const string ButtonId = "Replays";

        internal static void SyncVisibility()
        {
            if (ReplaysRuntime.IsEnabled)
            {
                Register();
            }
            else
            {
                Unregister();
            }
        }

        private static void Register()
        {
            MenuMirrorRegistry.SetCustomization(
                MenuKind.MainMenu,
                Feature,
                new MenuCustomization().AddCustom(new CustomMenuButton(ButtonId, "Replays", OpenReplayPicker)
                {
                    AfterButtonId = UIPrefab_MainMenu.UEID_JoinButton,
                }));
        }

        private static void Unregister()
        {
            MenuMirrorRegistry.ClearCustomization(MenuKind.MainMenu, Feature);
        }

        private static void OpenReplayPicker()
        {
            ReplayPickerController.TryOpenPicker();
        }
    }
}
