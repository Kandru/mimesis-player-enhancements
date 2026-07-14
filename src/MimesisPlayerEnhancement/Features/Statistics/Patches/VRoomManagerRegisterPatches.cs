using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.OnRegistPlayer))]
    public static class VRoomManagerRegisterPatches
    {
        [HarmonyPostfix]
        public static void Postfix(ulong steamID, MsgErrorCode __result)
        {
            StatisticsPatchGuard.Run(nameof(VRoomManager.OnRegistPlayer), () =>
            {
                if (__result != MsgErrorCode.Success || steamID == 0)
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
