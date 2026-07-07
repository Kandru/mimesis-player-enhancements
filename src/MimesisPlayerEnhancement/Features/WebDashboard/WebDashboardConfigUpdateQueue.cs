using System.Collections.Concurrent;
using System.Threading;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardConfigUpdateQueue
    {
        private const int WaitTimeoutMs = 5000;

        private static string L(string key) => WebDashboardL10n.Get($"api.{key}");

        private static readonly ConcurrentQueue<PendingWork> Pending = new();

        internal static bool IsProcessing { get; private set; }

        internal static T EnqueueAndWait<T>(Func<T> work)
        {
            string locale = WebDashboardRequestLocale.Current;
            PendingWork pending = new()
            {
                Work = () => WebDashboardRequestLocale.RunWithLocale(locale, work),
                Done = new ManualResetEventSlim(false),
            };

            Pending.Enqueue(pending);

            if (!pending.Done.Wait(WaitTimeoutMs))
            {
                throw new TimeoutException(L("timed_out"));
            }

            if (pending.Error != null)
            {
                throw pending.Error;
            }

            return pending.Result is T typed ? typed : default!;
        }

        internal static WebDashboardConfigUpdateResult EnqueueAndWait(
            WebDashboardConfigScope scope,
            int saveSlotId,
            string sectionId,
            string key,
            string value)
        {
            try
            {
                return EnqueueAndWait(() => scope switch
                {
                    WebDashboardConfigScope.Global => WebDashboardConfigBridge.ApplyGlobalUpdate(
                        sectionId,
                        key,
                        value),
                    WebDashboardConfigScope.Save => WebDashboardConfigBridge.ApplySaveUpdate(
                        saveSlotId,
                        sectionId,
                        key,
                        value),
                    _ => new WebDashboardConfigUpdateResult
                    {
                        Success = false,
                        Message = L("unknown_scope"),
                    },
                });
            }
            catch (TimeoutException ex)
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = ex.Message,
                };
            }
            catch (Exception ex)
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = ex.Message,
                };
            }
        }

        internal static void Process()
        {
            if (IsProcessing)
            {
                return;
            }

            while (Pending.TryDequeue(out PendingWork? pending))
            {
                IsProcessing = true;
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
                    IsProcessing = false;
                    pending.Done.Set();
                }
            }
        }

        private sealed class PendingWork
        {
            internal Func<object?> Work = null!;
            internal ManualResetEventSlim Done = null!;
            internal object? Result;
            internal Exception? Error;
        }
    }
}
