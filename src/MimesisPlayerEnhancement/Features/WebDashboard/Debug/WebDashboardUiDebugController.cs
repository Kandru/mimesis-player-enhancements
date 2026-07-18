using MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList;

namespace MimesisPlayerEnhancement.Features.WebDashboard.Debug
{
    internal static class WebDashboardUiDebugController
    {
        internal const string OverlaySpectator = "spectator";
        internal const string OverlayLoadingWait = "loadingWait";
        internal const string OverlayEscMenu = "escMenu";
        internal const string OverlaySurvivalResult = "survivalResult";

        private static bool _spectatorActive;
        private static bool _loadingWaitActive;
        private static bool _escMenuActive;
        private static bool _survivalResultActive;

        internal static WebDashboardUiDebugStatusDto GetStatus()
        {
            SyncFromAvailability();

            return new WebDashboardUiDebugStatusDto
            {
                Ingame = WebDashboardGameState.IsIngame(),
                Alive = WebDashboardGameState.IsLocalPlayerAlive(),
                MaxPlayers = MorePlayersPatchHelpers.GetMaxPlayers(),
                Spectator = _spectatorActive,
                LoadingWait = _loadingWaitActive,
                EscMenu = _escMenuActive,
                SurvivalResult = _survivalResultActive,
            };
        }

        internal static void SyncFromAvailability()
        {
            if (WebDashboardGameState.CanUseUiDebugOverlays())
            {
                return;
            }

            if (!_spectatorActive
                && !_loadingWaitActive
                && !_escMenuActive
                && !_survivalResultActive)
            {
                return;
            }

            OnSessionEnded();
        }

        internal static WebDashboardUiDebugToggleResult Toggle(string id)
        {
            SyncFromAvailability();

            if (!WebDashboardGameState.IsIngame())
            {
                return Fail(id, "debug_not_ingame");
            }

            if (!IsOverlayActive(id) && !WebDashboardGameState.IsLocalPlayerAlive())
            {
                return Fail(id, "debug_not_alive");
            }

            List<string> fakeNames = BuildFakePlayerNames(MorePlayersPatchHelpers.GetMaxPlayers());

            return id switch
            {
                OverlaySpectator => ToggleOverlay(
                    id,
                    ref _spectatorActive,
                    () => SpectatorPlayerGrid.DebugShow(fakeNames),
                    SpectatorPlayerGrid.DebugHide),
                OverlayLoadingWait => ToggleOverlay(
                    id,
                    ref _loadingWaitActive,
                    () => LoadingWaitPlayerListRuntime.DebugShow(fakeNames),
                    LoadingWaitPlayerListRuntime.DebugHide),
                OverlayEscMenu => ToggleOverlay(
                    id,
                    ref _escMenuActive,
                    () => InGameMenuDebugPreview.Show(fakeNames),
                    InGameMenuDebugPreview.Hide),
                OverlaySurvivalResult => ToggleOverlay(
                    id,
                    ref _survivalResultActive,
                    () => SurvivalResultDebugPreview.Show(fakeNames),
                    SurvivalResultDebugPreview.Hide),
                _ => Fail(id, "debug_unknown_overlay"),
            };
        }

        internal static void OnSessionEnded()
        {
            if (_spectatorActive)
            {
                SpectatorPlayerGrid.DebugHide();
            }

            if (_loadingWaitActive)
            {
                LoadingWaitPlayerListRuntime.DebugHide();
            }

            if (_escMenuActive)
            {
                InGameMenuDebugPreview.Hide();
            }

            if (_survivalResultActive)
            {
                SurvivalResultDebugPreview.Hide();
            }

            _spectatorActive = false;
            _loadingWaitActive = false;
            _escMenuActive = false;
            _survivalResultActive = false;
        }

        private static bool IsOverlayActive(string id) => id switch
        {
            OverlaySpectator => _spectatorActive,
            OverlayLoadingWait => _loadingWaitActive,
            OverlayEscMenu => _escMenuActive,
            OverlaySurvivalResult => _survivalResultActive,
            _ => false,
        };

        private static List<string> BuildFakePlayerNames(int count)
        {
            List<string> names = new(count);
            for (int i = 1; i <= count; i++)
            {
                names.Add($"Player {i:00}");
            }

            return names;
        }

        private static WebDashboardUiDebugToggleResult ToggleOverlay(
            string id,
            ref bool activeFlag,
            Func<bool> show,
            Action hide)
        {
            if (!activeFlag)
            {
                if (!show())
                {
                    return Fail(id, "debug_overlay_unavailable");
                }

                activeFlag = true;
            }
            else
            {
                hide();
                activeFlag = false;
            }

            return new WebDashboardUiDebugToggleResult
            {
                Success = true,
                Id = id,
                Active = activeFlag,
            };
        }

        private static WebDashboardUiDebugToggleResult Fail(string id, string reasonKey)
        {
            return new WebDashboardUiDebugToggleResult
            {
                Success = false,
                Id = id,
                Message = WebDashboardL10n.Get($"api.{reasonKey}"),
            };
        }
    }
}
