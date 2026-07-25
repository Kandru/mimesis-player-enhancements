using System.Collections.Concurrent;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardActionQueue
    {
        private const string Feature = "WebDashboard";

        private static readonly ConcurrentQueue<WebDashboardPendingAction> Pending = new();

        internal static void Enqueue(WebDashboardPendingAction action)
        {
            Pending.Enqueue(action);
        }

        internal static void Process()
        {
            while (Pending.TryDequeue(out WebDashboardPendingAction? action))
            {
                WebDashboardActionResult result = WebDashboardModerationService.Execute(action);
                if (!result.Success)
                {
                    ModLog.Warn(Feature, $"Moderation {action.Type} failed — {result.Message}");
                }
            }
        }
    }
}
