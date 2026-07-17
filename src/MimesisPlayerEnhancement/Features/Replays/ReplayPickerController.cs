namespace MimesisPlayerEnhancement.Features.Replays
{
    internal static class ReplayPickerController
    {
        private const string Feature = "Replays";

        private static UIPrefab_MainMenu? _mainMenuUi;
        private static ReplayPickerPanel? _panel;

        internal static bool IsPickerOpen { get; private set; }

        internal static ReplayPickerPanel? Panel => _panel;

        internal static UIPrefab_MainMenu? MainMenuUi => _mainMenuUi;

        internal static void OnMainMenuStarted(MainMenu mainMenu, UIPrefab_MainMenu mainMenuUi)
        {
            _ = mainMenu;
            _mainMenuUi = mainMenuUi;
            ResetMenuSession();
            ReplayMenuButton.SyncVisibility();
        }

        internal static void OnMainMenuShown(UIPrefab_MainMenu mainMenuUi)
        {
            _mainMenuUi = mainMenuUi;
            ResetMenuSession();
            ReplayMenuButton.SyncVisibility();
        }

        internal static void TryOpenPicker()
        {
            if (!ReplaysRuntime.IsEnabled)
            {
                ModLog.Warn(Feature, "Replays feature is disabled.");
                return;
            }

            if (_mainMenuUi == null)
            {
                ModLog.Warn(Feature, "Main menu unavailable — cannot open replay picker.");
                return;
            }

            if (_panel == null)
            {
                _panel = new ReplayPickerPanel(_mainMenuUi);
            }

            if (_panel.TryOpen())
            {
                IsPickerOpen = true;
            }
        }

        internal static void CloseIfOpen()
        {
            _panel?.Close();
            IsPickerOpen = false;
        }

        internal static void SetPickerOpen(bool open) => IsPickerOpen = open;

        private static void ResetMenuSession()
        {
            IsPickerOpen = false;

            if (_mainMenuUi != null)
            {
                ReplayPickerChrome.ForceRestoreMainMenuDimming(_mainMenuUi);
            }

            if (_panel == null)
            {
                return;
            }

            _panel.Dispose();
            _panel = null;
        }
    }
}
