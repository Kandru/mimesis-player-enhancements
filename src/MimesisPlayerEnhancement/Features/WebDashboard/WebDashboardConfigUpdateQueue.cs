using System.Collections.Concurrent;
using System.Threading;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardConfigUpdateQueue
    {
        private const int WaitTimeoutMs = 5000;

        private static string L(string key) => ModL10n.Get($"api.{key}");

        private static readonly ConcurrentQueue<PendingUpdate> Pending = new();

        internal static bool IsProcessing { get; private set; }

        internal static WebDashboardConfigUpdateResult EnqueueAndWait(
            WebDashboardConfigScope scope,
            int saveSlotId,
            string sectionId,
            string key,
            string value)
        {
            PendingUpdate pending = new()
            {
                Scope = scope,
                SaveSlotId = saveSlotId,
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
                    Message = L("timed_out"),
                }
                : pending.Result ?? new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = L("setting_not_complete"),
                };
        }

        internal static void Process()
        {
            if (IsProcessing)
            {
                return;
            }

            while (Pending.TryDequeue(out PendingUpdate? pending))
            {
                IsProcessing = true;
                try
                {
                    pending.Result = pending.Scope switch
                    {
                        WebDashboardConfigScope.Global => WebDashboardConfigBridge.ApplyGlobalUpdate(
                            pending.SectionId,
                            pending.Key,
                            pending.Value),
                        WebDashboardConfigScope.Save => WebDashboardConfigBridge.ApplySaveUpdate(
                            pending.SaveSlotId,
                            pending.SectionId,
                            pending.Key,
                            pending.Value),
                        _ => new WebDashboardConfigUpdateResult
                        {
                            Success = false,
                            Message = L("unknown_scope"),
                        },
                    };
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
                    IsProcessing = false;
                    pending.Done.Set();
                }
            }
        }

        private sealed class PendingUpdate
        {
            internal WebDashboardConfigScope Scope;
            internal int SaveSlotId;
            internal string SectionId = "";
            internal string Key = "";
            internal string Value = "";
            internal ManualResetEventSlim Done = null!;
            internal WebDashboardConfigUpdateResult? Result;
        }
    }
}
