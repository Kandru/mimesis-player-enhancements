using System.Reflection;

namespace MimesisPlayerEnhancement.Util
{
    internal static class SessionPlayerCountHelper
    {
        internal const int VanillaPlayerBaseline = 4;

        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly PropertyInfo? VWorldRoomManagerProperty =
            typeof(VWorld).GetProperty("VRoomManager", InstanceFlags);

        internal static int ResolveFromRoom(IVroom? room)
        {
            if (room != null)
            {
                try
                {
                    return room.GetMemberCount();
                }
                catch
                {
                    // Fall through to session count.
                }
            }

            return ResolveFromSession();
        }

        internal static int ResolveFromSession()
        {
            return TryResolveExactFromSession(out int count) ? count : VanillaPlayerBaseline;
        }

        /// <summary>Exact session roster size when available. Returns false when the session
        /// is not ready (does not fall back to <see cref="VanillaPlayerBaseline"/>).</summary>
        internal static bool TryResolveExactFromSession(out int playerCount)
        {
            playerCount = 0;
            VWorld? vworld = GameSessionAccess.TryGetVWorld();
            if (vworld == null)
            {
                return false;
            }

            if (VWorldRoomManagerProperty?.GetValue(vworld) is not VRoomManager roomManager)
            {
                return false;
            }

            try
            {
                int count = roomManager.GetPlayerCountInSession();
                if (count <= 0)
                {
                    return false;
                }

                playerCount = count;
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static int ResolveFromSession(GameSessionInfo? info)
        {
            return info?.TotalPlayerSteamIDs != null && info.TotalPlayerSteamIDs.Count > 0
                ? info.TotalPlayerSteamIDs.Count
                : ResolveFromSession();
        }
    }
}
