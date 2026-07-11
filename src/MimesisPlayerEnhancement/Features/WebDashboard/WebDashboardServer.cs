using System.Net;
using System.Threading;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardServer
    {
        private const string Feature = "WebDashboard";

        private static HttpListener? _listener;
        private static Thread? _listenerThread;
        private static volatile bool _running;
        private static volatile bool _shuttingDown;
        private static bool _syncDeferred;
        private static string _listenUrl = "";

        internal static bool IsShuttingDown => _shuttingDown;

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            ModConfig.Changed += OnConfigChanged;
            WebDashboardPatches.Apply(harmony);
        }

        private static void OnConfigChanged(ModConfigChangeInfo change)
        {
            WebDashboardSettingsCache.Invalidate();
            WebDashboardSnapshotCache.MarkDirty();
        }

        internal static void SyncFromConfig()
        {
            if (_shuttingDown || !ModConfig.IsInitialized)
            {
                return;
            }

            if (WebDashboardConfigUpdateQueue.IsProcessing)
            {
                _syncDeferred = true;
                return;
            }

            ApplySyncFromConfig();
        }

        private static void ApplySyncFromConfig()
        {
            if (!ModConfig.EnableWebDashboard.Value)
            {
                Stop();
                return;
            }

            string address = ModConfig.WebDashboardListenAddress.Value?.Trim() ?? "127.0.0.1";
            int port = ModConfig.WebDashboardListenPort.Value;
            if (port is < 1 or > 65535)
            {
                ModLog.Warn(Feature, $"Invalid port {port}; web dashboard not started.");
                Stop();
                return;
            }

            if (!IsLoopback(address))
            {
                ModLog.Warn(Feature, $"Binding to {address}:{port} exposes the dashboard on the network. Use 127.0.0.1 unless you trust your LAN.");
            }

            string prefix = $"http://{address}:{port}/";
            if (string.Equals(prefix, _listenUrl, StringComparison.OrdinalIgnoreCase) && _running)
            {
                return;
            }

            Stop();
            Start(prefix);
        }

        internal static void OnUpdate()
        {
            // Flush before and after queue processing: a deferred sync may already be
            // pending, and processing the queue can defer another one.
            FlushDeferredSync();
            WebDashboardConfigUpdateQueue.Process();
            WebDashboardItemSpawnQueue.Process();
            WebDashboardHostCheatsQueue.Process();
            FlushDeferredSync();

            if (!_running)
            {
                return;
            }

            WebDashboardActionQueue.Process();
            WebDashboardSnapshotCache.Tick(_listenUrl);
        }

        private static void FlushDeferredSync()
        {
            if (_syncDeferred && !WebDashboardConfigUpdateQueue.IsProcessing)
            {
                _syncDeferred = false;
                ApplySyncFromConfig();
            }
        }

        /// <summary>
        /// Tear down the HTTP server as soon as the game begins quitting, while the main
        /// thread can still drain dashboard queues in <see cref="OnUpdate"/>.
        /// </summary>
        internal static void PrepareApplicationQuit()
        {
            if (_shuttingDown)
            {
                return;
            }

            _shuttingDown = true;
            ModLog.Debug(Feature, "Application quit — shutting down dashboard.");
            WebDashboardHostCheatsRuntime.OnDeinitialize();
            Stop();
        }

        internal static void StopOnDeinit()
        {
            PrepareApplicationQuit();
            ModConfig.Changed -= OnConfigChanged;
        }

        private static void CancelAllPendingWork()
        {
            WebDashboardConfigUpdateQueue.CancelPending();
            WebDashboardItemSpawnQueue.CancelPending();
            WebDashboardHostCheatsQueue.CancelPending();
        }

        private static void Start(string prefix)
        {
            if (_shuttingDown)
            {
                return;
            }

            if (!WebDashboardEmbeddedAssets.IsAvailable)
            {
                ModLog.Error(Feature, "Web dashboard assets are missing from the mod assembly.");
                return;
            }

            WebDashboardSseHub.Start();

            try
            {
                HttpListener listener = new();
                listener.Prefixes.Add(prefix);
                listener.Start();
                _listener = listener;
                _listenUrl = prefix;
                _running = true;
                WebDashboardSnapshotCache.MarkDirty();
                WebDashboardSnapshotCache.Refresh(_listenUrl);

                _listenerThread = new Thread(ListenLoop)
                {
                    IsBackground = true,
                    Name = "MimesisWebDashboard",
                };
                _listenerThread.Start();

                ThreadPool.QueueUserWorkItem(_ => WebDashboardSettingsCache.WarmGlobal());

                ManagementMenuButton.SyncVisibility(dashboardRunning: true, _listenUrl);

                ModLog.Info(Feature, $"Listening at {_listenUrl.TrimEnd('/')}");
            }
            catch (Exception ex)
            {
                ModLog.Error(Feature, $"Failed to start HTTP listener at {prefix}: {ex.Message}");
                Stop();
            }
        }

        private static void Stop()
        {
            _running = false;
            ManagementMenuButton.SyncVisibility(dashboardRunning: false, "");
            CancelAllPendingWork();
            WebDashboardSseHub.Shutdown();
            HttpListener? listener = _listener;
            _listener = null;
            _listenUrl = "";

            if (listener != null)
            {
                try
                {
                    // Close abandons active SSE/API handlers immediately. Stop() can block
                    // on Mono while handlers wait for the game thread during quit.
                    listener.Close();
                }
                catch
                {
                    /* shutting down */
                }
            }

            _listenerThread = null;
        }

        private static void ListenLoop()
        {
            while (_running && _listener != null)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    _ = ThreadPool.QueueUserWorkItem(_ => WebDashboardRouter.Handle(context));
                }
                catch (HttpListenerException) when (!_running)
                {
                    break;
                }
                catch (ObjectDisposedException) when (!_running)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_running)
                    {
                        ModLog.Warn(Feature, $"Accept loop error: {ex.Message}");
                    }
                }
            }
        }

        private static bool IsLoopback(string address)
        {
            return string.Equals(address, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(address, "localhost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(address, "::1", StringComparison.OrdinalIgnoreCase);
        }
    }
}
