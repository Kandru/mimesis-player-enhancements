using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound
{
    internal static class RoundStartSoundPlayer
    {
        private static AudioSource? _audioSource;

        internal static bool TryPlayReplacement()
        {
            string? fileName = RoundStartSoundResolver.ResolveVariantFileName();
            if (string.IsNullOrWhiteSpace(fileName))
            {
                ModLog.Warn(RoundStartSoundConstants.Feature, "Dungeon landing sound replacement skipped — no embedded variants");
                return false;
            }

            AudioClip? clip = RoundStartSoundRuntime.TryGetCachedClip(fileName);
            if (clip == null)
            {
                ModLog.Warn(
                    RoundStartSoundConstants.Feature,
                    $"Dungeon landing sound replacement skipped — clip not preloaded ({fileName})");
                return false;
            }

            AudioSource? source = EnsureAudioSource();
            if (source == null)
            {
                return false;
            }

            source.PlayOneShot(clip, RoundStartSoundResolver.GetVolumeScale());
            ModLog.Info(
                RoundStartSoundConstants.Feature,
                $"Dungeon landing sound replaced — mode={RoundStartSoundResolver.GetMode()}, variant={fileName}");
            return true;
        }

        private static AudioSource? EnsureAudioSource()
        {
            if (_audioSource != null)
            {
                return _audioSource;
            }

            GameObject root = new(RoundStartSoundConstants.SourceObjectName);
            UnityEngine.Object.DontDestroyOnLoad(root);
            _audioSource = root.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = 0f;
            return _audioSource;
        }
    }
}
