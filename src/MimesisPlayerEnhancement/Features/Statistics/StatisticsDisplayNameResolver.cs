using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MimesisPlayerEnhancement.Features.Statistics
{
    /// <summary>
    /// Resolves player display names from the game's steamID-to-name cache
    /// (reflection fields cached per scene owner).
    /// </summary>
    internal static class StatisticsDisplayNameResolver
    {
        private const BindingFlags InstanceMemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static FieldInfo? _steamIdToNameCacheField;
        private static FieldInfo? _myNickNameField;
        private static object? _cachedNameCacheOwner;
        private static Dictionary<ulong, string>? _cachedNameDictionary;

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
                TryGetNameCache(out Dictionary<ulong, string>? cache);
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

                return ResolveFromSources(steamId, cache, localNick, localSteam, fallback);
            }
            catch
            {
                /* ignore */
            }

            return FallbackDisplayName(steamId, fallback);
        }

        /// <summary>
        /// Pure display-name resolution (test seam).
        /// </summary>
        internal static string ResolveFromSources(
            ulong steamId,
            IReadOnlyDictionary<ulong, string>? cache,
            string? localNick,
            ulong localSteamId,
            string fallback)
        {
            if (cache != null
                && cache.TryGetValue(steamId, out string? name)
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
                if (!TryGetNameCache(out Dictionary<ulong, string>? cache) || cache == null)
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
        /// Pure reverse lookup from a steamId→name map (test seam).
        /// </summary>
        internal static bool TryFindSteamIdByDisplayName(
            IReadOnlyDictionary<ulong, string> cache,
            string displayName,
            out ulong steamId)
        {
            steamId = 0;
            if (cache == null || string.IsNullOrWhiteSpace(displayName))
            {
                return false;
            }

            foreach (KeyValuePair<ulong, string> kvp in cache)
            {
                if (string.Equals(kvp.Value, displayName, StringComparison.OrdinalIgnoreCase))
                {
                    steamId = kvp.Key;
                    return true;
                }
            }

            return false;
        }

        private static string FallbackDisplayName(ulong steamId, string fallback) =>
            string.IsNullOrWhiteSpace(fallback) ? steamId.ToString() : fallback;

        private static bool TryGetNameCache([NotNullWhen(true)] out Dictionary<ulong, string>? cache)
        {
            cache = null;
            try
            {
                Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
                object? main = pdata?.GetType().GetField("main", InstanceMemberFlags)?.GetValue(pdata);
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
                    _cachedNameDictionary = _steamIdToNameCacheField.GetValue(main) as Dictionary<ulong, string>;
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
