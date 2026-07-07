namespace MimesisPlayerEnhancement.Features.MimicTuning.Patches
{
    [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.OnEnterDungeon))]
    internal static class CameraManagerOnEnterDungeonPostfix
    {
        private const string Feature = "MimicTuning";

        [HarmonyPostfix]
        internal static void Postfix()
        {
            try
            {
                MimicTuningRuntime.OnDungeonEnter();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnEnterDungeon postfix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.OnEndDungeon))]
    internal static class CameraManagerOnEndDungeonPrefix
    {
        private const string Feature = "MimicTuning";

        [HarmonyPrefix]
        internal static void Prefix()
        {
            try
            {
                MimicTuningRuntime.OnDungeonEnd();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnEndDungeon prefix failed — {ex.Message}");
            }
        }
    }
}
