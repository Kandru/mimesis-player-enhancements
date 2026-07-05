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
                Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
                object? main = pdata?.GetType().GetField("main", InstanceMemberFlags)?.GetValue(pdata);
                if (main != null)
                {
                    _steamIdToNameCacheField ??= main.GetType().GetField("steamIDToNameCache", InstanceMemberFlags);
                    if (_steamIdToNameCacheField != null)
                    {
                        if (!ReferenceEquals(_cachedNameCacheOwner, main))
                        {
                            _cachedNameCacheOwner = main;
                            _cachedNameDictionary = _steamIdToNameCacheField.GetValue(main) as Dictionary<ulong, string>;
                        }

                        if (_cachedNameDictionary != null
                            && _cachedNameDictionary.TryGetValue(steamId, out string? name)
                            && !string.IsNullOrWhiteSpace(name))
                        {
                            return name;
                        }
                    }
                }

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
    }
}
