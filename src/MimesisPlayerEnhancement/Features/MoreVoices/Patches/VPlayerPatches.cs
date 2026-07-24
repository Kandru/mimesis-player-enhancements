namespace MimesisPlayerEnhancement.Features.MoreVoices.Patches
{
    // game@0.3.1 Assembly-CSharp/VPlayer.cs:L117-154
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
