using System;
using MimesisPlayerEnhancement.Config.QuickSettings;

namespace MimesisPlayerEnhancement
{
    internal static class QuickSettingsCatalog
    {
        internal static string GetDisplayName(string presetId)
        {
            if (string.IsNullOrWhiteSpace(presetId))
            {
                return ModL10n.Get("quicksettings.profile.custom");
            }

            string localeKey = GetLocaleKeyForPreset(presetId);
            if (!string.IsNullOrEmpty(localeKey))
            {
                string localized = ModL10n.Get(localeKey);
                if (!string.Equals(localized, localeKey, StringComparison.Ordinal))
                {
                    return localized;
                }
            }

            if (UserQuickSettingsStore.TryGet(presetId, out QuickSettingPreset? userPreset) && userPreset != null)
            {
                return string.IsNullOrWhiteSpace(userPreset.Name) ? presetId : userPreset.Name!;
            }

            return presetId;
        }

        internal static string? GetDescription(string presetId)
        {
            string? localeKey = GetLocaleDescriptionKeyForPreset(presetId);
            if (string.IsNullOrEmpty(localeKey))
            {
                return null;
            }

            string localized = ModL10n.Get(localeKey);
            return string.Equals(localized, localeKey, StringComparison.Ordinal) ? null : localized;
        }

        internal static bool TryResolvePreset(string presetId, out QuickSettingPreset preset)
        {
            preset = null!;
            if (string.IsNullOrWhiteSpace(presetId))
            {
                return false;
            }

            if (BuiltinQuickSettings.TryGet(presetId, out QuickSettingPreset builtin))
            {
                preset = builtin;
                return true;
            }

            if (UserQuickSettingsStore.TryGet(presetId, out QuickSettingPreset userPreset))
            {
                preset = userPreset;
                return true;
            }

            return false;
        }

        internal static List<QuickSettingPreset> ListAllPresets()
        {
            List<QuickSettingPreset> presets = [];
            foreach (QuickSettingPreset builtin in BuiltinQuickSettings.GetAll())
            {
                presets.Add(builtin);
            }

            foreach (QuickSettingPreset userPreset in UserQuickSettingsStore.GetAll())
            {
                presets.Add(userPreset);
            }

            return presets;
        }

        internal static bool IsBuiltin(string presetId) => BuiltinQuickSettings.IsBuiltin(presetId);

        private static string GetLocaleKeyForPreset(string presetId)
        {
            return presetId.ToLowerInvariant() switch
            {
                BuiltinQuickSettings.LootPicnicId => "quicksettings.presets.loot_picnic.name",
                BuiltinQuickSettings.AsIntendedId => "quicksettings.presets.as_intended.name",
                BuiltinQuickSettings.FullHouseId => "quicksettings.presets.full_house.name",
                BuiltinQuickSettings.AbandonHopeId => "quicksettings.presets.abandon_hope.name",
                _ => "",
            };
        }

        private static string? GetLocaleDescriptionKeyForPreset(string presetId)
        {
            return presetId.ToLowerInvariant() switch
            {
                BuiltinQuickSettings.LootPicnicId => "quicksettings.presets.loot_picnic.description",
                BuiltinQuickSettings.AsIntendedId => "quicksettings.presets.as_intended.description",
                BuiltinQuickSettings.FullHouseId => "quicksettings.presets.full_house.description",
                BuiltinQuickSettings.AbandonHopeId => "quicksettings.presets.abandon_hope.description",
                _ => null,
            };
        }
    }
}
