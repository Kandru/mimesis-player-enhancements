using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.JoinAnytime
{
    /// <summary>
    /// Pure helpers for 0.3.1 seamless reconnect eligibility (mirrors SessionManager.IsGraceEligible).
    /// </summary>
    internal static class JoinAnytimeReconnectLogic
    {
        internal static bool IsGraceEligible(SessionContext context, DisconnectReason reason)
        {
            if (context == null)
            {
                return false;
            }

            return IsGraceEligible(
                context.Session is VirtualAcceptSession,
                context.IsDummy,
                context.SteamID,
                context.GetPlayerUID(),
                context.ExistPlayer(),
                context.PlayerInfoSnapshot != null,
                reason);
        }

        internal static bool IsGraceEligible(
            bool isVirtualAcceptSession,
            bool isDummy,
            ulong steamId,
            long playerUid,
            bool hasPlayer,
            bool hasPlayerSnapshot,
            DisconnectReason reason)
        {
            if (isVirtualAcceptSession || isDummy)
            {
                return false;
            }

            if (steamId == 0 || playerUid == 0)
            {
                return false;
            }

            if (!hasPlayer && !hasPlayerSnapshot)
            {
                return false;
            }

            return reason == DisconnectReason.ByServer
                || reason is DisconnectReason.ConnectionError
                    or DisconnectReason.Undefined
                    or DisconnectReason.PacketError
                    or DisconnectReason.TransientDrop;
        }
    }
}
