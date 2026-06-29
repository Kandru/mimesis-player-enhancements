namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardGameState
    {
        internal static bool IsConnected()
        {
            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            return pdata != null && pdata.SessionJoined;
        }

        internal static bool IsHost()
        {
            return JoinAnytimeHub.GetPdata()?.ClientMode == ReluNetwork.ConstEnum.NetworkClientMode.Host;
        }

        internal static int GetSaveSlotId()
        {
            if (!IsHost())
            {
                return -1;
            }

            return MimesisSaveManager.TryGetActiveSaveSlotId(out int slotId) ? slotId : -1;
        }
    }
}
