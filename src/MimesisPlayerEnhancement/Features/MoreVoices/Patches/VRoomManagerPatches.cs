namespace MimesisPlayerEnhancement.Features.MoreVoices.Patches
{
    // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L372-409
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
}
