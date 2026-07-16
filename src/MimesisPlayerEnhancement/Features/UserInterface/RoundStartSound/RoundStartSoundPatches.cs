namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound
{
    internal static class RoundStartSoundPatches
    {
        private const string Feature = RoundStartSoundConstants.Feature;

        internal static void Apply(HarmonyLib.Harmony harmony)
        {
            IEnumerable<Type> patchTypes = HarmonyPatchHelper.GetNamespacePatchTypes(typeof(RoundStartSoundPatches));
            HarmonyPatchHelper.PatchApplyResult result = HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                patchTypes);

            LogPatchAudit(harmony);
            HarmonyPatchHelper.LogPatchSummary(Feature, result);
        }

        private static void LogPatchAudit(HarmonyLib.Harmony harmony)
        {
            HarmonyPatchHelper.LogPatchAudit(Feature, harmony,
            [
                ("PlaySfx/AudioManager (dungeon landing melody)", AccessTools.Method(typeof(Mimic.Audio.AudioManager), nameof(Mimic.Audio.AudioManager.PlaySfx), [typeof(string)])),
                ("PlaySfxTransform/AudioManager (dungeon landing melody)", AccessTools.Method(typeof(Mimic.Audio.AudioManager), nameof(Mimic.Audio.AudioManager.PlaySfxTransform), [typeof(string), typeof(UnityEngine.Transform)])),
                ("Start/AudioPlayer (dungeon landing melody)", AccessTools.Method(typeof(AudioPlayer), "Start")),
                ("Start/GamePlayScene (entry tracker)", AccessTools.Method(typeof(GamePlayScene), "Start")),
                ("InvokeTimingCallback/ModHelper (EnterGame)", AccessTools.Method(typeof(ModUtility.ModHelper), nameof(ModUtility.ModHelper.InvokeTimingCallback))),
                ("OnDestroy/GameMainBase (GamePlayScene cleanup)", AccessTools.Method(typeof(GameMainBase), "OnDestroy")),
            ]);
        }
    }
}
