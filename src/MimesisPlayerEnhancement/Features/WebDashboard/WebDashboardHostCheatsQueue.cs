using System.Collections.Concurrent;
using System.Threading;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardHostCheatsQueue
    {
        private const int WaitTimeoutMs = 5000;

        private static string L(string key) => WebDashboardL10n.Get($"api.{key}");

        private static readonly ConcurrentQueue<PendingWork> Pending = new();

        internal static WebDashboardHostCheatsDto EnqueueAndWait(Func<WebDashboardHostCheatsDto> work)
        {
            if (WebDashboardServer.IsShuttingDown)
            {
                return new WebDashboardHostCheatsDto
                {
                    Success = false,
                    Message = L("timed_out"),
                };
            }

            string locale = WebDashboardRequestLocale.Current;
            PendingWork pending = new()
            {
                Work = () => WebDashboardRequestLocale.RunWithLocale(locale, work),
                Done = new ManualResetEventSlim(false),
            };

            Pending.Enqueue(pending);

            if (!pending.Done.Wait(WaitTimeoutMs))
            {
                return new WebDashboardHostCheatsDto
                {
                    Success = false,
                    Message = L("timed_out"),
                };
            }

            if (pending.Error != null)
            {
                return new WebDashboardHostCheatsDto
                {
                    Success = false,
                    Message = pending.Error.Message,
                };
            }

            return pending.Result ?? WebDashboardHostCheatsService.BuildState();
        }

        internal static void Process()
        {
            while (Pending.TryDequeue(out PendingWork? pending))
            {
                try
                {
                    pending.Result = pending.Work();
                }
                catch (Exception ex)
                {
                    pending.Error = ex;
                }
                finally
                {
                    pending.Done.Set();
                }
            }
        }

        internal static void CancelPending()
        {
            while (Pending.TryDequeue(out PendingWork? pending))
            {
                pending.Error = new OperationCanceledException(L("timed_out"));
                pending.Done.Set();
            }
        }

        private sealed class PendingWork
        {
            internal Func<WebDashboardHostCheatsDto> Work = null!;
            internal ManualResetEventSlim Done = null!;
            internal WebDashboardHostCheatsDto? Result;
            internal Exception? Error;
        }
    }
}
