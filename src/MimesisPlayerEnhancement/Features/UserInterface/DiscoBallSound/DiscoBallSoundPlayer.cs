using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.DiscoBallSound
{
    internal static class DiscoBallSoundPlayer
    {
        private static readonly FieldInfo? PartyAudioTransformField =
            AccessTools.Field(typeof(PartyButtonLevelObject), "partyAudioTransform");

        private static readonly Dictionary<int, AudioSource> SourcesByInstanceId = new();

        internal static bool TryStartLoop(PartyButtonLevelObject button)
        {
            string? fileName = DiscoBallSoundSession.ResolveVariantFileName();
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            AudioClip? clip = DiscoBallSoundRuntime.TryGetCachedClip(fileName);
            if (clip == null)
            {
                ModLog.Warn(
                    DiscoBallSoundConstants.Feature,
                    $"Disco ball sound replacement skipped — clip not preloaded ({fileName})");
                return false;
            }

            StopLoop(button);

            Transform parent = ResolveParentTransform(button);
            GameObject root = new(DiscoBallSoundConstants.SourceObjectName);
            root.transform.SetParent(parent, worldPositionStays: false);
            root.transform.localPosition = Vector3.zero;

            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = DiscoBallSoundConstants.SpatialBlend;
            audioSource.minDistance = DiscoBallSoundConstants.MinDistance;
            audioSource.maxDistance = DiscoBallSoundConstants.MaxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.volume = DiscoBallSoundResolver.GetVolumeScale();

            audioSource.Play();
            SourcesByInstanceId[button.GetInstanceID()] = audioSource;

            ModLog.Info(
                DiscoBallSoundConstants.Feature,
                $"Disco ball music replaced — mode={DiscoBallSoundResolver.GetMode()}, variant={fileName}");
            return true;
        }

        internal static void StopLoop(PartyButtonLevelObject button)
        {
            int instanceId = button.GetInstanceID();
            if (!SourcesByInstanceId.TryGetValue(instanceId, out AudioSource? source))
            {
                return;
            }

            DestroySource(source);
            SourcesByInstanceId.Remove(instanceId);
        }

        internal static void StopAll()
        {
            foreach (AudioSource source in SourcesByInstanceId.Values)
            {
                DestroySource(source);
            }

            SourcesByInstanceId.Clear();
        }

        internal static void ApplyVolumeToActiveLoops()
        {
            float volume = DiscoBallSoundResolver.GetVolumeScale();
            foreach (AudioSource source in SourcesByInstanceId.Values)
            {
                if (source != null)
                {
                    source.volume = volume;
                }
            }
        }

        private static Transform ResolveParentTransform(PartyButtonLevelObject button)
        {
            Transform? partyTransform = PartyAudioTransformField?.GetValue(button) as Transform;
            return partyTransform != null ? partyTransform : button.transform;
        }

        private static void DestroySource(AudioSource? source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            // Detach before clip cache Clear() — Destroy(GameObject) alone is end-of-frame deferred.
            source.clip = null;
            UnityEngine.Object.DestroyImmediate(source.gameObject);
        }
    }
}
