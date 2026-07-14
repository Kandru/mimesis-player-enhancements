using System.Globalization;
using MelonLoader;

namespace MimesisPlayerEnhancement
{
    internal static class ModConfigFloatHelper
    {
        internal const int DecimalPlaces = 2;

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
            _ = filePath;
            _ = entries;
        }
    }
}
