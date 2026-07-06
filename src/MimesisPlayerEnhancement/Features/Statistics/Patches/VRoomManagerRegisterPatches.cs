namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.OnRegistPlayer))]
    public static class VRoomManagerRegisterPatches
    {
        [HarmonyPostfix]
        public static void Postfix(ulong steamID)
        {
            StatisticsPatchGuard.Run(nameof(VRoomManager.OnRegistPlayer), () =>
            {
                if (steamID == 0)
                {
                    return;
                }

                if (JoinAnytime.JoinAnytimePlayerRegistration.ShouldDeferRegistrationBySteamId(steamID))
                {
                    return;
                }

                int slotId = GameSessionAccess.GetSaveSlotId();
                if (!MimesisSaveManager.IsValidSaveSlotId(slotId)
                    && !StatisticsTracker.TryGetLoadedSlotId(out slotId))
                {
                    return;
                }

                StatisticsTracker.OnPlayerRegistered(steamID, slotId);
            });
        }
    }
}
