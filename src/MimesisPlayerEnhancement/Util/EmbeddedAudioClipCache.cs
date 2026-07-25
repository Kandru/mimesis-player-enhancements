using System.IO;
using MelonLoader.Utils;
using UnityEngine;

namespace MimesisPlayerEnhancement.Util
{
    internal sealed class EmbeddedAudioClipCache
    {
        private readonly string _assetFolder;
        private readonly string _featureTag;
        private readonly string _tempSubfolder;
        private readonly Dictionary<string, AudioClip> _clipsByFileName = new(StringComparer.OrdinalIgnoreCase);

        internal EmbeddedAudioClipCache(string assetFolder, string featureTag, string tempSubfolder)
        {
            _assetFolder = assetFolder;
            _featureTag = featureTag;
            _tempSubfolder = tempSubfolder;
        }

        internal bool HasCachedClips => _clipsByFileName.Count > 0;

        internal AudioClip? TryGetCachedClip(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            return _clipsByFileName.TryGetValue(fileName, out AudioClip? cached) && cached != null
                ? cached
                : null;
        }

        internal bool TryPreloadClip(string fileName)
        {
            return TryGetClip(fileName) != null;
        }

        internal AudioClip? TryGetClip(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }

            if (_clipsByFileName.TryGetValue(fileName, out AudioClip? cached) && cached != null)
            {
                return cached;
            }

            if (!EmbeddedAssets.TryReadFeature(_assetFolder, fileName, out byte[] bytes, out string extension))
            {
                return null;
            }

            AudioClip? clip = DecodeClip(bytes, extension, fileName);
            if (clip != null)
            {
                _clipsByFileName[fileName] = clip;
            }

            return clip;
        }

        internal void Clear()
        {
            foreach (AudioClip clip in _clipsByFileName.Values)
            {
                if (clip != null)
                {
                    UnityEngine.Object.Destroy(clip);
                }
            }

            _clipsByFileName.Clear();
        }

        private AudioClip? DecodeClip(byte[] bytes, string extension, string fileName)
        {
            string safeExtension = string.IsNullOrWhiteSpace(extension) ? ".mp3" : extension;
            if (!safeExtension.StartsWith(".", StringComparison.Ordinal))
            {
                safeExtension = "." + safeExtension;
            }

            string tempPath = Path.Combine(
                MelonEnvironment.UserDataDirectory,
                _tempSubfolder,
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
                ModLog.Warn(_featureTag, $"Embedded audio clip decode failed — {fileName}, {ex.Message}");
                return null;
            }
            finally
            {
                TryDelete(tempPath);
            }
        }

        private AudioClip? LoadClipFromFile(string filePath, string extension, string clipName)
        {
            AudioType audioType = ResolveAudioType(extension);
            using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, audioType);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                // Preload-only path: runs during config refresh or scene start, not on audio trigger.
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                ModLog.Warn(_featureTag, $"Embedded audio clip request failed — {clipName}, {request.error}");
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
