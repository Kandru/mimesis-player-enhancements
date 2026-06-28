using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardSseHub
    {
        private const string Feature = "WebDashboard";
        private const int ThrottleMs = 1000;
        private const int KeepaliveMs = 15000;

        private static readonly object Gate = new();
        private static readonly List<SseClient> Clients = [];
        private static readonly AutoResetEvent BroadcastSignal = new(false);
        private static Thread? _broadcastThread;
        private static volatile bool _shuttingDown;
        private static volatile bool _pendingBroadcast;
        private static int _clientCount;
        private static long _lastPublishMs;
        private static int _lastPublishedVersion;

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

            _pendingBroadcast = true;
            _ = BroadcastSignal.Set();
        }

        internal static void Shutdown()
        {
            _shuttingDown = true;
            _pendingBroadcast = false;
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
                _ = BroadcastSignal.WaitOne(ThrottleMs);
                if (_shuttingDown)
                {
                    break;
                }

                if (!_pendingBroadcast)
                {
                    continue;
                }

                WaitForThrottleWindow();

                if (_shuttingDown || !_pendingBroadcast)
                {
                    continue;
                }

                SseClient[] clients;
                lock (Gate)
                {
                    if (_shuttingDown || Clients.Count == 0)
                    {
                        _pendingBroadcast = false;
                        continue;
                    }

                    clients = [.. Clients];
                    _lastPublishMs = UtcNowMs();
                    _pendingBroadcast = false;
                }

                int snapshotVersion = WebDashboardSnapshotCache.Version;
                string payload;
                try
                {
                    payload = WebDashboardJson.SerializeSnapshotEvent(WebDashboardSnapshotCache.Get());
                }
                catch (Exception ex)
                {
                    ModLog.Warn(Feature, $"SSE snapshot serialization failed: {ex.Message}");
                    continue;
                }

                _lastPublishedVersion = snapshotVersion;

                foreach (SseClient client in clients)
                {
                    if (!client.TryWriteEvent("snapshot", payload))
                    {
                        RemoveClient(client);
                        TryClose(client);
                        continue;
                    }

                    _ = client.Signal.Set();
                }

                if (WebDashboardSnapshotCache.Version != _lastPublishedVersion)
                {
                    _pendingBroadcast = true;
                    _ = BroadcastSignal.Set();
                }
            }
        }

        private static void WaitForThrottleWindow()
        {
            while (!_shuttingDown)
            {
                long elapsed = UtcNowMs() - _lastPublishMs;
                if (_lastPublishMs == 0 || elapsed >= ThrottleMs)
                {
                    return;
                }

                int waitMs = (int)Math.Min(ThrottleMs - elapsed, ThrottleMs);
                _ = BroadcastSignal.WaitOne(waitMs);
            }
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
