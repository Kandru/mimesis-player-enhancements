using System;
using System.IO;
using System.Net;
using System.Text;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardRouter
    {
        private static string _assetsRoot = "";

        internal static void SetAssetsRoot(string path)
        {
            _assetsRoot = path;
            WebDashboardAvatarService.SetAssetsRoot(path);
        }

        internal static void Handle(HttpListenerContext context)
        {
            try
            {
                string method = context.Request.HttpMethod.ToUpperInvariant();
                string path = NormalizePath(context.Request.Url?.AbsolutePath ?? "/");

                if (path.StartsWith("/api/", StringComparison.Ordinal))
                {
                    HandleApi(context, method, path);
                    return;
                }

                ServeStatic(context, path);
            }
            catch (Exception ex)
            {
                ModLog.Warn("WebDashboard", $"Request failed: {ex.Message}");
                TryWriteError(context, 500, "Internal server error.");
            }
        }

        private static void HandleApi(HttpListenerContext context, string method, string path)
        {
            WebDashboardSnapshot snapshot = WebDashboardSnapshotCache.Get();

            if (path == "/api/status" && method == "GET")
            {
                WriteJson(context, 200, WebDashboardJson.SerializeStatus(snapshot.Status));
                return;
            }

            if (path == "/api/players" && method == "GET")
            {
                WriteJson(context, 200, WebDashboardJson.SerializePlayers(snapshot.Players));
                return;
            }

            if (path == "/api/leaderboard" && method == "GET")
            {
                if (!snapshot.Status.IsHost)
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, "Host only."));
                    return;
                }

                if (string.IsNullOrEmpty(snapshot.LeaderboardJson))
                {
                    WriteJson(context, 200, /*lang=json,strict*/ "{\"saveSlotId\":-1,\"connectedSteamIds\":[],\"entries\":[]}");
                    return;
                }

                WriteJson(context, 200, snapshot.LeaderboardJson);
                return;
            }

            if (path == "/api/settings" && method == "GET")
            {
                if (!snapshot.Status.IsHost)
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, "Host only."));
                    return;
                }

                WriteJson(context, 200, WebDashboardJson.SerializeSettings(WebDashboardConfigBridge.BuildSettings()));
                return;
            }

            if (path.StartsWith("/api/players/", StringComparison.Ordinal))
            {
                HandlePlayerApi(context, method, path, snapshot);
                return;
            }

            WriteJson(context, 404, WebDashboardJson.SerializeError(404, "Not found."));
        }

        private static void HandlePlayerApi(HttpListenerContext context, string method, string path, WebDashboardSnapshot snapshot)
        {
            string remainder = path["/api/players/".Length..];
            int slash = remainder.IndexOf('/');
            string steamIdPart = slash >= 0 ? remainder[..slash] : remainder;
            string action = slash >= 0 ? remainder[(slash + 1)..] : "";

            if (!ulong.TryParse(steamIdPart, out ulong steamId) || steamId == 0)
            {
                WriteJson(context, 400, WebDashboardJson.SerializeError(400, "Invalid Steam ID."));
                return;
            }

            if (action == "avatar" && method == "GET")
            {
                _ = WebDashboardAvatarService.TryServe(context, steamId);
                return;
            }

            if (action == "stats" && method == "GET")
            {
                if (!snapshot.Status.IsHost)
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, "Host only."));
                    return;
                }

                if (snapshot.PlayerStatsJson.TryGetValue(steamId, out string? cached))
                {
                    WriteJson(context, 200, cached);
                    return;
                }

                int slotId = snapshot.Status.SaveSlotId;
                if (slotId < 0)
                {
                    WriteJson(context, 404, WebDashboardJson.SerializeError(404, "No active save slot."));
                    return;
                }

                string? json = WebDashboardStatisticsBridge.BuildPlayerStatsJson(slotId, steamId);
                if (string.IsNullOrEmpty(json))
                {
                    WriteJson(context, 404, WebDashboardJson.SerializeError(404, "Player statistics not found."));
                    return;
                }

                WriteJson(context, 200, json);
                return;
            }

            if (!snapshot.Status.IsHost)
            {
                WriteJson(context, 403, WebDashboardJson.SerializeError(403, "Host only."));
                return;
            }

            if (method != "POST")
            {
                WriteJson(context, 405, WebDashboardJson.SerializeError(405, "Method not allowed."));
                return;
            }

            WebDashboardActionType? actionType = action switch
            {
                "kick" => WebDashboardActionType.Kick,
                "ban" => WebDashboardActionType.Ban,
                "unban" => WebDashboardActionType.Unban,
                _ => null,
            };

            if (actionType == null)
            {
                WriteJson(context, 404, WebDashboardJson.SerializeError(404, "Not found."));
                return;
            }

            long playerUid = 0;
            foreach (WebDashboardPlayerDto player in snapshot.Players)
            {
                if (player.SteamId == steamId)
                {
                    playerUid = player.PlayerUid;
                    break;
                }
            }

            WebDashboardActionQueue.Enqueue(new WebDashboardPendingAction
            {
                Type = actionType.Value,
                SteamId = steamId,
                PlayerUid = playerUid,
            });

            WriteJson(context, 202, WebDashboardJson.SerializeActionResult(new WebDashboardActionResult
            {
                Success = true,
                Message = "Action queued.",
            }));
        }

        private static void ServeStatic(HttpListenerContext context, string path)
        {
            if (string.IsNullOrEmpty(_assetsRoot) || !Directory.Exists(_assetsRoot))
            {
                WriteText(context, 503, "text/plain", "Web dashboard assets not found.");
                return;
            }

            string relative = path == "/" ? "index.html" : path.TrimStart('/');
            string fullPath = Path.GetFullPath(Path.Combine(_assetsRoot, relative));
            string rootFull = Path.GetFullPath(_assetsRoot);
            if (!fullPath.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
            {
                WriteText(context, 403, "text/plain", "Forbidden.");
                return;
            }

            if (!File.Exists(fullPath))
            {
                string indexPath = Path.Combine(_assetsRoot, "index.html");
                if (File.Exists(indexPath))
                {
                    fullPath = indexPath;
                }
                else
                {
                    WriteText(context, 404, "text/plain", "Not found.");
                    return;
                }
            }

            string contentType = GetContentType(Path.GetExtension(fullPath));
            byte[] bytes = File.ReadAllBytes(fullPath);
            context.Response.StatusCode = 200;
            context.Response.ContentType = contentType;
            context.Response.ContentLength64 = bytes.Length;
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.OutputStream.Close();
        }

        private static string GetContentType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".html" => "text/html; charset=utf-8",
                ".css" => "text/css; charset=utf-8",
                ".js" => "application/javascript; charset=utf-8",
                ".json" => "application/json; charset=utf-8",
                ".svg" => "image/svg+xml",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".ico" => "image/x-icon",
                _ => "application/octet-stream",
            };
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "/";
            }

            int query = path.IndexOf('?');
            if (query >= 0)
            {
                path = path[..query];
            }

            return path.EndsWith('/') && path.Length > 1 ? path[..^1] : path;
        }

        private static void WriteJson(HttpListenerContext context, int statusCode, string json)
        {
            WriteText(context, statusCode, "application/json; charset=utf-8", json);
        }

        private static void WriteText(HttpListenerContext context, int statusCode, string contentType, string body)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = contentType;
            context.Response.ContentLength64 = bytes.Length;
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.OutputStream.Close();
        }

        private static void TryWriteError(HttpListenerContext context, int statusCode, string message)
        {
            try
            {
                WriteJson(context, statusCode, WebDashboardJson.SerializeError(statusCode, message));
            }
            catch
            {
                /* response may already be closed */
            }
        }
    }
}
