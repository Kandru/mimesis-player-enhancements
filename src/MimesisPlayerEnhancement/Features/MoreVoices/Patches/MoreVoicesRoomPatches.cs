namespace MimesisPlayerEnhancement.Features.MoreVoices.Patches
{
    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.EnterWaitingRoom))]
    internal static class VRoomManagerEnterWaitingRoomRecordingPatch
    {
        private const string Feature = "MoreVoices";

        [HarmonyPostfix]
        public static void Postfix(SessionContext context)
        {
            if (!MoreVoicesRecording.IsFeatureActive()
                || context == null
                || !LocalPlayerHelper.IsLocalSteamId(context.SteamID))
            {
                return;
            }

            try
            {
                MoreVoicesRecording.ApplyRecordingState();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Hub voice recording apply on waiting room enter failed: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(VPlayer), nameof(VPlayer.HandleLevelLoadComplete))]
    internal static class VPlayerHandleLevelLoadCompleteRecordingPatch
    {
        private const string Feature = "MoreVoices";

        [HarmonyPostfix]
        public static void Postfix(VPlayer __instance)
        {
            if (!MoreVoicesRecording.IsFeatureActive()
                || __instance == null
                || !LocalPlayerHelper.IsLocalSteamId(__instance.SteamID))
            {
                return;
            }

            try
            {
                MoreVoicesRecording.ApplyRecordingState();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"Hub voice recording apply on level load failed: {ex.Message}");
            }
        }
    }
}
