using System;
using System.Collections.Generic;
using System.Reflection;
using Steamworks;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardGameAvatarSource
    {
        private const string Feature = "WebDashboard";

        private const BindingFlags InstanceMemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<ulong, byte[]> PngCache = [];
        private static FieldInfo? _uimanField;
        private static PropertyInfo? _uimanProperty;
        private static FieldInfo? _avatarCacheField;
        private static FieldInfo? _playerUiElementsField;
        private static FieldInfo? _playerInfosField;

        internal static bool TryGetPng(ulong steamId, out byte[] png)
        {
            png = [];
            if (steamId == 0)
            {
                return false;
            }

            lock (PngCache)
            {
                if (PngCache.TryGetValue(steamId, out byte[]? cached))
                {
                    png = cached;
                    return cached.Length > 0;
                }
            }

            _ = SyncFromInGameMenu();

            lock (PngCache)
            {
                if (PngCache.TryGetValue(steamId, out byte[]? cached))
                {
                    png = cached;
                    return cached.Length > 0;
                }
            }

            Texture2D? texture = TryGetTexture(steamId);
            return texture != null && TryStoreTexture(steamId, texture, out png);
        }

        internal static bool OnAvatarLoaded(ulong steamId, Texture2D texture)
        {
            lock (PngCache)
            {
                if (PngCache.TryGetValue(steamId, out byte[]? cached) && cached.Length > 0)
                {
                    return false;
                }
            }

            if (!TryStoreTexture(steamId, texture, out byte[] png))
            {
                return false;
            }

            WebDashboardAvatarService.StorePng(steamId, png);
            return true;
        }

        internal static bool SyncFromInGameMenu(UIPrefab_InGameMenu? menu = null)
        {
            menu ??= ResolveInGameMenu(createIfMissing: false);
            if (menu == null)
            {
                return false;
            }

            bool imported = false;

            if (GetAvatarCache(menu) is Dictionary<CSteamID, Texture2D> cache)
            {
                foreach (KeyValuePair<CSteamID, Texture2D> pair in cache)
                {
                    if (pair.Value != null && OnAvatarLoaded(pair.Key.m_SteamID, pair.Value))
                    {
                        imported = true;
                    }
                }
            }

            if (TrySyncFromPlayerUi(menu))
            {
                imported = true;
            }

            return imported;
        }

        internal static void Clear()
        {
            lock (PngCache)
            {
                PngCache.Clear();
            }
        }

        private static Texture2D? TryGetTexture(ulong steamId)
        {
            CSteamID cSteamId = new(steamId);
            PumpSteamCallbacks();
            _ = SteamFriends.RequestUserInformation(cSteamId, false);

            Texture2D? fromMenu = TryGetTextureFromInGameMenu(cSteamId, createIfMissing: false);
            if (fromMenu != null)
            {
                return fromMenu;
            }

            Texture2D? fromSteam = LoadTextureFromSteamworks(cSteamId);
            return fromSteam ?? TryGetTextureFromInGameMenu(cSteamId, createIfMissing: true);
        }

        private static Texture2D? TryGetTextureFromInGameMenu(CSteamID steamId, bool createIfMissing)
        {
            UIPrefab_InGameMenu? menu = ResolveInGameMenu(createIfMissing);
            if (menu == null)
            {
                return null;
            }

            try
            {
                Texture2D? cached = TryReadAvatarCache(menu, steamId);
                return cached ?? menu.GetSteamAvatar(steamId);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"In-game menu avatar lookup failed for {steamId.m_SteamID}: {ex.Message}");
                return null;
            }
        }

        private static UIPrefab_InGameMenu? ResolveInGameMenu(bool createIfMissing)
        {
            try
            {
                UIManager? uiman = ResolveUiManager();
                if (uiman == null)
                {
                    return null;
                }

                if (createIfMissing && uiman.inGameMenu == null)
                {
                    uiman.OpenInGameMenu();
                    uiman.HideIngameMenu();
                }

                return uiman.inGameMenu;
            }
            catch
            {
                return null;
            }
        }

        private static UIManager? ResolveUiManager()
        {
            Hub? hub = Hub.s;
            if (hub == null)
            {
                return null;
            }

            _uimanProperty ??= typeof(Hub).GetProperty("uiman", InstanceMemberFlags);
            if (_uimanProperty?.GetValue(hub) is UIManager propertyManager)
            {
                return propertyManager;
            }

            _uimanField ??= typeof(Hub).GetField("uiman", InstanceMemberFlags)
                ?? typeof(Hub).GetField("<uiman>k__BackingField", InstanceMemberFlags);
            return _uimanField?.GetValue(hub) as UIManager;
        }

        private static Dictionary<CSteamID, Texture2D>? GetAvatarCache(UIPrefab_InGameMenu menu)
        {
            _avatarCacheField ??= typeof(UIPrefab_InGameMenu).GetField("avatarCache", InstanceMemberFlags);
            return _avatarCacheField?.GetValue(menu) as Dictionary<CSteamID, Texture2D>;
        }

        private static Texture2D? TryReadAvatarCache(UIPrefab_InGameMenu menu, CSteamID steamId)
        {
            Dictionary<CSteamID, Texture2D>? cache = GetAvatarCache(menu);
            return cache != null && cache.TryGetValue(steamId, out Texture2D? texture) ? texture : null;
        }

        private static bool TrySyncFromPlayerUi(UIPrefab_InGameMenu menu)
        {
            _playerUiElementsField ??= typeof(UIPrefab_InGameMenu).GetField("playerUIElements", InstanceMemberFlags);
            _playerInfosField ??= typeof(UIPrefab_InGameMenu).GetField("playerInfos", InstanceMemberFlags);

            if (_playerUiElementsField?.GetValue(menu) is not System.Collections.IList uiElements
                || _playerInfosField?.GetValue(menu) is not System.Collections.IList playerInfos)
            {
                return false;
            }

            int count = Math.Min(uiElements.Count, playerInfos.Count);
            bool imported = false;

            for (int i = 0; i < count; i++)
            {
                object? info = playerInfos[i];
                object? ui = uiElements[i];
                if (info == null || ui == null)
                {
                    continue;
                }

                string? steamIdText = info.GetType().GetField("steamID", InstanceMemberFlags)?.GetValue(info) as string;
                if (string.IsNullOrEmpty(steamIdText) || !ulong.TryParse(steamIdText, out ulong steamId) || steamId == 0)
                {
                    continue;
                }

                FieldInfo? avatarButtonField = ui.GetType().GetField("avatarButton", InstanceMemberFlags);
                object? avatarButton = avatarButtonField?.GetValue(ui);
                if (avatarButton == null)
                {
                    continue;
                }

                PropertyInfo? imageProperty = avatarButton.GetType().GetProperty("image", InstanceMemberFlags);
                object? image = imageProperty?.GetValue(avatarButton);
                PropertyInfo? spriteProperty = image?.GetType().GetProperty("sprite", InstanceMemberFlags);
                object? sprite = spriteProperty?.GetValue(image);
                PropertyInfo? textureProperty = sprite?.GetType().GetProperty("texture", InstanceMemberFlags);
                Texture2D? texture = textureProperty?.GetValue(sprite) as Texture2D;
                if (texture == null)
                {
                    continue;
                }

                if (OnAvatarLoaded(steamId, texture))
                {
                    imported = true;
                }
            }

            return imported;
        }

        private static bool TryStoreTexture(ulong steamId, Texture2D texture, out byte[] png)
        {
            png = [];
            if (!TryEncodeToPng(texture, out png))
            {
                return false;
            }

            lock (PngCache)
            {
                if (PngCache.Count >= 64)
                {
                    PngCache.Clear();
                }

                PngCache[steamId] = png;
            }

            return true;
        }

        private static bool TryEncodeToPng(Texture2D source, out byte[] png)
        {
            png = [];
            try
            {
                png = ImageConversion.EncodeToPNG(source);
                if (png.Length > 0)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Avatar PNG encode failed: {ex.Message}");
            }

            try
            {
                Texture2D copy = new(source.width, source.height, TextureFormat.RGBA32, mipChain: false);
                copy.SetPixels(source.GetPixels());
                copy.Apply();
                png = ImageConversion.EncodeToPNG(copy);
                UnityEngine.Object.Destroy(copy);
                if (png.Length > 0)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Avatar PNG readable-copy encode failed: {ex.Message}");
            }

            return false;
        }

        private static Texture2D? LoadTextureFromSteamworks(CSteamID steamId)
        {
            int imageHandle = SteamFriends.GetMediumFriendAvatar(steamId);
            if (imageHandle <= 0)
            {
                imageHandle = SteamFriends.GetLargeFriendAvatar(steamId);
            }

            if (imageHandle <= 0)
            {
                imageHandle = SteamFriends.GetSmallFriendAvatar(steamId);
            }

            if (imageHandle <= 0)
            {
                return null;
            }

            if (!SteamUtils.GetImageSize(imageHandle, out uint width, out uint height) || width == 0 || height == 0)
            {
                return null;
            }

            byte[] rgba = new byte[width * height * 4];
            return !SteamUtils.GetImageRGBA(imageHandle, rgba, rgba.Length) ? null : CreateFlippedTexture(rgba, (int)width, (int)height);
        }

        private static Texture2D CreateFlippedTexture(byte[] rgba, int width, int height)
        {
            Texture2D raw = new(width, height, TextureFormat.RGBA32, mipChain: false);
            raw.LoadRawTextureData(rgba);
            raw.Apply();

            Texture2D flipped = new(width, height, TextureFormat.RGBA32, mipChain: false);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    flipped.SetPixel(x, y, raw.GetPixel(x, height - 1 - y));
                }
            }

            flipped.Apply();
            return flipped;
        }

        private static void PumpSteamCallbacks()
        {
            try
            {
                SteamAPI.RunCallbacks();
            }
            catch
            {
                /* Steam may be unavailable during teardown */
            }
        }
    }
}
