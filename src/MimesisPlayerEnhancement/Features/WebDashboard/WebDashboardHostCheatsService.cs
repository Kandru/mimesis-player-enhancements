using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardHostCheatsService
    {
        private static string L(string key) => WebDashboardL10n.Get($"api.{key}");

        internal static WebDashboardHostCheatsDto BuildState()
        {
            return new WebDashboardHostCheatsDto
            {
                Success = true,
            };
        }

        internal static WebDashboardHostCheatsDto Apply(WebDashboardHostCheatsUpdateRequest? request)
        {
            if (request == null)
            {
                return Fail(WebDashboardL10n.Get("api.invalid_host_cheats_request"));
            }

            if (request.DisableAll == true)
            {
                WebDashboardHostCheatsRuntime.DisableAll("web request");
                return BuildState();
            }

            return Fail(WebDashboardL10n.Get("api.invalid_host_cheats_request"));
        }

        internal static WebDashboardHostCheatsDto TogglePlayerCheat(ulong steamId, long playerUid, bool godMode)
        {
            if (!TryResolveVPlayer(steamId, playerUid, out VPlayer? player, out string? resolveError))
            {
                return Fail(resolveError ?? L("player_not_found"));
            }

            bool enabled;
            string? errorMessage;
            bool success = godMode
                ? WebDashboardHostCheatsRuntime.TryToggleGodMode(player!, out enabled, out errorMessage)
                : WebDashboardHostCheatsRuntime.TryToggleNoClip(player!, out enabled, out errorMessage);

            if (!success)
            {
                return Fail(errorMessage ?? L("failed_apply"));
            }

            string message = godMode
                ? enabled ? L("player_godmode_enabled") : L("player_godmode_disabled")
                : enabled ? L("player_noclip_enabled") : L("player_noclip_disabled");

            return new WebDashboardHostCheatsDto
            {
                Success = true,
                Message = message,
                GodMode = WebDashboardHostCheatsRuntime.IsGodModeEnabled(player!.UID),
                NoClip = WebDashboardHostCheatsRuntime.IsNoClipEnabled(player.UID),
            };
        }

        internal static WebDashboardHostCheatsDto DisableAll()
        {
            WebDashboardHostCheatsRuntime.DisableAll("web request");
            return BuildState();
        }

        private static bool TryResolveVPlayer(ulong steamId, long playerUid, out VPlayer? player, out string? errorMessage)
        {
            player = null;
            errorMessage = null;

            if (playerUid != 0
                && WebDashboardSessionAccess.TryGetPlayerByUid(playerUid, out VPlayer? byUid)
                && byUid != null)
            {
                player = byUid;
                return true;
            }

            if (steamId == 0)
            {
                errorMessage = L("player_not_found");
                return false;
            }

            SessionManager? sessionManager = WebDashboardSessionAccess.GetSessionManager();
            if (sessionManager == null)
            {
                errorMessage = L("session_manager_unavailable");
                return false;
            }

            foreach (SessionContext context in WebDashboardSessionAccess.EnumerateSessionContexts(sessionManager))
            {
                if (context.SteamID != steamId)
                {
                    continue;
                }

                player = WebDashboardSessionAccess.GetVPlayer(context);
                if (player != null)
                {
                    return true;
                }

                errorMessage = L("player_not_in_game");
                return false;
            }

            errorMessage = L("player_not_found");
            return false;
        }

        private static WebDashboardHostCheatsDto Fail(string message)
        {
            WebDashboardHostCheatsDto dto = BuildState();
            dto.Success = false;
            dto.Message = message;
            return dto;
        }
    }
}
