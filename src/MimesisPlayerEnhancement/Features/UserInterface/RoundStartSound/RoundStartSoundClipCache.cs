using System.IO;
using MelonLoader.Utils;
using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.RoundStartSound
{
    internal static class RoundStartSoundClipCache
    {
        private static readonly Dictionary<string, AudioClip> ClipsByFileName = new(StringComparer.OrdinalIgnoreCase);

        internal static AudioClip? TryGetClip(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            if (ClipsByFileName.TryGetValue(fileName, out AudioClip? cached) && cached != null)
            {
                return cached;
            }

            if (!EmbeddedAssets.TryReadFeature(RoundStartSoundConstants.AssetFolder, fileName, out byte[] bytes, out string extension))
            {
                return null;
            }

            AudioClip? clip = DecodeClip(bytes, extension, fileName);
            if (clip != null)
            {
                ClipsByFileName[fileName] = clip;
            }

            return clip;
        }

        internal static void Clear()
        {
            foreach (AudioClip clip in ClipsByFileName.Values)
            {
                if (clip != null)
                {
                    UnityEngine.Object.Destroy(clip);
                }
            }

            ClipsByFileName.Clear();
        }

        private static AudioClip? DecodeClip(byte[] bytes, string extension, string fileName)
        {
            string safeExtension = string.IsNullOrWhiteSpace(extension) ? ".mp3" : extension;
            if (!safeExtension.StartsWith(".", StringComparison.Ordinal))
            {
                safeExtension = "." + safeExtension;
            }

            string tempPath = Path.Combine(
                MelonEnvironment.UserDataDirectory,
                "RoundStartSound",
                $"decode-{Guid.NewGuid():N}{safeExtension}");

            try
            {
                string? directory = Path.GetDirectoryName(tempPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(tempPath, bytes);
                return LoadClipFromFile(tempPath, safeExtension, fileName);
            }
            catch (Exception ex)
            {
                ModLog.Warn(RoundStartSoundConstants.Feature, $"Dungeon landing clip decode failed — {fileName}, {ex.Message}");
                return null;
            }
            finally
            {
                TryDelete(tempPath);
            }
        }

        private static AudioClip? LoadClipFromFile(string filePath, string extension, string clipName)
        {
            AudioType audioType = ResolveAudioType(extension);
            using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, audioType);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                // UnityWebRequest on the main thread during gameplay; this path runs on audio trigger.
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                ModLog.Warn(RoundStartSoundConstants.Feature, $"Dungeon landing clip request failed — {clipName}, {request.error}");
                return null;
            }

            AudioClip? clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip != null)
            {
                clip.name = Path.GetFileNameWithoutExtension(clipName);
            }

            return clip;
        }

        private static AudioType ResolveAudioType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".wav" => AudioType.WAV,
                ".ogg" => AudioType.OGGVORBIS,
                _ => AudioType.MPEG,
            };
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best-effort temp cleanup.
            }
        }
    }
}
