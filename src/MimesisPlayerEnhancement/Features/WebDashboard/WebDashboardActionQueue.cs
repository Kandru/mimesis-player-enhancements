using System.Collections.Concurrent;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardActionQueue
    {
        private static readonly ConcurrentQueue<WebDashboardPendingAction> Pending = new();

        internal static void Enqueue(WebDashboardPendingAction action)
        {
            Pending.Enqueue(action);
        }

        internal static void Process()
        {
            while (Pending.TryDequeue(out WebDashboardPendingAction? action))
            {
                _ = WebDashboardModerationService.Execute(action);
            }
        }
    }
}
