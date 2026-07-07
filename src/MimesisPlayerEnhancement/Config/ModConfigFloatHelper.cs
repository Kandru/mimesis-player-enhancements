using System.Globalization;
using System.IO;
using MelonLoader;

namespace MimesisPlayerEnhancement
{
    internal static class ModConfigFloatHelper
    {
        internal const int DecimalPlaces = 2;

        private static bool _normalizingFile;

        internal static float Round(float value)
        {
            return (float)Math.Round(value, DecimalPlaces, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// At least one decimal place (<c>1</c> → <c>1.0</c>), up to two when needed (<c>1.22222</c> → <c>1.22</c>).
        /// </summary>
        internal static string Format(float value)
        {
            return Round(value).ToString("0.0#", CultureInfo.InvariantCulture);
        }

        internal static void SanitizeEntry(MelonPreferences_Entry<float> entry)
        {
            float rounded = Round(entry.Value);
            if (!entry.Value.Equals(rounded))
            {
                entry.Value = rounded;
            }
        }

        internal static void SanitizeAll(IReadOnlyList<MelonPreferences_Entry<float>> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                SanitizeEntry(entries[i]);
            }
        }

        internal static void NormalizeSavedFloats(string filePath, IReadOnlyList<MelonPreferences_Entry<float>> entries)
        {
            if (_normalizingFile || !File.Exists(filePath))
            {
                return;
            }

            HashSet<string> floatKeys = new(StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                _ = floatKeys.Add(entries[i].Identifier);
            }

            string[] lines = File.ReadAllLines(filePath);
            bool changed = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (!TryNormalizeLine(lines[i], floatKeys, out string normalized))
                {
                    continue;
                }

                if (normalized == lines[i])
                {
                    continue;
                }

                lines[i] = normalized;
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            _normalizingFile = true;
            try
            {
                File.WriteAllLines(filePath, lines);
            }
            finally
            {
                _normalizingFile = false;
            }
        }

        private static bool TryNormalizeLine(string line, HashSet<string> floatKeys, out string normalized)
        {
            normalized = line;
            int eq = line.IndexOf('=');
            if (eq < 0)
            {
                return false;
            }

            string key = line[..eq].Trim();
            if (!floatKeys.Contains(key))
            {
                return false;
            }

            string remainder = line[(eq + 1)..];
            string valuePart = remainder;
            string trailing = "";

            int commentIdx = remainder.IndexOf('#');
            if (commentIdx >= 0)
            {
                valuePart = remainder[..commentIdx];
                trailing = remainder[commentIdx..];
            }

            valuePart = valuePart.Trim();
            if (!float.TryParse(valuePart, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                return false;
            }

            string formatted = Format(parsed);
            if (valuePart == formatted)
            {
                return false;
            }

            normalized = key + " = " + formatted + trailing;
            return true;
        }
    }
}
