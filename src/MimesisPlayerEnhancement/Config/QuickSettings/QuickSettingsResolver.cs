using MelonLoader;
using MimesisPlayerEnhancement.Config.QuickSettings;

namespace MimesisPlayerEnhancement
{
    internal static class QuickSettingsResolver
    {
        private const string Feature = "QuickSettings";

        internal static bool TryApplyPreset(string presetId, out string? error)
        {
            error = null;
            if (!QuickSettingsCatalog.TryResolvePreset(presetId, out QuickSettingPreset preset))
            {
                error = ModL10n.Get("api.quick_preset_not_found");
                return false;
            }

            ApplyPresetValues(preset.Values);
            ModLog.Info(Feature, $"Applied quick preset — id={presetId}, revision={preset.Revision}");
            return true;
        }

        internal static void ResetSaveOverrideableToDefaults()
        {
            if (!ModConfig.IsInitialized)
            {
                return;
            }

            foreach ((string sectionId, string key) in ModConfigRegistry.EnumerateSaveOverrideableKeys())
            {
                if (!ModConfigRegistry.TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null)
                {
                    continue;
                }

                _ = ModConfigRegistry.TrySetEntryValue(
                    sectionId,
                    key,
                    ModConfigRegistry.FormatEntryDefaultValue(entry),
                    out _);
            }
        }

        internal static void ApplyPresetValues(Dictionary<string, Dictionary<string, string>> values)
        {
            if (ModConfig.IsInitialized)
            {
                // Refresh global-only settings (UI, dashboard, debug) from disk, then replace
                // save-overrideable gameplay keys with preset values instead of global gameplay.
                ModConfig.ReloadGlobalFromFile();
            }

            ResetSaveOverrideableToDefaults();
            ApplyValues(values);
        }

        internal static void ApplyValues(Dictionary<string, Dictionary<string, string>> values)
        {
            foreach (KeyValuePair<string, Dictionary<string, string>> section in values)
            {
                foreach (KeyValuePair<string, string> pair in section.Value)
                {
                    if (!ModConfigRegistry.IsSaveOverrideAllowed(section.Key, pair.Key))
                    {
                        continue;
                    }

                    if (!ModConfigRegistry.TrySetEntryValue(section.Key, pair.Key, pair.Value, out string? error))
                    {
                        ModLog.Warn(Feature, $"Skipped preset value {section.Key}/{pair.Key} — {error}");
                    }
                }
            }
        }
    }
}
