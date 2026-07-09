using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using MimesisPlayerEnhancement.Config.QuickSettings;
using MimesisPlayerEnhancement.Features.Statistics.Models;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardRouter
    {
        private const string Feature = "WebDashboard";

        private static string L(string key) => WebDashboardL10n.Get($"api.{key}");

        internal static void Handle(HttpListenerContext context)
        {
            WebDashboardRequestLocale.Set(context.Request);
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
            finally
            {
                WebDashboardRequestLocale.Clear();
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
                WebDashboardStatusDto status = new()
                {
                    IsConnected = snapshot.Status.IsConnected,
                    IsHost = snapshot.Status.IsHost,
                    SaveSlotId = snapshot.Status.SaveSlotId,
                    LobbyName = snapshot.Status.LobbyName,
                    ModVersion = snapshot.Status.ModVersion,
                    ListenUrl = snapshot.Status.ListenUrl,
                    SnapshotVersion = snapshot.Status.SnapshotVersion,
                    ConfigVersion = snapshot.Status.ConfigVersion,
                    JoinAnytimeRoutingCount = snapshot.Status.JoinAnytimeRoutingCount,
                    Locale = WebDashboardRequestLocale.Current,
                };
                WriteJson(context, 200, WebDashboardJson.SerializeStatus(status));
                return;
            }

            if (path == "/api/host-cheats" && method == "GET")
            {
                if (!snapshot.Status.IsHost)
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                WriteJson(context, 200, ModJson.Serialize(WebDashboardCatalogCache.GetHostCheats()));
                return;
            }

            if (path == "/api/host-cheats" && method == "POST")
            {
                if (!snapshot.Status.IsHost)
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                WebDashboardHostCheatsUpdateRequest? request =
                    ModJson.Deserialize<WebDashboardHostCheatsUpdateRequest>(ReadRequestBody(context.Request));
                WebDashboardHostCheatsDto result = WebDashboardHostCheatsQueue.EnqueueAndWait(
                    () => WebDashboardHostCheatsService.Apply(request));
                WriteJson(context, result.Success ? 200 : 400, ModJson.Serialize(result));
                return;
            }

            if (path == "/api/players" && method == "GET")
            {
                WriteJson(context, 200, WebDashboardJson.SerializePlayers(snapshot.Players));
                return;
            }

            if (path == "/api/minimap/blind" && method == "POST")
            {
                WebDashboardMinimapBlindRequest? blindRequest =
                    ModJson.Deserialize<WebDashboardMinimapBlindRequest>(ReadRequestBody(context.Request));
                if (blindRequest == null)
                {
                    WriteJson(context, 400, WebDashboardJson.SerializeError(400, L("invalid_settings_request")));
                    return;
                }

                WebDashboardMinimapBlindMode.SetEnabled(blindRequest.Enabled);
                WriteJson(context, 200, WebDashboardJson.SerializeActionResult(new WebDashboardActionResult
                {
                    Success = true,
                    Message = L("done"),
                }));
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
                if (!WebDashboardGameState.CanViewGlobalSettings())
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                WriteJson(context, 200, ModJson.Serialize(WebDashboardConfigBridge.BuildGlobalSettings()));
                return;
            }

            if (path == "/api/settings/global" && method == "POST")
            {
                WebDashboardConfigUpdateRequest? globalRequest = ModJson.Deserialize<WebDashboardConfigUpdateRequest>(ReadRequestBody(context.Request));
                if (globalRequest == null
                    || string.IsNullOrWhiteSpace(globalRequest.SectionId)
                    || string.IsNullOrWhiteSpace(globalRequest.Key))
                {
                    WriteJson(context, 400, WebDashboardJson.SerializeError(400, L("invalid_settings_request")));
                    return;
                }

                if (!WebDashboardGameState.CanEditGlobalSetting(globalRequest.SectionId, globalRequest.Key))
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
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

                int saveSlotId = WebDashboardGameState.GetSaveSlotId();
                try
                {
                    WebDashboardSettingsDto saveSettings = WebDashboardConfigUpdateQueue.EnqueueAndWait(
                        () => WebDashboardConfigBridge.BuildSaveSettings(saveSlotId));
                    WriteJson(context, 200, ModJson.Serialize(saveSettings));
                }
                catch (TimeoutException)
                {
                    WriteJson(context, 504, WebDashboardJson.SerializeError(504, L("timed_out")));
                }
                catch (Exception ex)
                {
                    WriteJson(context, 500, WebDashboardJson.SerializeError(500, ex.Message));
                }
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
                    WebDashboardGameState.GetSaveSlotId(),
                    saveRequest.SectionId,
                    saveRequest.Key,
                    saveRequest.Value ?? "");

                WriteJson(context, saveResult.Success ? 200 : 400, WebDashboardJson.SerializeConfigUpdateResult(saveResult));
                return;
            }

            if (path == "/api/settings/save/profile" && method == "GET")
            {
                if (!WebDashboardGameState.CanEditSaveSettings())
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                int profileSlotId = WebDashboardGameState.GetSaveSlotId();
                WebDashboardSaveProfileResponseDto profileResponse = WebDashboardConfigUpdateQueue.EnqueueAndWait(
                    () => WebDashboardQuickSettingsBridge.BuildSaveProfile(profileSlotId));
                WriteJson(context, 200, ModJson.Serialize(profileResponse));
                return;
            }

            if (path == "/api/settings/save/profile" && method == "POST")
            {
                if (!WebDashboardGameState.CanEditSaveSettings())
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                WebDashboardSaveProfileRequest? profileRequest = ModJson.Deserialize<WebDashboardSaveProfileRequest>(ReadRequestBody(context.Request));
                if (profileRequest == null || string.IsNullOrWhiteSpace(profileRequest.Mode))
                {
                    WriteJson(context, 400, WebDashboardJson.SerializeError(400, L("invalid_settings_request")));
                    return;
                }

                int profileSlotId = WebDashboardGameState.GetSaveSlotId();
                WebDashboardSaveProfileResponseDto profileResult = WebDashboardConfigUpdateQueue.EnqueueAndWait(
                    () => WebDashboardQuickSettingsBridge.ApplySaveProfile(profileSlotId, profileRequest));
                WriteJson(context, profileResult.Success ? 200 : 400, ModJson.Serialize(profileResult));
                return;
            }

            if (path == "/api/quick-presets" && method == "GET")
            {
                if (!WebDashboardGameState.CanEditSaveSettings())
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                WebDashboardQuickPresetsListDto presetList = WebDashboardConfigUpdateQueue.EnqueueAndWait(
                    WebDashboardQuickSettingsBridge.BuildPresetList);
                WriteJson(context, 200, ModJson.Serialize(presetList));
                return;
            }

            if (path == "/api/quick-presets" && method == "POST")
            {
                if (!WebDashboardGameState.CanEditSaveSettings())
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                WebDashboardQuickPresetSaveRequest? savePresetRequest = ModJson.Deserialize<WebDashboardQuickPresetSaveRequest>(ReadRequestBody(context.Request));
                if (savePresetRequest == null || string.IsNullOrWhiteSpace(savePresetRequest.Name))
                {
                    WriteJson(context, 400, WebDashboardJson.SerializeError(400, L("invalid_settings_request")));
                    return;
                }

                WebDashboardQuickPresetDto? savedPreset = WebDashboardConfigUpdateQueue.EnqueueAndWait(
                    () => WebDashboardQuickSettingsBridge.SaveUserPreset(savePresetRequest));
                if (savedPreset == null)
                {
                    WriteJson(context, 400, WebDashboardJson.SerializeError(400, L("failed_apply")));
                    return;
                }

                WriteJson(context, 200, ModJson.Serialize(savedPreset));
                return;
            }

            if (path == "/api/quick-presets/import" && method == "POST")
            {
                if (!WebDashboardGameState.CanEditSaveSettings())
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                WebDashboardQuickPresetImportRequest? importRequest = ModJson.Deserialize<WebDashboardQuickPresetImportRequest>(ReadRequestBody(context.Request));
                if (importRequest == null || string.IsNullOrWhiteSpace(importRequest.ShareString))
                {
                    WriteJson(context, 400, WebDashboardJson.SerializeError(400, L("quick_share_invalid")));
                    return;
                }

                int importSlotId = WebDashboardGameState.GetSaveSlotId();
                WebDashboardQuickPresetImportResultDto importResult = WebDashboardConfigUpdateQueue.EnqueueAndWait(
                    () => WebDashboardQuickSettingsBridge.ImportShareString(importSlotId, importRequest));
                WriteJson(context, importResult.Success ? 200 : 400, ModJson.Serialize(importResult));
                return;
            }

            if (path.StartsWith("/api/quick-presets/", StringComparison.Ordinal) && method == "DELETE")
            {
                if (!WebDashboardGameState.CanEditSaveSettings())
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                string deleteId = Uri.UnescapeDataString(path["/api/quick-presets/".Length..]);
                string? deleteErrorMsg = null;
                bool deleted = WebDashboardConfigUpdateQueue.EnqueueAndWait(
                    () => WebDashboardQuickSettingsBridge.DeleteUserPreset(deleteId, out deleteErrorMsg));
                if (!deleted)
                {
                    WriteJson(context, BuiltinQuickSettings.IsBuiltin(deleteId) ? 403 : 404, WebDashboardJson.SerializeError(404, !string.IsNullOrEmpty(deleteErrorMsg) ? deleteErrorMsg : L("quick_preset_not_found")));
                    return;
                }

                WriteJson(context, 200, WebDashboardJson.SerializeActionResult(new WebDashboardActionResult
                {
                    Success = true,
                    Message = L("quick_preset_deleted"),
                }));
                return;
            }

            if (path.StartsWith("/api/quick-presets/", StringComparison.Ordinal) && path.EndsWith("/export", StringComparison.Ordinal) && method == "GET")
            {
                if (!WebDashboardGameState.CanEditSaveSettings())
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                string exportPath = path["/api/quick-presets/".Length..];
                exportPath = exportPath[..^"/export".Length];
                string exportId = Uri.UnescapeDataString(exportPath);
                WebDashboardQuickPresetShareDto exportDto;
                if (string.Equals(exportId, "current", StringComparison.OrdinalIgnoreCase))
                {
                    int exportSlotId = WebDashboardGameState.GetSaveSlotId();
                    exportDto = WebDashboardConfigUpdateQueue.EnqueueAndWait(
                        () => WebDashboardQuickSettingsBridge.ExportCurrentSave(exportSlotId));
                }
                else
                {
                    exportDto = WebDashboardConfigUpdateQueue.EnqueueAndWait(
                        () => WebDashboardQuickSettingsBridge.ExportPreset(exportId));
                }

                WriteJson(context, 200, ModJson.Serialize(exportDto));
                return;
            }

            if (path == "/api/items" && method == "GET")
            {
                if (!snapshot.Status.IsHost)
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                WriteJson(context, 200, WebDashboardCatalogCache.GetItemsJson());
                return;
            }

            if (path == "/api/dungeons" && method == "GET")
            {
                if (!snapshot.Status.IsHost)
                {
                    WriteJson(context, 403, WebDashboardJson.SerializeError(403, L("host_only")));
                    return;
                }

                WriteJson(context, 200, WebDashboardCatalogCache.GetDungeonsJson());
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

                string? displayName;
                PlayerStatisticsDocument doc;
                try
                {
                    (doc, displayName) = WebDashboardConfigUpdateQueue.EnqueueAndWait(
                        () => WebDashboardPlayerStatsService.TryGetStats(steamId, slotId));
                }
                catch (TimeoutException)
                {
                    WriteJson(context, 504, WebDashboardJson.SerializeError(504, L("timed_out")));
                    return;
                }

                if (doc == null)
                {
                    WriteJson(context, 404, WebDashboardJson.SerializeError(404, L("player_stats_not_found")));
                    return;
                }

                string json = WebDashboardJson.SerializePlayerStatsSnapshot(doc, displayName);
                WriteJson(context, 200, json);
                return;
            }

            if (action == "delete" && method == "POST")
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

                WebDashboardActionResult deleteResult;
                try
                {
                    deleteResult = WebDashboardConfigUpdateQueue.EnqueueAndWait(
                        () => SaveSlotPlayerDataService.RemovePlayer(slotId, steamId));
                }
                catch (TimeoutException)
                {
                    WriteJson(context, 504, WebDashboardJson.SerializeError(504, L("timed_out")));
                    return;
                }

                WriteJson(
                    context,
                    deleteResult.Success ? 200 : 400,
                    WebDashboardJson.SerializeActionResult(deleteResult));
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

            if (action is "godmode" or "noclip")
            {
                long cheatPlayerUid = ResolvePlayerUid(snapshot, steamId);
                bool toggleGodMode = action == "godmode";
                WebDashboardHostCheatsDto cheatResult = WebDashboardHostCheatsQueue.EnqueueAndWait(
                    () => WebDashboardHostCheatsService.TogglePlayerCheat(steamId, cheatPlayerUid, toggleGodMode));
                WriteJson(context, cheatResult.Success ? 200 : 400, ModJson.Serialize(cheatResult));
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
