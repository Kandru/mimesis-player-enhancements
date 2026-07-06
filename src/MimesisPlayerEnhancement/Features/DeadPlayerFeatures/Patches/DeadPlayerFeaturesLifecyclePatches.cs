using System;
namespace MimesisPlayerEnhancement.Features.DeadPlayerFeatures.Patches
{
    [HarmonyPatch(typeof(CameraManager), nameof(CameraManager.OnEnterDungeon))]
    internal static class CameraManagerOnEnterDungeonPostfix
    {
        private const string Feature = "DeadPlayerFeatures";

        [HarmonyPostfix]
        internal static void Postfix()
        {
            try
            {
                DeadPlayerFeaturesRuntime.OnDungeonEnter();
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
        private const string Feature = "DeadPlayerFeatures";

        [HarmonyPrefix]
        internal static void Prefix()
        {
            try
            {
                DeadPlayerFeaturesRuntime.OnDungeonEnd();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"OnEndDungeon prefix failed — {ex.Message}");
            }
        }
    }
}
