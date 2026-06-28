using System.Collections.Concurrent;
using System.Threading;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardConfigUpdateQueue
    {
        private const int WaitTimeoutMs = 5000;

        private static readonly ConcurrentQueue<PendingUpdate> Pending = new();

        internal static WebDashboardConfigUpdateResult EnqueueAndWait(string sectionId, string key, string value)
        {
            PendingUpdate pending = new()
            {
                SectionId = sectionId,
                Key = key,
                Value = value,
                Done = new ManualResetEventSlim(false),
            };

            Pending.Enqueue(pending);

            return !pending.Done.Wait(WaitTimeoutMs)
                ? new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = "Timed out waiting for the game thread.",
                }
                : pending.Result ?? new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = "Setting update did not complete.",
                };
        }

        internal static void Process()
        {
            while (Pending.TryDequeue(out PendingUpdate? pending))
            {
                try
                {
                    pending.Result = WebDashboardConfigBridge.ApplyUpdate(
                        pending.SectionId,
                        pending.Key,
                        pending.Value);
                }
                catch (System.Exception ex)
                {
                    pending.Result = new WebDashboardConfigUpdateResult
                    {
                        Success = false,
                        Message = ex.Message,
                    };
                }
                finally
                {
                    pending.Done.Set();
                }
            }
        }

        private sealed class PendingUpdate
        {
            internal string SectionId = "";
            internal string Key = "";
            internal string Value = "";
            internal ManualResetEventSlim Done = null!;
            internal WebDashboardConfigUpdateResult? Result;
        }
    }
}
