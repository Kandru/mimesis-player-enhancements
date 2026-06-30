using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardServer
    {
        private const string Feature = "WebDashboard";

        private static HttpListener? _listener;
        private static Thread? _listenerThread;
        private static volatile bool _running;
        private static bool _syncDeferred;
        private static string _listenUrl = "";
        private static string _assetsRoot = "";

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            ModConfig.Changed += OnConfigChanged;
            WebDashboardPatches.Apply(harmony);
            WebDashboardMinimapPatches.Apply(harmony);
        }

        private static void OnConfigChanged()
        {
            WebDashboardSnapshotCache.MarkDirty();
        }

        internal static void SyncFromConfig()
        {
            if (!ModConfig.IsInitialized)
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
            if (_syncDeferred && !WebDashboardConfigUpdateQueue.IsProcessing)
            {
                _syncDeferred = false;
                ApplySyncFromConfig();
            }

            if (!_running)
            {
                return;
            }

            WebDashboardActionQueue.Process();
            WebDashboardConfigUpdateQueue.Process();

            if (_syncDeferred && !WebDashboardConfigUpdateQueue.IsProcessing)
            {
                _syncDeferred = false;
                ApplySyncFromConfig();
            }

            if (!_running)
            {
                return;
            }

            WebDashboardSnapshotCache.Tick(_listenUrl);
        }

        internal static void StopOnDeinit()
        {
            ModConfig.Changed -= OnConfigChanged;
            Stop();
        }

        private static void Start(string prefix)
        {
            _assetsRoot = ResolveAssetsRoot();
            if (string.IsNullOrEmpty(_assetsRoot) || !Directory.Exists(_assetsRoot))
            {
                ModLog.Error(Feature, $"Assets folder not found at expected path next to the mod DLL.");
                return;
            }

            WebDashboardRouter.SetAssetsRoot(_assetsRoot);
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
            WebDashboardSseHub.Shutdown();
            HttpListener? listener = _listener;
            _listener = null;
            _listenUrl = "";

            if (listener != null)
            {
                try
                {
                    listener.Stop();
                    listener.Close();
                }
                catch
                {
                    /* shutting down */
                }
            }

            Thread? thread = _listenerThread;
            _listenerThread = null;
            if (thread != null && thread.IsAlive && thread != Thread.CurrentThread)
            {
                try
                {
                    _ = thread.Join(500);
                }
                catch
                {
                    /* ignore */
                }
            }
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

        private static string ResolveAssetsRoot()
        {
            try
            {
                string? dllDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return string.IsNullOrEmpty(dllDir) ? "" : Path.Combine(dllDir, "MimesisPlayerEnhancement", "assets");
            }
            catch
            {
                return "";
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
