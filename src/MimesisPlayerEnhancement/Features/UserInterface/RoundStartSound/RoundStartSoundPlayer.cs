using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound
{
    internal static class RoundStartSoundPlayer
    {
        private static GameObject? _root;
        private static AudioSource? _audioSource;

        internal static bool TryPlayReplacement()
        {
            string? fileName = RoundStartSoundResolver.ResolveVariantFileName();
            if (string.IsNullOrWhiteSpace(fileName))
            {
                ModLog.Warn(RoundStartSoundConstants.Feature, "Dungeon landing sound replacement skipped — no embedded variants");
                return false;
            }

            AudioClip? clip = RoundStartSoundClipCache.TryGetClip(fileName);
            if (clip == null)
            {
                ModLog.Warn(RoundStartSoundConstants.Feature, $"Dungeon landing sound replacement skipped — could not load {fileName}");
                return false;
            }

            EnsureAudioSource();
            if (_audioSource == null)
            {
                return false;
            }

            _audioSource.PlayOneShot(clip);
            ModLog.Info(
                RoundStartSoundConstants.Feature,
                $"Dungeon landing sound replaced — mode={RoundStartSoundResolver.GetMode()}, variant={fileName}");
            return true;
        }

        internal static void Shutdown()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
                _audioSource = null;
            }
        }

        private static void EnsureAudioSource()
        {
            if (_audioSource != null)
            {
                return;
            }

            _root = new GameObject("MimesisPlayerEnhancement_RoundStartSound");
            UnityEngine.Object.DontDestroyOnLoad(_root);
            _audioSource = _root.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = 0f;
        }
    }
}
