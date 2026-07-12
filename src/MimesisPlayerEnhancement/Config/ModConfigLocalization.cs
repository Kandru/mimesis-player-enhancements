using MelonLoader;

namespace MimesisPlayerEnhancement
{
    /// <summary>
    /// Applies localized MelonPreferences display names and descriptions from <see cref="ModL10n"/>.
    /// </summary>
    internal static class ModConfigLocalization
    {
        private static string _appliedLocale = "";

        internal static void Apply()
        {
            Apply(GameLocaleAccess.GetCurrentLanguage());
        }

        internal static void Apply(string locale)
        {
            if (!ModConfig.IsInitialized)
            {
                return;
            }

            locale = GameLocaleAccess.NormalizeLanguageCode(locale);
            foreach (string sectionId in ModConfigRegistry.GetSectionOrder())
            {
                string sectionTitle = ModL10n.GetConfigSectionTitle(sectionId, locale) ?? sectionId;
                ModConfigRegistry.SetSectionTitle(sectionId, sectionTitle);

                MelonPreferences_Category? category = null;
                foreach (string key in ModConfigRegistry.GetEntryOrder(sectionId))
                {
                    if (!ModConfigRegistry.TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry)
                        || entry == null)
                    {
                        continue;
                    }

                    category ??= entry.Category;
                    entry.DisplayName = ModL10n.GetConfigEntryTitle(sectionId, key, locale) ?? key;
                    entry.Description = ModL10n.GetConfigEntryDescription(sectionId, key, locale) ?? string.Empty;
                }

                if (category != null)
                {
                    category.DisplayName = sectionTitle;
                }
            }

            _appliedLocale = locale;
        }

        internal static void RefreshIfLanguageChanged()
        {
            string current = GameLocaleAccess.GetCurrentLanguage();
            if (string.Equals(current, _appliedLocale, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Apply(current);
            ModConfig.SaveToFile();
        }
    }
}
