using System.Collections.Concurrent;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardActionQueue
    {
        private static readonly ConcurrentQueue<WebDashboardPendingAction> Pending = new();
        private static WebDashboardActionResult? _lastResult;

        internal static void Enqueue(WebDashboardPendingAction action)
        {
            Pending.Enqueue(action);
        }

        internal static void Process()
        {
            while (Pending.TryDequeue(out WebDashboardPendingAction? action))
            {
                _lastResult = WebDashboardModerationService.Execute(action);
            }
        }

        internal static WebDashboardActionResult? ConsumeLastResult()
        {
            WebDashboardActionResult? result = _lastResult;
            _lastResult = null;
            return result;
        }
    }
}
