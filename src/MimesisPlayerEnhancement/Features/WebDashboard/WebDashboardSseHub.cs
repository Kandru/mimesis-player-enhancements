using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardSseHub
    {
        private const string Feature = "WebDashboard";
        private const int SnapshotThrottleMs = 1000;
        private const int MinimapThrottleMs = 100;
        private const int LoopWaitMs = 50;
        private const int KeepaliveMs = 15000;

        private static readonly object Gate = new();
        private static readonly List<SseClient> Clients = [];
        private static readonly AutoResetEvent BroadcastSignal = new(false);
        private static Thread? _broadcastThread;
        private static volatile bool _shuttingDown;
        private static volatile bool _pendingSnapshotBroadcast;
        private static volatile bool _pendingMinimapBroadcast;
        private static int _clientCount;
        private static long _lastSnapshotPublishMs;
        private static long _lastMinimapPublishMs;
        private static int _lastPublishedVersion;

        internal static bool HasClients => Volatile.Read(ref _clientCount) > 0;

        internal static void Start()
        {
            lock (Gate)
            {
                if (_broadcastThread != null && _broadcastThread.IsAlive)
                {
                    return;
                }

                _shuttingDown = false;
                _broadcastThread = new Thread(BroadcastLoop)
                {
                    IsBackground = true,
                    Name = "MimesisWebDashboardSse",
                };
                _broadcastThread.Start();
            }
        }

        internal static void Subscribe(HttpListenerContext context)
        {
            HttpListenerResponse response = context.Response;
            response.StatusCode = 200;
            response.ContentType = "text/event-stream; charset=utf-8";
            response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            response.Headers["Connection"] = "keep-alive";
            response.SendChunked = true;

            SseClient client = new(context);
            string initialPayload = WebDashboardJson.SerializeSnapshotEvent(WebDashboardSnapshotCache.Get());

            lock (Gate)
            {
                if (_shuttingDown)
                {
                    TryClose(client);
                    return;
                }

                Clients.Add(client);
                _ = Interlocked.Increment(ref _clientCount);
            }

            WebDashboardSnapshotCache.MarkDirty();
            WebDashboardSnapshotCache.RequestFullPublish();

            try
            {
                if (!client.TryWriteEvent("snapshot", initialPayload))
                {
                    return;
                }

                while (!_shuttingDown && client.Active)
                {
                    if (!client.Signal.WaitOne(KeepaliveMs))
                    {
                        if (_shuttingDown || !client.Active)
                        {
                            break;
                        }

                        if (!client.TryWriteComment("ping"))
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!_shuttingDown)
                {
                    ModLog.Warn(Feature, $"SSE client disconnected: {ex.Message}");
                }
            }
            finally
            {
                RemoveClient(client);
                TryClose(client);
            }
        }

        internal static void NotifySnapshotChanged()
        {
            if (_shuttingDown || Volatile.Read(ref _clientCount) == 0)
            {
                return;
            }

            _pendingSnapshotBroadcast = true;
            _ = BroadcastSignal.Set();
        }

        internal static void NotifyMinimapChanged()
        {
            if (_shuttingDown || Volatile.Read(ref _clientCount) == 0)
            {
                return;
            }

            _pendingMinimapBroadcast = true;
            _ = BroadcastSignal.Set();
        }

        internal static void Shutdown()
        {
            _shuttingDown = true;
            _pendingSnapshotBroadcast = false;
            _pendingMinimapBroadcast = false;
            _ = BroadcastSignal.Set();

            List<SseClient> toClose;
            lock (Gate)
            {
                toClose = [.. Clients];
                Clients.Clear();
                _clientCount = 0;
            }

            foreach (SseClient client in toClose)
            {
                client.Active = false;
                _ = client.Signal.Set();
                TryClose(client);
            }

            Thread? thread = _broadcastThread;
            _broadcastThread = null;
            if (thread != null && thread.IsAlive && thread != Thread.CurrentThread)
            {
                try
                {
                    _ = thread.Join(500);
                }
                catch
                {
                    /* shutting down */
                }
            }
        }

        private static void BroadcastLoop()
        {
            while (!_shuttingDown)
            {
                _ = BroadcastSignal.WaitOne(LoopWaitMs);
                if (_shuttingDown)
                {
                    break;
                }

                TryBroadcastMinimap();
                TryBroadcastSnapshot();
            }
        }

        private static void TryBroadcastMinimap()
        {
            if (!_pendingMinimapBroadcast || !IsThrottleReady(_lastMinimapPublishMs, MinimapThrottleMs))
            {
                return;
            }

            SseClient[]? clients = CopyClientsAndClearPending(isMinimap: true);
            if (clients == null)
            {
                return;
            }

            WebDashboardSnapshot snapshot = WebDashboardSnapshotCache.Get();
            string payload;
            try
            {
                List<WebDashboardMinimapMarkerDto> filteredMarkers =
                    WebDashboardMinimapService.FilterMarkersForClient(snapshot.MinimapMarkers);
                payload = WebDashboardJson.SerializeMinimap(
                    snapshot.MinimapLayout,
                    filteredMarkers,
                    snapshot.MinimapTrain);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SSE minimap serialization failed: {ex.Message}");
                return;
            }

            _lastMinimapPublishMs = UtcNowMs();
            BroadcastEvent(clients, "minimap", payload);
        }

        private static void TryBroadcastSnapshot()
        {
            if (!_pendingSnapshotBroadcast || !IsThrottleReady(_lastSnapshotPublishMs, SnapshotThrottleMs))
            {
                return;
            }

            SseClient[]? clients = CopyClientsAndClearPending(isMinimap: false);
            if (clients == null)
            {
                return;
            }

            int snapshotVersion = WebDashboardSnapshotCache.Version;
            string? payload = WebDashboardSnapshotEventCache.TryGetPayload(snapshotVersion);
            if (string.IsNullOrEmpty(payload))
            {
                WebDashboardSnapshotEventCache.ScheduleBuild(
                    WebDashboardSnapshotCache.Get(),
                    snapshotVersion,
                    livePlayersOnly: false,
                    livePlayers: null);
                _pendingSnapshotBroadcast = true;
                _ = BroadcastSignal.Set();
                return;
            }

            _lastSnapshotPublishMs = UtcNowMs();
            _lastPublishedVersion = snapshotVersion;
            BroadcastEvent(clients, "snapshot", payload);

            if (WebDashboardSnapshotCache.Version != _lastPublishedVersion)
            {
                _pendingSnapshotBroadcast = true;
                _ = BroadcastSignal.Set();
            }
        }

        private static SseClient[]? CopyClientsAndClearPending(bool isMinimap)
        {
            lock (Gate)
            {
                if (_shuttingDown || Clients.Count == 0)
                {
                    if (isMinimap)
                    {
                        _pendingMinimapBroadcast = false;
                    }
                    else
                    {
                        _pendingSnapshotBroadcast = false;
                    }

                    return null;
                }

                if (isMinimap)
                {
                    _pendingMinimapBroadcast = false;
                }
                else
                {
                    _pendingSnapshotBroadcast = false;
                }

                return [.. Clients];
            }
        }

        private static void BroadcastEvent(SseClient[] clients, string eventName, string payload)
        {
            foreach (SseClient client in clients)
            {
                if (!client.TryWriteEvent(eventName, payload))
                {
                    RemoveClient(client);
                    TryClose(client);
                    continue;
                }

                _ = client.Signal.Set();
            }
        }

        private static bool IsThrottleReady(long lastPublishMs, int throttleMs)
        {
            return lastPublishMs == 0 || UtcNowMs() - lastPublishMs >= throttleMs;
        }

        private static void RemoveClient(SseClient client)
        {
            lock (Gate)
            {
                if (Clients.Remove(client))
                {
                    _ = Interlocked.Decrement(ref _clientCount);
                }
            }
        }

        private static void TryClose(SseClient client)
        {
            client.Active = false;
            try
            {
                client.Context.Response.OutputStream.Close();
            }
            catch
            {
                /* client gone */
            }

            try
            {
                client.Context.Response.Close();
            }
            catch
            {
                /* client gone */
            }
        }

        private static long UtcNowMs()
        {
            return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }

        private sealed class SseClient
        {
            internal readonly HttpListenerContext Context;
            internal readonly AutoResetEvent Signal = new(false);
            internal volatile bool Active = true;
            private readonly object _writeLock = new();
            private Stream? _stream;

            internal SseClient(HttpListenerContext context)
            {
                Context = context;
            }

            internal bool TryWriteEvent(string eventName, string data)
            {
                StringBuilder frame = new();
                _ = frame.Append("event: ").Append(eventName).Append('\n');
                foreach (string line in data.Split('\n'))
                {
                    _ = frame.Append("data: ").Append(line).Append('\n');
                }

                _ = frame.Append('\n');
                return TryWrite(frame.ToString());
            }

            internal bool TryWriteComment(string comment)
            {
                return TryWrite(": " + comment + "\n\n");
            }

            private bool TryWrite(string text)
            {
                if (!Active || _shuttingDown)
                {
                    return false;
                }

                lock (_writeLock)
                {
                    try
                    {
                        _stream ??= Context.Response.OutputStream;
                        byte[] bytes = Encoding.UTF8.GetBytes(text);
                        _stream.Write(bytes, 0, bytes.Length);
                        _stream.Flush();
                        return true;
                    }
                    catch
                    {
                        Active = false;
                        return false;
                    }
                }
            }
        }
    }
}
