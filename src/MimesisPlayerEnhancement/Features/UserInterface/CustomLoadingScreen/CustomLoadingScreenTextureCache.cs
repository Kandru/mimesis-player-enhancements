using UnityEngine;

namespace MimesisPlayerEnhancement.Features.UserInterface.CustomLoadingScreen
{
    internal static class CustomLoadingScreenTextureCache
    {
        private static readonly Dictionary<string, Texture2D> TexturesByRelativePath =
            new(StringComparer.OrdinalIgnoreCase);

        internal static Texture2D? TryGetTexture(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            if (TexturesByRelativePath.TryGetValue(relativePath, out Texture2D? cached) && cached != null)
            {
                return cached;
            }

            if (!EmbeddedAssets.TryReadFeature(
                    CustomLoadingScreenConstants.AssetFolder,
                    relativePath,
                    out byte[] bytes,
                    out _))
            {
                return null;
            }

            Texture2D texture = new(2, 2, TextureFormat.RGBA32, mipChain: false);
            if (!texture.LoadImage(bytes))
            {
                UnityEngine.Object.Destroy(texture);
                ModLog.Warn(CustomLoadingScreenConstants.Feature,
                    $"Custom loading screen decode failed — {relativePath}");
                return null;
            }

            texture.name = relativePath;
            texture.wrapMode = TextureWrapMode.Clamp;
            TexturesByRelativePath[relativePath] = texture;
            return texture;
        }

        internal static void Clear()
        {
            foreach (Texture2D texture in TexturesByRelativePath.Values)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            TexturesByRelativePath.Clear();
        }
    }
}
