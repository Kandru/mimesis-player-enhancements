using System;
using System.Collections;
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
        private static FieldInfo? _avatarCacheField;

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

            Texture2D? texture = TryGetTexture(steamId);
            if (texture == null)
            {
                return false;
            }

            try
            {
                png = ImageConversion.EncodeToPNG(texture);
                if (png.Length == 0)
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
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Avatar PNG encode failed for steamId={steamId}: {ex.Message}");
                return false;
            }
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

            _uimanField ??= typeof(Hub).GetField("uiman", InstanceMemberFlags)
                ?? typeof(Hub).GetField("<uiman>k__BackingField", InstanceMemberFlags);
            return _uimanField?.GetValue(hub) as UIManager;
        }

        private static Texture2D? TryReadAvatarCache(UIPrefab_InGameMenu menu, CSteamID steamId)
        {
            _avatarCacheField ??= typeof(UIPrefab_InGameMenu).GetField("avatarCache", InstanceMemberFlags);
            if (_avatarCacheField?.GetValue(menu) is not IDictionary cache)
            {
                return null;
            }

            foreach (DictionaryEntry entry in cache)
            {
                if (entry.Key is CSteamID cachedId && cachedId == steamId && entry.Value is Texture2D texture)
                {
                    return texture;
                }
            }

            return null;
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
