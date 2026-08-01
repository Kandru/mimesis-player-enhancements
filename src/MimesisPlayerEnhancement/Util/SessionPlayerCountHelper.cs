using System.Reflection;
using ReluNetwork.ConstEnum;
using MimesisPlayerEnhancement.Features.MoreVoices;

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

        /// <summary>Lobby roster size for local UI gates. Uses host session when available;
        /// falls back to client-visible voice roster when <see cref="VWorld"/> is absent.</summary>
        internal static bool TryResolveLobbyPlayerCount(out int playerCount)
        {
            if (TryResolveExactFromSession(out playerCount))
            {
                return true;
            }

            Hub.PersistentData? pdata = GameSessionAccess.TryGetPdata();
            if (pdata != null && pdata.SessionJoined && pdata.ClientMode == NetworkClientMode.Participant)
            {
                int voiceCount = ResolveVoicePlayerCount();
                playerCount = voiceCount > 1 ? voiceCount : 2;
                return true;
            }

            int fallbackCount = ResolveVoicePlayerCount();
            if (fallbackCount > 0)
            {
                playerCount = fallbackCount;
                return true;
            }

            playerCount = 0;
            return false;
        }

        internal static bool IsMultiplayerLobby() =>
            TryResolveLobbyPlayerCount(out int playerCount) && playerCount > 1;

        private static int ResolveVoicePlayerCount()
        {
            try
            {
                return MoreVoicesVoiceAccess.TryGetVoiceManager()?.Players?.Count ?? 0;
            }
            catch
            {
                return 0;
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
