using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    /// <summary>
    /// Resolves player display names via the game's <see cref="GameMainBase.ResolveNickName"/>
    /// (Steam persona + <c>steamIDToNameCache</c>, keyed by steam id string).
    /// </summary>
    internal static class StatisticsDisplayNameResolver
    {
        private const BindingFlags InstanceMemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static FieldInfo? _steamIdToNameCacheField;
        private static FieldInfo? _myNickNameField;
        private static object? _cachedNameCacheOwner;
        private static Dictionary<string, string>? _cachedNameDictionary;

        internal static void ClearRuntimeState()
        {
            _steamIdToNameCacheField = null;
            _myNickNameField = null;
            _cachedNameCacheOwner = null;
            _cachedNameDictionary = null;
        }

        internal static string Resolve(ulong steamId, string fallback)
        {
            try
            {
                string? localNick = null;
                ulong localSteam = 0;
                Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
                _myNickNameField ??= pdata?.GetType().GetField("MyNickName", InstanceMemberFlags);
                if (_myNickNameField?.GetValue(pdata) is string myNick
                    && !string.IsNullOrWhiteSpace(myNick))
                {
                    localNick = myNick;
                    localSteam = GameSessionAccess.GetLocalSteamId();
                }

                if (steamId != 0 && pdata?.main is GameMainBase main)
                {
                    string seed = !string.IsNullOrWhiteSpace(fallback) ? fallback : string.Empty;
                    string fromGame = main.ResolveNickName(steamId.ToString(), seed);
                    if (IsUsableResolvedName(fromGame, steamId))
                    {
                        return fromGame;
                    }
                }

                TryGetNameCache(out Dictionary<string, string>? cache);
                return ResolveFromSources(steamId, cache, localNick, localSteam, fallback);
            }
            catch
            {
                /* ignore */
            }

            return FallbackDisplayName(steamId, fallback);
        }

        /// <summary>
        /// Pure display-name resolution (test seam). Cache keys match the game: steam id strings.
        /// </summary>
        internal static string ResolveFromSources(
            ulong steamId,
            IReadOnlyDictionary<string, string>? cache,
            string? localNick,
            ulong localSteamId,
            string fallback)
        {
            if (cache != null
                && cache.TryGetValue(steamId.ToString(), out string? name)
                && !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            if (!string.IsNullOrWhiteSpace(localNick) && localSteamId == steamId)
            {
                return localNick!;
            }

            return FallbackDisplayName(steamId, fallback);
        }

        internal static bool TryResolveSteamId(string displayName, out ulong steamId)
        {
            steamId = 0;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                return false;
            }

            try
            {
                if (!TryGetNameCache(out Dictionary<string, string>? cache) || cache == null)
                {
                    return false;
                }

                return TryFindSteamIdByDisplayName(cache, displayName, out steamId);
            }
            catch
            {
                /* ignore */
            }

            return false;
        }

        /// <summary>
        /// Pure reverse lookup from a steamId-string→name map (test seam).
        /// </summary>
        internal static bool TryFindSteamIdByDisplayName(
            IReadOnlyDictionary<string, string> cache,
            string displayName,
            out ulong steamId)
        {
            steamId = 0;
            if (cache == null || string.IsNullOrWhiteSpace(displayName))
            {
                return false;
            }

            foreach (KeyValuePair<string, string> kvp in cache)
            {
                if (!string.Equals(kvp.Value, displayName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (ulong.TryParse(kvp.Key, out steamId) && steamId != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsUsableResolvedName(string? name, ulong steamId) =>
            !string.IsNullOrWhiteSpace(name) && name != steamId.ToString();

        private static string FallbackDisplayName(ulong steamId, string fallback) =>
            string.IsNullOrWhiteSpace(fallback) ? steamId.ToString() : fallback;

        private static bool TryGetNameCache([NotNullWhen(true)] out Dictionary<string, string>? cache)
        {
            cache = null;
            try
            {
                Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
                object? main = pdata?.main;
                if (main == null)
                {
                    return false;
                }

                _steamIdToNameCacheField ??= main.GetType().GetField("steamIDToNameCache", InstanceMemberFlags);
                if (_steamIdToNameCacheField == null)
                {
                    return false;
                }

                if (!ReferenceEquals(_cachedNameCacheOwner, main))
                {
                    _cachedNameCacheOwner = main;
                    _cachedNameDictionary = _steamIdToNameCacheField.GetValue(main) as Dictionary<string, string>;
                }

                cache = _cachedNameDictionary;
                return cache != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
