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

        private static string L(string key) => ModL10n.Get($"api.{key}");

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
                TryWriteError(context, 500, L("internal_error"));
            }
        }

        private static void HandleApi(HttpListenerContext context, string method, string path)
        {
            if (path == "/api/events" && method == "GET")
            {
                WebDashboardSseHub.Subscribe(context);
                return;
            }

            if (path.StartsWith("/api/locale/", StringComparison.Ordinal) && method == "GET")
            {
                string locale = path["/api/locale/".Length..];
                if (ModLocaleAssets.TryReadLocaleJson(locale, out byte[] localeBytes))
                {
                    WriteBytes(context, 200, "application/json; charset=utf-8", localeBytes);
                    return;
                }

                WriteJson(context, 404, WebDashboardJson.SerializeError(404, L("not_found")));
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
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
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
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                WriteJson(context, 200, WebDashboardJson.SerializeSettings(WebDashboardConfigBridge.BuildGlobalSettings()));
                return;
            }

            if (path == "/api/settings/global" && method == "POST")
            {
                if (!WebDashboardGameState.CanEditGlobalSettings())
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                WebDashboardConfigUpdateRequest? globalRequest = ModJson.Deserialize<WebDashboardConfigUpdateRequest>(ReadRequestBody(context.Request));
                if (globalRequest == null
                    || string.IsNullOrWhiteSpace(globalRequest.SectionId)
                    || string.IsNullOrWhiteSpace(globalRequest.Key))
                {
                    WriteJson(context, 400, WebDashboardJson.SerializeError(400, L("invalid_settings_request")));
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
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
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
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                WebDashboardConfigUpdateRequest? saveRequest = ModJson.Deserialize<WebDashboardConfigUpdateRequest>(ReadRequestBody(context.Request));
                if (saveRequest == null
                    || string.IsNullOrWhiteSpace(saveRequest.SectionId)
                    || string.IsNullOrWhiteSpace(saveRequest.Key))
                {
                    WriteJson(context, 400, WebDashboardJson.SerializeError(400, L("invalid_settings_request")));
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

            if (path == "/api/items" && method == "GET")
            {
                if (!snapshot.Status.IsHost)
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                WriteJson(context, 200, WebDashboardJson.SerializeItems(WebDashboardItemCatalogService.BuildCatalog()));
                return;
            }

            if (path.StartsWith("/api/players/", StringComparison.Ordinal))
            {
                HandlePlayerApi(context, method, path, snapshot);
                return;
            }

            WriteJson(context, 404, WebDashboardJson.SerializeError(404, L("not_found")));
        }

        private static void HandleMinimapApi(HttpListenerContext context, WebDashboardSnapshot snapshot)
        {
            if (!snapshot.Status.IsConnected)
            {
                WriteJson(context, 404, WebDashboardJson.SerializeError(404, L("not_connected")));
                return;
            }

            NameValueCollection query = context.Request.QueryString;
            bool showAll = string.Equals(query["showAll"], "true", StringComparison.OrdinalIgnoreCase);
            if (showAll && !snapshot.Status.IsHost)
            {
                WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                return;
            }

            ulong focusSteamId = 0;
            string? focusParam = query["focusSteamId"];
            if (!string.IsNullOrWhiteSpace(focusParam) && !ulong.TryParse(focusParam, out focusSteamId))
            {
                WriteJson(context, 400, WebDashboardJson.SerializeError(400, L("invalid_focus_steam_id")));
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
                WriteJson(context, 400, WebDashboardJson.SerializeError(400, L("invalid_steam_id")));
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
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                int slotId = snapshot.Status.SaveSlotId;
                if (slotId < 0)
                {
                    WriteJson(context, 404, WebDashboardJson.SerializeError(404, L("no_active_save_slot")));
                    return;
                }

                if (StatisticsTracker.TryGetPlayerDocument(steamId) is not PlayerStatisticsDocument doc)
                {
                    WriteJson(context, 404, WebDashboardJson.SerializeError(404, L("player_stats_not_found")));
                    return;
                }

                string? displayName = WebDashboardPlayerService.ResolveDisplayNameForSteamId(steamId, slotId);
                string json = WebDashboardJson.SerializePlayerStatsSnapshot(doc, displayName);
                WriteJson(context, 200, json);
                return;
            }

            if (action == "spawn-item" && method == "POST")
            {
                if (!snapshot.Status.IsHost)
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                WebDashboardSpawnItemRequest? spawnRequest =
                    ModJson.Deserialize<WebDashboardSpawnItemRequest>(ReadRequestBody(context.Request));
                if (spawnRequest == null || string.IsNullOrWhiteSpace(spawnRequest.ItemId))
                {
                    WriteJson(context, 400, WebDashboardJson.SerializeError(400, L("invalid_spawn_item_request")));
                    return;
                }

                long spawnPlayerUid = ResolvePlayerUid(snapshot, steamId);

                WebDashboardSpawnItemResult spawnResult = WebDashboardItemSpawnQueue.EnqueueAndWait(
                    steamId,
                    spawnPlayerUid,
                    spawnRequest.ItemId,
                    spawnRequest.Percent);

                WriteJson(
                    context,
                    spawnResult.Success ? 200 : 400,
                    WebDashboardJson.SerializeSpawnItemResult(spawnResult));
                return;
            }

            if (!snapshot.Status.IsHost)
            {
                WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                return;
            }

            if (method != "POST")
            {
                WriteJson(context, 405, WebDashboardJson.SerializeError(405, L("method_not_allowed")));
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
                WriteJson(context, 404, WebDashboardJson.SerializeError(404, L("not_found")));
                return;
            }

            long playerUid = ResolvePlayerUid(snapshot, steamId);

            WebDashboardActionQueue.Enqueue(new WebDashboardPendingAction
            {
                Type = actionType.Value,
                SteamId = steamId,
                PlayerUid = playerUid,
            });

            WriteJson(context, 202, WebDashboardJson.SerializeActionResult(new WebDashboardActionResult
            {
                Success = true,
                Message = L("action_queued"),
            }));
        }

        private static long ResolvePlayerUid(WebDashboardSnapshot snapshot, ulong steamId)
        {
            foreach (WebDashboardPlayerDto player in snapshot.Players)
            {
                if (player.SteamId == steamId)
                {
                    return player.PlayerUid;
                }
            }

            return 0;
        }

        private static void ServeStatic(HttpListenerContext context, string path)
        {
            if (!WebDashboardEmbeddedAssets.TryRead(path, out byte[] bytes, out string extension)
                && !WebDashboardEmbeddedAssets.TryRead(WebDashboardEmbeddedAssets.IndexWebPath, out bytes, out extension))
            {
                WriteText(context, 404, "text/plain", L("not_found"));
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

        private static void WriteBytes(HttpListenerContext context, int statusCode, string contentType, byte[] bytes)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = contentType;
            context.Response.ContentLength64 = bytes.Length;
            context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.OutputStream.Close();
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
