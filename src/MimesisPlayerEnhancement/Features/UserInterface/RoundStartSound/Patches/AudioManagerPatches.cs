namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound.Patches
{
    // game@0.3.1 Assembly-CSharp/Mimic.Audio/AudioManager.cs:L189-197
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

    // game@0.3.1 Assembly-CSharp/Mimic.Audio/AudioManager.cs:L209-218
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
