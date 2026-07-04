using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardRouter
    {
        private const string Feature = "WebDashboard";

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
                ModLog.Warn(Feature, $"Request failed: {ex.Message}");
                TryWriteError(context, 500, "Internal server error.");
            }
        }

        private static void HandleApi(HttpListenerContext context, string method, string path)
        {
            if (path == "/api/events" && method == "GET")
            {
                WebDashboardSseHub.Subscribe(context);
                return;
            }

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

            if (path == "/api/minimap" && method == "GET")
            {
                HandleMinimapApi(context, snapshot);
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

            if (path == "/api/settings/global" && method == "GET")
            {
                if (!WebDashboardGameState.CanEditGlobalSettings())
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, "Host only."));
                    return;
                }

                WriteJson(context, 200, WebDashboardJson.SerializeSettings(WebDashboardConfigBridge.BuildGlobalSettings()));
                return;
            }

            if (path == "/api/settings/global" && method == "POST")
            {
                if (!WebDashboardGameState.CanEditGlobalSettings())
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, "Host only."));
                    return;
                }

                WebDashboardConfigUpdateRequest? globalRequest = ModJson.Deserialize<WebDashboardConfigUpdateRequest>(ReadRequestBody(context.Request));
                if (globalRequest == null
                    || string.IsNullOrWhiteSpace(globalRequest.SectionId)
                    || string.IsNullOrWhiteSpace(globalRequest.Key))
                {
                    WriteJson(context, 400, WebDashboardJson.SerializeError(400, "Invalid settings update request."));
                    return;
                }

                WebDashboardConfigUpdateResult globalResult = WebDashboardConfigUpdateQueue.EnqueueAndWait(
                    WebDashboardConfigScope.Global,
                    saveSlotId: -1,
                    globalRequest.SectionId,
                    globalRequest.Key,
                    globalRequest.Value ?? "");

                WriteJson(context, globalResult.Success ? 200 : 400, WebDashboardJson.SerializeConfigUpdateResult(globalResult));
                return;
            }

            if (path == "/api/settings/save" && method == "GET")
            {
                if (!WebDashboardGameState.CanEditSaveSettings())
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, "Host only."));
                    return;
                }

                int saveSlotId = snapshot.Status.SaveSlotId;
                WriteJson(context, 200, WebDashboardJson.SerializeSettings(WebDashboardConfigBridge.BuildSaveSettings(saveSlotId)));
                return;
            }

            if (path == "/api/settings/save" && method == "POST")
            {
                if (!WebDashboardGameState.CanEditSaveSettings())
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, "Host only."));
                    return;
                }

                WebDashboardConfigUpdateRequest? saveRequest = ModJson.Deserialize<WebDashboardConfigUpdateRequest>(ReadRequestBody(context.Request));
                if (saveRequest == null
                    || string.IsNullOrWhiteSpace(saveRequest.SectionId)
                    || string.IsNullOrWhiteSpace(saveRequest.Key))
                {
                    WriteJson(context, 400, WebDashboardJson.SerializeError(400, "Invalid settings update request."));
                    return;
                }

                WebDashboardConfigUpdateResult saveResult = WebDashboardConfigUpdateQueue.EnqueueAndWait(
                    WebDashboardConfigScope.Save,
                    snapshot.Status.SaveSlotId,
                    saveRequest.SectionId,
                    saveRequest.Key,
                    saveRequest.Value ?? "");

                WriteJson(context, saveResult.Success ? 200 : 400, WebDashboardJson.SerializeConfigUpdateResult(saveResult));
                return;
            }

            if (path.StartsWith("/api/players/", StringComparison.Ordinal))
            {
                HandlePlayerApi(context, method, path, snapshot);
                return;
            }

            WriteJson(context, 404, WebDashboardJson.SerializeError(404, "Not found."));
        }

        private static void HandleMinimapApi(HttpListenerContext context, WebDashboardSnapshot snapshot)
        {
            if (!snapshot.Status.IsConnected)
            {
                WriteJson(context, 404, WebDashboardJson.SerializeError(404, "Not connected to a game."));
                return;
            }

            NameValueCollection query = context.Request.QueryString;
            bool showAll = string.Equals(query["showAll"], "true", StringComparison.OrdinalIgnoreCase);
            if (showAll && !snapshot.Status.IsHost)
            {
                WriteJson(context, 403, WebDashboardJson.SerializeError(403, "Host only."));
                return;
            }

            ulong focusSteamId = 0;
            string? focusParam = query["focusSteamId"];
            if (!string.IsNullOrWhiteSpace(focusParam) && !ulong.TryParse(focusParam, out focusSteamId))
            {
                WriteJson(context, 400, WebDashboardJson.SerializeError(400, "Invalid focusSteamId."));
                return;
            }

            List<WebDashboardMinimapMarkerDto> markers = WebDashboardMinimapService.FilterMarkers(
                snapshot.MinimapMarkers,
                focusSteamId,
                showAll,
                snapshot.Status.IsHost);

            WriteJson(
                context,
                200,
                WebDashboardJson.SerializeMinimap(snapshot.MinimapLayout, markers, snapshot.MinimapTrain));
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

                int slotId = snapshot.Status.SaveSlotId;
                if (slotId < 0)
                {
                    WriteJson(context, 404, WebDashboardJson.SerializeError(404, "No active save slot."));
                    return;
                }

                if (StatisticsTracker.TryGetPlayerDocument(steamId) is not PlayerStatisticsDocument doc)
                {
                    WriteJson(context, 404, WebDashboardJson.SerializeError(404, "Player statistics not found."));
                    return;
                }

                string? displayName = WebDashboardPlayerService.ResolveDisplayNameForSteamId(steamId, slotId);
                string json = WebDashboardJson.SerializePlayerStatsSnapshot(doc, displayName);
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
                "respawn" => WebDashboardActionType.Respawn,
                "heal" => WebDashboardActionType.Heal,
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
            if (!WebDashboardEmbeddedAssets.TryRead(path, out byte[] bytes, out string extension)
                && !WebDashboardEmbeddedAssets.TryRead(WebDashboardEmbeddedAssets.IndexWebPath, out bytes, out extension))
            {
                WriteText(context, 404, "text/plain", "Not found.");
                return;
            }

            string contentType = GetContentType(extension);
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

        private static string ReadRequestBody(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
            {
                return "";
            }

            using StreamReader reader = new(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
            return reader.ReadToEnd();
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
