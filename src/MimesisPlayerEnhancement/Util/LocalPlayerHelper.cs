using System.Reflection;

namespace MimesisPlayerEnhancement.Util
{
    /// <summary>
    /// Platform-level local Steam ID before Hub/pdata is ready. In session prefer
    /// <see cref="GameSessionAccess.GetLocalSteamId"/>.
    /// </summary>
    internal static class LocalPlayerHelper
    {
        private const BindingFlags InstanceMemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly FieldInfo? UniqueUserPathField =
            typeof(PlatformMgr).GetField("_uniqueUserPath", InstanceMemberFlags);

        internal static ulong TryGetLocalSteamId() => GameSessionAccess.GetLocalSteamId();

        internal static ulong TryGetPlatformSteamId()
        {
            try
            {
                return TryReadPlatformSteamId(MonoSingleton<PlatformMgr>.Instance);
            }
            catch
            {
                return 0;
            }
        }

        internal static bool IsLocalSteamId(ulong steamId)
        {
            if (steamId == 0)
            {
                return false;
            }

            ulong localSteamId = GameSessionAccess.GetLocalSteamId();
            return localSteamId != 0 && localSteamId == steamId;
        }

        private static ulong TryReadPlatformSteamId(PlatformMgr platformMgr)
        {
            string? userPath = UniqueUserPathField?.GetValue(platformMgr) as string;
            return !string.IsNullOrEmpty(userPath) && ulong.TryParse(userPath, out ulong localSteam)
                ? localSteam
                : 0;
        }
    }
}
