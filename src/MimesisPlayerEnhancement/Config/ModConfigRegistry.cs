using System;
using System.Collections.Generic;
using System.Globalization;
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

        internal static int Version { get; private set; }

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
            if (!ModConfig.IsInitialized)
            {
                return;
            }

            ModConfig.MainCategory.SaveToFile(false);
        }

        internal static void NotifyRuntimeChange()
        {
            Version++;
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
