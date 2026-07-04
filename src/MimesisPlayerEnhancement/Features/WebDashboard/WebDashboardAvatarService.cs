using System.Net;
using System.Threading;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardAvatarService
    {
        private const int MaxPrewarmPerRefresh = 2;

        private static readonly Dictionary<ulong, CachedAvatar> ServeCache = [];
        private static byte[]? _defaultAvatarBytes;

        private sealed class CachedAvatar
        {
            internal readonly byte[] Bytes;
            internal readonly string ContentType;

            internal CachedAvatar(byte[] bytes, string contentType)
            {
                Bytes = bytes;
                ContentType = contentType;
            }
        }

        internal static void StorePng(ulong steamId, byte[] png)
        {
            if (steamId == 0 || png.Length == 0)
            {
                return;
            }

            StoreServeCache(steamId, new CachedAvatar(png, "image/png"));
        }

        internal static void PrewarmForPlayers(IReadOnlyList<ulong> steamIds)
        {
            if (WebDashboardGameAvatarSource.SyncFromInGameMenu())
            {
                WebDashboardSnapshotCache.MarkDirty();
            }

            int encodedThisRefresh = 0;
            foreach (ulong steamId in steamIds)
            {
                if (steamId == 0)
                {
                    continue;
                }

                if (TryGetServeCache(steamId, out _))
                {
                    continue;
                }

                if (encodedThisRefresh >= MaxPrewarmPerRefresh)
                {
                    break;
                }

                if (WebDashboardGameAvatarSource.TryGetPng(steamId, out byte[] png))
                {
                    StoreServeCache(steamId, new CachedAvatar(png, "image/png"));
                    encodedThisRefresh++;
                }
            }
        }

        internal static void Invalidate(ulong steamId)
        {
            if (steamId == 0)
            {
                return;
            }

            lock (ServeCache)
            {
                _ = ServeCache.Remove(steamId);
            }
        }

        internal static void Clear()
        {
            lock (ServeCache)
            {
                ServeCache.Clear();
            }

            WebDashboardGameAvatarSource.Clear();
        }

        internal static bool TryServe(HttpListenerContext context, ulong steamId)
        {
            if (TryGetServeCache(steamId, out CachedAvatar? cached))
            {
                WriteImage(context, cached.Bytes, cached.ContentType, cacheable: true);
                return true;
            }

            WriteImage(context, GetDefaultAvatar(), "image/svg+xml", cacheable: false);
            return true;
        }

        private static bool TryGetServeCache(ulong steamId, out CachedAvatar cached)
        {
            lock (ServeCache)
            {
                if (ServeCache.TryGetValue(steamId, out cached!))
                {
                    return cached.Bytes.Length > 0;
                }
            }

            cached = null!;
            return false;
        }

        private static void StoreServeCache(ulong steamId, CachedAvatar entry)
        {
            lock (ServeCache)
            {
                if (ServeCache.Count >= 64)
                {
                    ServeCache.Clear();
                }

                ServeCache[steamId] = entry;
            }
        }

        private static byte[] GetDefaultAvatar()
        {
            byte[]? cached = Volatile.Read(ref _defaultAvatarBytes);
            if (cached != null)
            {
                return cached;
            }

            byte[] bytes = LoadDefaultAvatar();
            _ = Interlocked.Exchange(ref _defaultAvatarBytes, bytes);
            return bytes;
        }

        private static byte[] LoadDefaultAvatar()
        {
            if (WebDashboardEmbeddedAssets.TryRead("img/default-avatar.svg", out byte[] bytes, out _))
            {
                return bytes;
            }

            return DefaultAvatarSvgBytes;
        }

        private static readonly byte[] DefaultAvatarSvgBytes = System.Text.Encoding.UTF8.GetBytes(
            "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 64 64\">" +
            "<rect width=\"64\" height=\"64\" fill=\"#21262d\"/>" +
            "<circle cx=\"32\" cy=\"24\" r=\"12\" fill=\"#484f58\"/>" +
            "<ellipse cx=\"32\" cy=\"54\" rx=\"18\" ry=\"14\" fill=\"#484f58\"/>" +
            "</svg>");

        private static void WriteImage(HttpListenerContext context, byte[] bytes, string contentType, bool cacheable)
        {
            context.Response.StatusCode = 200;
            context.Response.ContentType = contentType;
            context.Response.ContentLength64 = bytes.Length;
            context.Response.Headers["Cache-Control"] = cacheable ? "public, max-age=3600" : "no-cache";
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.OutputStream.Close();
        }
    }
}
