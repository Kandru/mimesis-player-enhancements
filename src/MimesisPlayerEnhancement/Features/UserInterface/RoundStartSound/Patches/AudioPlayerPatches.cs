using System.Reflection;

namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound.Patches
{
    // game@0.3.1 Assembly-CSharp/AudioPlayer.cs:L11-17
    [HarmonyPatch(typeof(AudioPlayer), "Start")]
    internal static class AudioPlayerStartPatch
    {
        private static readonly FieldInfo? SfxIdField =
            AccessTools.Field(typeof(AudioPlayer), "sfxId");

        [HarmonyPrefix]
        private static bool Prefix(AudioPlayer __instance)
        {
            string? sfxId = SfxIdField?.GetValue(__instance) as string;
            if (!RoundStartSoundGate.ShouldReplaceSfx(sfxId))
            {
                return true;
            }

            return !RoundStartSoundPlayer.TryPlayReplacement();
        }
    }
}
