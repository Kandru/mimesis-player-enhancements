using System.Collections.Concurrent;
using System.Threading;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardItemSpawnQueue
    {
        private const int WaitTimeoutMs = 5000;

        private static string L(string key) => WebDashboardL10n.Get($"api.{key}");

        private static readonly ConcurrentQueue<PendingSpawn> Pending = new();

        internal static bool IsProcessing { get; private set; }

        internal static WebDashboardSpawnItemResult EnqueueAndWait(
            ulong steamId,
            long playerUid,
            string itemId,
            int? percent)
        {
            if (WebDashboardServer.IsShuttingDown)
            {
                return new WebDashboardSpawnItemResult
                {
                    Success = false,
                    Message = L("timed_out"),
                };
            }

            string locale = WebDashboardRequestLocale.Current;
            PendingSpawn pending = new()
            {
                SteamId = steamId,
                PlayerUid = playerUid,
                ItemId = itemId,
                Percent = percent,
                Locale = locale,
                Done = new ManualResetEventSlim(false),
            };

            Pending.Enqueue(pending);

            return !pending.Done.Wait(WaitTimeoutMs)
                ? new WebDashboardSpawnItemResult
                {
                    Success = false,
                    Message = L("timed_out"),
                }
                : pending.Result ?? new WebDashboardSpawnItemResult
                {
                    Success = false,
                    Message = L("item_spawn_failed"),
                };
        }

        internal static void Process()
        {
            if (IsProcessing)
            {
                return;
            }

            while (Pending.TryDequeue(out PendingSpawn? pending))
            {
                IsProcessing = true;
                try
                {
                    pending.Result = WebDashboardRequestLocale.RunWithLocale(
                        pending.Locale,
                        () => WebDashboardItemSpawnService.Execute(
                            pending.SteamId,
                            pending.PlayerUid,
                            pending.ItemId,
                            pending.Percent));
                }
                catch (Exception ex)
                {
                    pending.Result = new WebDashboardSpawnItemResult
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

        internal static void CancelPending()
        {
            IsProcessing = false;
            while (Pending.TryDequeue(out PendingSpawn? pending))
            {
                pending.Result = new WebDashboardSpawnItemResult
                {
                    Success = false,
                    Message = L("timed_out"),
                };
                pending.Done.Set();
            }
        }

        private sealed class PendingSpawn
        {
            internal ulong SteamId;
            internal long PlayerUid;
            internal string ItemId = "";
            internal int? Percent;
            internal string Locale = "en";
            internal ManualResetEventSlim Done = null!;
            internal WebDashboardSpawnItemResult? Result;
        }
    }
}
