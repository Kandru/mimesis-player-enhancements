using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound
{
    internal static class RoundStartSoundClipCache
    {
        private static readonly EmbeddedAudioClipCache Cache = new(
            RoundStartSoundConstants.AssetFolder,
            RoundStartSoundConstants.Feature,
            "RoundStartSound");

        internal static bool HasCachedClips => Cache.HasCachedClips;

        internal static AudioClip? TryGetCachedClip(string fileName) => Cache.TryGetCachedClip(fileName);

        internal static bool TryPreloadClip(string fileName) => Cache.TryPreloadClip(fileName);

        internal static void Clear() => Cache.Clear();
    }
}
