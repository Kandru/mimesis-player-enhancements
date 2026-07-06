using System;
namespace MimesisPlayerEnhancement.Util
{
    internal static class ModLocaleAssets
    {
        private const string LocaleFolder = "Locale";

        internal static bool TryReadLocaleJson(string locale, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            locale = GameLocaleAccess.NormalizeLanguageCode(locale);
            if (EmbeddedAssets.TryReadFeature(LocaleFolder, $"{locale}.json", out bytes, out _))
            {
                return true;
            }

            return locale != "en"
                   && EmbeddedAssets.TryReadFeature(LocaleFolder, "en.json", out bytes, out _);
        }
    }
}
