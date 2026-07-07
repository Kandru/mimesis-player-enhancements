using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardHostCheatsService
    {
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
