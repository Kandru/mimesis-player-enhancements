namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound.Patches
{
    [HarmonyPatch(typeof(Mimic.Audio.AudioManager), nameof(Mimic.Audio.AudioManager.PlaySfx), [typeof(string)])]
    internal static class AudioManagerPlaySfxPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(string? sfxId)
        {
            if (!RoundStartSoundGate.ShouldReplaceSfx(sfxId))
            {
                return true;
            }

            return !RoundStartSoundPlayer.TryPlayReplacement();
        }
    }

    [HarmonyPatch(typeof(Mimic.Audio.AudioManager), nameof(Mimic.Audio.AudioManager.PlaySfxTransform), [typeof(string), typeof(UnityEngine.Transform)])]
    internal static class AudioManagerPlaySfxTransformPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(string? sfxId)
        {
            if (!RoundStartSoundGate.ShouldReplaceSfx(sfxId))
            {
                return true;
            }

            return !RoundStartSoundPlayer.TryPlayReplacement();
        }
    }
}
