using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.Statistics.Patches
{
    // game@0.3.2 Assembly-CSharp/VRoomManager.cs:L705-727
    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.TerminateSession))]
    internal static class VRoomManagerTerminateSessionPatches
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            StatisticsPatchGuard.Run(nameof(VRoomManager.TerminateSession), () =>
            {
                GameSessionInfo? session = GameSessionAccess.TryGetGameSessionInfo();
                if (session != null && session.StageCount <= 1)
                {
                    StatisticsRunTracker.OnRunRestart();
                }
            });
        }
    }

    // game@0.3.2 Assembly-CSharp/VRoomManager.cs:L894-905
    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.OnRegistPlayer))]
    public static class VRoomManagerRegisterPatches
    {
        private const string Feature = "Statistics";

        [HarmonyPostfix]
        public static void Postfix(ulong steamID, MsgErrorCode __result)
        {
            try
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
                    && !PlayerRegistry.TryGetLoadedSlotId(out slotId))
                {
                    return;
                }

                PlayerPresenceEvents.OnPlayerRegistered(steamID, slotId);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"{nameof(VRoomManager.OnRegistPlayer)} failed — {ex.Message}");
            }
        }
    }
}
