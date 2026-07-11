using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardHostCheatsNoClipSync
    {
        internal const int EnableHashCode = unchecked((int)0x4E4F434C);
        internal const int DisableHashCode = unchecked((int)0x4E4F4344);

        internal static void SendToPlayer(VPlayer player, bool enabled)
        {
            if (player == null)
            {
                return;
            }

            try
            {
                int hashCode = enabled ? EnableHashCode : DisableHashCode;
                player.SendToMe(new AdminCommandRes(hashCode)
                {
                    errorCode = MsgErrorCode.Success,
                });
            }
            catch (Exception ex)
            {
                ModLog.Warn("WebDashboard", $"Noclip sync failed — uid={player.UID}, {ex.Message}");
            }
        }

        internal static bool TryHandleAdminCommandRes(AdminCommandRes? response)
        {
            if (response == null)
            {
                return false;
            }

            if (response.hashCode == EnableHashCode)
            {
                WebDashboardHostCheatsClientRuntime.SetLocalNoClip(enabled: true);
                return true;
            }

            if (response.hashCode == DisableHashCode)
            {
                WebDashboardHostCheatsClientRuntime.SetLocalNoClip(enabled: false);
                return true;
            }

            return false;
        }
    }
}
