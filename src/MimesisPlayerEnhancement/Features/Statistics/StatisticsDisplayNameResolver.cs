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

        internal static string Resolve(ulong steamId, string fallback)
        {
            try
            {
                if (TryGetNameCache(out Dictionary<ulong, string>? cache)
                    && cache.TryGetValue(steamId, out string? name)
                    && !string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }

                Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
                _myNickNameField ??= pdata?.GetType().GetField("MyNickName", InstanceMemberFlags);
                if (_myNickNameField?.GetValue(pdata) is string myNick
                    && !string.IsNullOrWhiteSpace(myNick))
                {
                    ulong localSteam = GameSessionAccess.GetLocalSteamId();
                    if (localSteam == steamId)
                    {
                        return myNick;
                    }
                }
            }
            catch
            {
                /* ignore */
            }

            return string.IsNullOrWhiteSpace(fallback) ? steamId.ToString() : fallback;
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
                if (!TryGetNameCache(out Dictionary<ulong, string>? cache))
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
            }
            catch
            {
                /* ignore */
            }

            return false;
        }

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
