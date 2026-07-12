using System.IO;
using System.Reflection;

namespace MimesisPlayerEnhancement.Util
{
    /// <summary>
    /// Reads files embedded from the project <c>Assets/</c> tree at build time.
    /// Resource names follow <c>Assets.{feature}.{relative.path.with.dots}</c>.
    /// </summary>
    internal static class EmbeddedAssets
    {
        private const string RootPrefix = "Assets.";
        private static readonly Assembly Assembly = typeof(EmbeddedAssets).Assembly;

        internal static bool TryReadFeature(string featureFolder, string relativePath, out byte[] bytes, out string extension)
        {
            bytes = Array.Empty<byte>();
            extension = "";

            string? resourceName = ToResourceName(featureFolder, relativePath);
            if (resourceName == null)
            {
                return false;
            }

            try
            {
                using Stream? stream = Assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    return false;
                }

                using MemoryStream buffer = new MemoryStream();
                stream.CopyTo(buffer);
                bytes = buffer.ToArray();
                extension = Path.GetExtension(resourceName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static IReadOnlyList<string> ListFeatureFiles(string featureFolder)
        {
            if (string.IsNullOrWhiteSpace(featureFolder))
            {
                return Array.Empty<string>();
            }

            string featurePrefix = featureFolder.Trim('/').Replace('/', '.').Replace('\\', '.');
            string prefix = RootPrefix + featurePrefix + ".";
            List<string> files = [];
            foreach (string resourceName in Assembly.GetManifestResourceNames())
            {
                if (!resourceName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                string fileName = resourceName[prefix.Length..];
                if (!string.IsNullOrEmpty(fileName))
                {
                    files.Add(fileName);
                }
            }

            return files;
        }

        private static string? ToResourceName(string featureFolder, string relativePath)
        {
            if (string.IsNullOrWhiteSpace(featureFolder))
            {
                return null;
            }

            string relative = relativePath.TrimStart('/').Replace('\\', '/');
            if (string.IsNullOrEmpty(relative) || relative.Contains("..", StringComparison.Ordinal))
            {
                return null;
            }

            string featurePrefix = featureFolder.Trim('/').Replace('/', '.').Replace('\\', '.');
            string dottedPath = relative.Replace('/', '.');
            return RootPrefix + featurePrefix + "." + dottedPath;
        }
    }
}
