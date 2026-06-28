namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardGameState
    {
        internal static bool IsInSession()
        {
            Hub.PersistentData? pdata = JoinAnytimeHub.GetPdata();
            return pdata != null && pdata.SessionJoined && pdata.main is InTramWaitingScene
                or GamePlayScene
                or MaintenanceScene
                or DeathMatchScene;
        }

        internal static bool IsHost()
        {
            return MimesisSaveManager.IsHost();
        }

        internal static int GetSaveSlotId()
        {
            return MimesisSaveManager.TryGetActiveSaveSlotId(out int slotId) ? slotId : -1;
        }
    }
}
