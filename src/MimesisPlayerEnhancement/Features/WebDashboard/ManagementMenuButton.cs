using MimesisPlayerEnhancement.Ui.MenuMirror;
using Steamworks;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    /// <summary>
    /// Yellow "Management" button on the main menu and ESC menu that opens the web
    /// dashboard in the Steam overlay browser (system browser as fallback).
    /// Shown exactly while the dashboard server is running.
    /// </summary>
    internal static class ManagementMenuButton
    {
        private const string Feature = "WebDashboard";
        private const string ButtonId = "Management";

        private static readonly Color ManagementYellow = new(1f, 0.8f, 0.1f, 1f);

        private static bool _registered;

        internal static void SyncVisibility(bool dashboardRunning, string listenUrl)
        {
            if (dashboardRunning)
            {
                Register(listenUrl);
            }
            else
            {
                Unregister();
            }
        }

        private static void Register(string listenUrl)
        {
            string url = BuildBrowserUrl(listenUrl);

            foreach (MenuKind kind in new[] { MenuKind.MainMenu, MenuKind.InGameMenu })
            {
                string settingsButtonId = kind == MenuKind.MainMenu
                    ? UIPrefab_MainMenu.UEID_SettingButton
                    : UIPrefab_InGameMenu.UEID_SettingButton;

                MenuMirrorRegistry.SetCustomization(
                    kind,
                    Feature,
                    new MenuCustomization().AddCustom(new CustomMenuButton(ButtonId, "Management", () => OpenDashboard(url))
                    {
                        LabelColor = ManagementYellow,
                        AfterButtonId = settingsButtonId,
                    }));
            }

            _registered = true;
        }

        private static void Unregister()
        {
            if (!_registered)
            {
                return;
            }

            MenuMirrorRegistry.ClearCustomization(MenuKind.MainMenu, Feature);
            MenuMirrorRegistry.ClearCustomization(MenuKind.InGameMenu, Feature);
            _registered = false;
        }

        private static void OpenDashboard(string url)
        {
            try
            {
                if (SteamUtils.IsOverlayEnabled())
                {
                    SteamFriends.ActivateGameOverlayToWebPage(url);
                    ModLog.Info(Feature, $"Opened dashboard in Steam overlay — {url}");
                    return;
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Steam overlay open failed — {ex.Message}");
            }

            try
            {
                Application.OpenURL(url);
                ModLog.Info(Feature, $"Opened dashboard in system browser — {url}");
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Failed to open dashboard URL — {ex.Message}");
            }
        }

        private static string BuildBrowserUrl(string listenUrl)
        {
            // A wildcard/any bind address is not reachable as a URL host; use loopback.
            return listenUrl
                .Replace("://0.0.0.0", "://127.0.0.1")
                .Replace("://*", "://127.0.0.1")
                .Replace("://+", "://127.0.0.1");
        }
    }
}
