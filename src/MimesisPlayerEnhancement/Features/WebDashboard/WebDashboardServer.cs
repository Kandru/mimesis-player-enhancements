using System.Net;
using System.Threading;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardServer
    {
        private const string Feature = "WebDashboard";
        private const int PortFallbackCount = 20;

        private static HttpListener? _listener;
        private static Thread? _listenerThread;
        private static volatile bool _running;
        private static volatile bool _shuttingDown;
        private static bool _syncDeferred;
        private static string _listenUrl = "";

        internal static bool IsRunning => _running;
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
            string address = ModConfig.WebDashboardListenAddress.Value?.Trim() ?? "127.0.0.1";
            int configuredPort = ModConfig.WebDashboardListenPort.Value;
            if (configuredPort is < 1 or > 65535)
            {
                ModLog.Warn(Feature, $"Invalid port {configuredPort}; web dashboard not started.");
                Stop();
                return;
            }

            if (!IsLoopback(address))
            {
                ModLog.Warn(Feature, $"Binding to {address}:{configuredPort} exposes the dashboard on the network. Use 127.0.0.1 unless you trust your LAN.");
            }

            string configuredPrefix = BuildPrefix(address, configuredPort);
            if (string.Equals(configuredPrefix, _listenUrl, StringComparison.OrdinalIgnoreCase) && _running)
            {
                return;
            }

            Stop();
            TryStartWithPortFallback(address, configuredPort);
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
            WebDashboardUiDebugController.SyncFromAvailability();
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

        private static void TryStartWithPortFallback(string address, int configuredPort)
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

            int maxPort = Math.Min(configuredPort + PortFallbackCount, 65535);
            for (int port = configuredPort; port <= maxPort; port++)
            {
                string prefix = BuildPrefix(address, port);
                if (TryBindPrefix(prefix, out HttpListener listener))
                {
                    StartBoundListener(listener, prefix, configuredPort);
                    return;
                }
            }

            int rangeEnd = Math.Min(configuredPort + PortFallbackCount, 65535);
            ModLog.Error(Feature, $"No free port in range {configuredPort}–{rangeEnd} on {address}; web dashboard not started.");
            Stop();
        }

        private static void StartBoundListener(HttpListener listener, string prefix, int configuredPort)
        {
            _listener = listener;
            _listenUrl = prefix;
            _running = true;

            WebDashboardSseHub.Start();
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

            string listenUrl = _listenUrl.TrimEnd('/');
            if (TryGetPortFromPrefix(prefix, out int boundPort) && boundPort != configuredPort)
            {
                ModLog.Info(Feature, $"Listening at {listenUrl} (configured port {configuredPort} was unavailable)");
            }
            else
            {
                ModLog.Info(Feature, $"Listening at {listenUrl}");
            }
        }

        private static bool TryBindPrefix(string prefix, out HttpListener listener)
        {
            listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            try
            {
                listener.Start();
                return true;
            }
            catch (Exception ex)
            {
                ModLog.Debug(Feature, $"Bind failed at {prefix.TrimEnd('/')} — {ex.Message}");
                try
                {
                    listener.Close();
                }
                catch
                {
                    /* probe failed */
                }

                listener = null!;
                return false;
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

        private static string BuildPrefix(string address, int port)
        {
            return $"http://{address}:{port}/";
        }

        private static bool TryGetPortFromPrefix(string prefix, out int port)
        {
            port = 0;
            if (!Uri.TryCreate(prefix, UriKind.Absolute, out Uri? uri))
            {
                return false;
            }

            port = uri.Port;
            return true;
        }

        private static bool IsLoopback(string address)
        {
            return string.Equals(address, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(address, "localhost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(address, "::1", StringComparison.OrdinalIgnoreCase);
        }
    }
}
