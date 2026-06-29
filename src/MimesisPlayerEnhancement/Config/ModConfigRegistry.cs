using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using MelonLoader;

namespace MimesisPlayerEnhancement
{
    /// <summary>
    /// Lookup and runtime updates for MelonPreferences entries exposed by <see cref="ModConfig"/>.
    /// </summary>
    internal static class ModConfigRegistry
    {
        private static readonly Dictionary<string, Dictionary<string, MelonPreferences_Entry>> EntriesBySection =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly List<string> SectionOrder = [];
        private static readonly Dictionary<string, List<string>> EntryOrderBySection =
            new(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> SectionTitles =
            new(StringComparer.Ordinal);

        internal static int Version { get; private set; }

        internal static IReadOnlyList<string> GetSectionOrder() => SectionOrder;

        internal static IReadOnlyList<string> GetEntryOrder(string sectionId)
        {
            return EntryOrderBySection.TryGetValue(sectionId, out List<string>? keys)
                ? keys
                : [];
        }

        internal static bool TryGetSectionTitle(string sectionId, out string title)
        {
            return SectionTitles.TryGetValue(sectionId, out title!);
        }

        internal static void ClearRegistrationOrder()
        {
            SectionOrder.Clear();
            EntryOrderBySection.Clear();
            SectionTitles.Clear();
        }

        internal static void TrackCategory(MelonPreferences_Category category)
        {
            string sectionId = category.Identifier;
            if (EntryOrderBySection.ContainsKey(sectionId))
            {
                return;
            }

            SectionOrder.Add(sectionId);
            EntryOrderBySection[sectionId] = [];
            SectionTitles[sectionId] = category.DisplayName ?? sectionId;
        }

        internal static void TrackEntry(MelonPreferences_Entry entry)
        {
            string sectionId = entry.Category.Identifier;
            if (!EntryOrderBySection.TryGetValue(sectionId, out List<string>? keys))
            {
                TrackCategory(entry.Category);
                keys = EntryOrderBySection[sectionId];
            }

            keys.Add(entry.Identifier);
        }

        internal static string FormatEntryValue(MelonPreferences_Entry entry)
        {
            Type? type = entry.GetReflectedType();
            if (type == typeof(float) && entry.BoxedValue is float floatValue)
            {
                return ModConfigFloatHelper.Format(floatValue);
            }

            if (type == typeof(bool) && entry.BoxedValue is bool boolValue)
            {
                return boolValue ? "true" : "false";
            }

            return SafeGetString(entry.GetValueAsString);
        }

        internal static string FormatEntryDefaultValue(MelonPreferences_Entry entry)
        {
            Type? type = entry.GetReflectedType();
            string raw = SafeGetString(entry.GetDefaultValueAsString);
            if (type == typeof(float)
                && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
            {
                return ModConfigFloatHelper.Format(floatValue);
            }

            if (type == typeof(bool)
                && bool.TryParse(raw, out bool boolValue))
            {
                return boolValue ? "true" : "false";
            }

            return raw;
        }

        private static string SafeGetString(Func<string> getter)
        {
            try
            {
                return getter() ?? "";
            }
            catch
            {
                return "";
            }
        }

        internal static void Rebuild()
        {
            EntriesBySection.Clear();

            foreach (PropertyInfo property in typeof(ModConfig).GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                Type propertyType = property.PropertyType;
                if (!propertyType.IsGenericType
                    || propertyType.GetGenericTypeDefinition() != typeof(MelonPreferences_Entry<>))
                {
                    continue;
                }

                object? raw = property.GetValue(null);
                if (raw is not MelonPreferences_Entry entry)
                {
                    continue;
                }

                Register(entry);
            }
        }

        internal static bool TryGetEntry(string sectionId, string key, out MelonPreferences_Entry? entry)
        {
            entry = null;
            return !string.IsNullOrWhiteSpace(sectionId) && !string.IsNullOrWhiteSpace(key) && EntriesBySection.TryGetValue(sectionId, out Dictionary<string, MelonPreferences_Entry>? keys)
                && keys.TryGetValue(key, out entry);
        }

        internal static bool TrySetEntryValue(string sectionId, string key, string rawValue, out string? error)
        {
            error = null;

            if (!ModConfig.IsInitialized)
            {
                error = "Configuration is not initialized.";
                return false;
            }

            if (!TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null)
            {
                error = "Unknown setting.";
                return false;
            }

            Type valueType = entry.GetReflectedType()
                ?? throw new InvalidOperationException($"Setting {sectionId}/{key} has no value type.");

            if (!TryParseValue(valueType, rawValue, out object? parsed, out error))
            {
                return false;
            }

            try
            {
                if (!TrySetBoxedValue(entry, parsed))
                {
                    error = "Failed to apply setting value.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            return true;
        }

        internal static void SaveToFile()
        {
            if (!ModConfig.IsInitialized || SaveSlotConfigStore.IsApplyingOverrides)
            {
                return;
            }

            ModConfig.MainCategory.SaveToFile(false);
        }

        internal static void NotifyRuntimeChange()
        {
            Version++;
        }

        internal static bool TryGetFeatureToggleKey(string sectionId, out string key)
        {
            key = "";

            if (string.Equals(sectionId, "MimesisPlayerEnhancement", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            foreach (string entryKey in GetEntryOrder(sectionId))
            {
                if (!TryGetEntry(sectionId, entryKey, out MelonPreferences_Entry? entry) || entry == null)
                {
                    continue;
                }

                if (entry.GetReflectedType() != typeof(bool))
                {
                    continue;
                }

                if (entryKey.StartsWith("Enable", StringComparison.Ordinal)
                    || string.Equals(entryKey, "ShowPlayerAnnouncements", StringComparison.Ordinal))
                {
                    key = entryKey;
                    return true;
                }
            }

            return false;
        }

        internal static bool IsSaveOverrideAllowed(string sectionId, string key)
        {
            if (string.IsNullOrWhiteSpace(sectionId) || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (string.Equals(sectionId, "MimesisPlayerEnhancement_WebDashboard", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.Equals(sectionId, "MimesisPlayerEnhancement_ExtendedSaveSlots", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return TryGetEntry(sectionId, key, out _);
        }

        internal static bool TryNormalizeRawValue(
            string sectionId,
            string key,
            string rawValue,
            out string normalized,
            out string? error)
        {
            normalized = "";
            error = null;

            if (!TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null)
            {
                error = "Unknown setting.";
                return false;
            }

            Type valueType = entry.GetReflectedType()
                ?? throw new InvalidOperationException($"Setting {sectionId}/{key} has no value type.");

            if (!TryParseValue(valueType, rawValue, out object? parsed, out error))
            {
                return false;
            }

            normalized = FormatParsedValue(entry, parsed);
            return true;
        }

        internal static bool RawValuesEqual(string sectionId, string key, string rawA, string rawB)
        {
            if (string.Equals(rawA, rawB, StringComparison.Ordinal))
            {
                return true;
            }

            if (!TryNormalizeRawValue(sectionId, key, rawA, out string normalizedA, out _)
                || !TryNormalizeRawValue(sectionId, key, rawB, out string normalizedB, out _))
            {
                return string.Equals(rawA?.Trim(), rawB?.Trim(), StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(normalizedA, normalizedB, StringComparison.Ordinal);
        }

        internal static bool TryGetGlobalRawValue(string sectionId, string key, out string globalRaw)
        {
            globalRaw = "";

            if (!TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(ModConfig.FilePath) || !File.Exists(ModConfig.FilePath))
            {
                globalRaw = FormatEntryDefaultValue(entry);
                return true;
            }

            SparseTomlConfig.Document doc = SparseTomlConfig.Load(
                File.ReadAllText(ModConfig.FilePath));

            if (doc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys)
                && keys.TryGetValue(key, out string? fromFile))
            {
                if (TryNormalizeRawValue(sectionId, key, fromFile, out string normalized, out _))
                {
                    globalRaw = normalized;
                    return true;
                }

                globalRaw = fromFile;
                return true;
            }

            globalRaw = FormatEntryDefaultValue(entry);
            return true;
        }

        private static string FormatParsedValue(MelonPreferences_Entry entry, object? parsed)
        {
            Type? type = entry.GetReflectedType();
            if (type == typeof(float) && parsed is float floatValue)
            {
                return ModConfigFloatHelper.Format(floatValue);
            }

            if (type == typeof(bool) && parsed is bool boolValue)
            {
                return boolValue ? "true" : "false";
            }

            return Convert.ToString(parsed, CultureInfo.InvariantCulture) ?? "";
        }

        private static bool TrySetBoxedValue(MelonPreferences_Entry entry, object? parsed)
        {
            PropertyInfo? valueProperty = entry.GetType().GetProperty("Value");
            if (valueProperty?.GetSetMethod() != null)
            {
                valueProperty.SetValue(entry, parsed);
                return true;
            }

            entry.BoxedValue = parsed;
            return true;
        }

        private static void Register(MelonPreferences_Entry entry)
        {
            string sectionId = entry.Category.Identifier;
            if (!EntriesBySection.TryGetValue(sectionId, out Dictionary<string, MelonPreferences_Entry>? keys))
            {
                keys = new Dictionary<string, MelonPreferences_Entry>(StringComparer.OrdinalIgnoreCase);
                EntriesBySection[sectionId] = keys;
            }

            keys[entry.Identifier] = entry;
        }

        private static bool TryParseValue(Type type, string rawValue, out object? value, out string? error)
        {
            value = null;
            error = null;
            rawValue = rawValue?.Trim() ?? "";

            if (type == typeof(bool))
            {
                if (bool.TryParse(rawValue, out bool boolValue))
                {
                    value = boolValue;
                    return true;
                }

                if (string.Equals(rawValue, "1", StringComparison.Ordinal)
                    || string.Equals(rawValue, "on", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(rawValue, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    value = true;
                    return true;
                }

                if (string.Equals(rawValue, "0", StringComparison.Ordinal)
                    || string.Equals(rawValue, "off", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(rawValue, "no", StringComparison.OrdinalIgnoreCase))
                {
                    value = false;
                    return true;
                }

                error = "Invalid boolean value.";
                return false;
            }

            if (type == typeof(int))
            {
                if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                {
                    value = intValue;
                    return true;
                }

                error = "Invalid integer value.";
                return false;
            }

            if (type == typeof(float))
            {
                if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                {
                    value = floatValue;
                    return true;
                }

                error = "Invalid number value.";
                return false;
            }

            if (type == typeof(string))
            {
                value = rawValue;
                return true;
            }

            error = $"Unsupported setting type: {type.Name}.";
            return false;
        }
    }
}
