using System.Reflection;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.DiscoBallSound
{
    internal static class DiscoBallSoundPlayer
    {
        private static readonly FieldInfo? PartyAudioTransformField =
            AccessTools.Field(typeof(PartyButtonLevelObject), "partyAudioTransform");

        private static readonly Dictionary<int, ActiveLoop> LoopsByInstanceId = new();

        private sealed class ActiveLoop
        {
            internal ActiveLoop(PartyButtonLevelObject button, GameObject root, AudioSource audioSource)
            {
                Button = button;
                Root = root;
                AudioSource = audioSource;
            }

            internal PartyButtonLevelObject Button { get; }
            internal GameObject Root { get; }
            internal AudioSource AudioSource { get; }
        }

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
            ConfigureAudioSource(audioSource, clip);

            audioSource.Play();
            LoopsByInstanceId[button.GetInstanceID()] = new ActiveLoop(button, root, audioSource);

            ModLog.Info(
                DiscoBallSoundConstants.Feature,
                $"Disco ball music replaced — mode={DiscoBallSoundResolver.GetMode()}, variant={fileName}");
            return true;
        }

        internal static void StopLoop(PartyButtonLevelObject button)
        {
            int instanceId = button.GetInstanceID();
            if (!LoopsByInstanceId.TryGetValue(instanceId, out ActiveLoop? active))
            {
                return;
            }

            DestroyLoop(active);
            LoopsByInstanceId.Remove(instanceId);
        }

        internal static void StopAll()
        {
            List<ActiveLoop> activeLoops = [.. LoopsByInstanceId.Values];
            for (int i = 0; i < activeLoops.Count; i++)
            {
                DestroyLoop(activeLoops[i]);
            }

            LoopsByInstanceId.Clear();
        }

        internal static void Prune()
        {
            List<int> stale = [];
            foreach (KeyValuePair<int, ActiveLoop> pair in LoopsByInstanceId)
            {
                if (pair.Value.Button == null || pair.Value.Root == null)
                {
                    stale.Add(pair.Key);
                }
            }

            for (int i = 0; i < stale.Count; i++)
            {
                LoopsByInstanceId.Remove(stale[i]);
            }
        }

        internal static void ApplyVolumeToActiveLoops()
        {
            float volume = DiscoBallSoundResolver.GetVolumeScale();
            foreach (ActiveLoop active in LoopsByInstanceId.Values)
            {
                if (active.AudioSource != null)
                {
                    active.AudioSource.volume = volume;
                }
            }
        }

        internal static IReadOnlyList<PartyButtonLevelObject> GetActiveButtons()
        {
            List<PartyButtonLevelObject> buttons = [];
            foreach (ActiveLoop active in LoopsByInstanceId.Values)
            {
                if (active.Button != null)
                {
                    buttons.Add(active.Button);
                }
            }

            return buttons;
        }

        private static Transform ResolveParentTransform(PartyButtonLevelObject button)
        {
            Transform? partyTransform = PartyAudioTransformField?.GetValue(button) as Transform;
            return partyTransform != null ? partyTransform : button.transform;
        }

        private static void ConfigureAudioSource(AudioSource audioSource, AudioClip clip)
        {
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = DiscoBallSoundConstants.SpatialBlend;
            audioSource.minDistance = DiscoBallSoundConstants.MinDistance;
            audioSource.maxDistance = DiscoBallSoundConstants.MaxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.dopplerLevel = DiscoBallSoundConstants.DopplerLevel;
            audioSource.spread = DiscoBallSoundConstants.Spread;
            audioSource.volume = DiscoBallSoundResolver.GetVolumeScale();
            audioSource.outputAudioMixerGroup = GameAudioMixerAccess.TryResolveSfxGroup();
        }

        private static void DestroyLoop(ActiveLoop active)
        {
            if (active.AudioSource != null)
            {
                active.AudioSource.Stop();
            }

            if (active.Root != null)
            {
                UnityEngine.Object.Destroy(active.Root);
            }
        }
    }
}
