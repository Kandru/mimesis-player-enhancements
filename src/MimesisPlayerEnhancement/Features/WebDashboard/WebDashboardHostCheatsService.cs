using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardHostCheatsService
    {
        internal static WebDashboardHostCheatsDto BuildState()
        {
            return new WebDashboardHostCheatsDto
            {
                GodMode = WebDashboardHostCheatsRuntime.GodModeEnabled,
                NoClip = WebDashboardHostCheatsRuntime.NoClipEnabled,
                Available = WebDashboardHostCheatsRuntime.IsAvailable,
            };
        }

        internal static WebDashboardHostCheatsDto Apply(WebDashboardHostCheatsUpdateRequest? request)
        {
            if (request == null)
            {
                return Fail(WebDashboardL10n.Get("api.invalid_host_cheats_request"));
            }

            if (request.GodMode.HasValue
                && !WebDashboardHostCheatsRuntime.TrySetGodMode(request.GodMode.Value, out string? godModeError))
            {
                return Fail(godModeError ?? WebDashboardL10n.Get("api.failed_apply"));
            }

            if (request.NoClip.HasValue
                && !WebDashboardHostCheatsRuntime.TrySetNoClip(request.NoClip.Value, out string? noClipError))
            {
                return Fail(noClipError ?? WebDashboardL10n.Get("api.failed_apply"));
            }

            return BuildState();
        }

        internal static WebDashboardHostCheatsDto DisableAll()
        {
            WebDashboardHostCheatsRuntime.DisableAll("web request");
            return BuildState();
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
