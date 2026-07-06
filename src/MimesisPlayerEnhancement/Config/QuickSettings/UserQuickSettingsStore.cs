using System;
using System.Globalization;
using System.Text.RegularExpressions;
using MimesisPlayerEnhancement.Config.QuickSettings;

namespace MimesisPlayerEnhancement
{
    internal static class UserQuickSettingsStore
    {
        private const string Feature = "UserQuickSettings";
        private const string HeaderSectionId = "MimesisPlayerEnhancement_QuickPresets";
        private const string SchemaVersionKey = "SchemaVersion";
        private const int CurrentSchemaVersion = 1;
        private const string UserPrefix = "user:";

        private static readonly Regex SlugPattern = new("^[a-z0-9][a-z0-9_-]{0,63}$", RegexOptions.Compiled);

        private static readonly Dictionary<string, QuickSettingPreset> Cache =
            new(StringComparer.OrdinalIgnoreCase);

        private static bool _loaded;
        private static bool _dirty;

        internal static IReadOnlyCollection<QuickSettingPreset> GetAll()
        {
            EnsureLoaded();
            return Cache.Values;
        }

        internal static bool TryGet(string presetId, out QuickSettingPreset preset)
        {
            EnsureLoaded();
            return Cache.TryGetValue(presetId, out preset!);
        }

        internal static bool TryCreateOrUpdate(
            string presetId,
            string name,
            Dictionary<string, Dictionary<string, string>> values,
            bool overwriteExisting,
            out QuickSettingPreset preset,
            out string? error)
        {
            preset = null!;
            error = null;

            if (BuiltinQuickSettings.IsBuiltin(presetId))
            {
                error = ModL10n.Get("api.quick_preset_builtin_readonly");
                return false;
            }

            if (!TryNormalizeUserPresetId(presetId, out string normalizedId, out error))
            {
                return false;
            }

            EnsureLoaded();
            bool exists = Cache.ContainsKey(normalizedId);
            if (exists && !overwriteExisting)
            {
                error = ModL10n.Get("api.quick_preset_exists");
                return false;
            }

            string utcNow = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            QuickSettingPreset entry = exists
                ? Cache[normalizedId]
                : new QuickSettingPreset
                {
                    Id = normalizedId,
                    IsBuiltin = false,
                    CreatedUtc = utcNow,
                };

            entry.Name = name.Trim();
            entry.UpdatedUtc = utcNow;
            entry.Values = QuickSettingsValuesBuilder.CloneValues(FilterAllowedValues(values));
            Cache[normalizedId] = entry;
            preset = entry;
            _dirty = true;
            FlushToDisk(waitForCompletion: false);
            return true;
        }

        internal static bool TryDelete(string presetId, out string? error)
        {
            error = null;
            if (BuiltinQuickSettings.IsBuiltin(presetId))
            {
                error = ModL10n.Get("api.quick_preset_builtin_readonly");
                return false;
            }

            EnsureLoaded();
            if (!Cache.Remove(presetId))
            {
                error = ModL10n.Get("api.quick_preset_not_found");
                return false;
            }

            _dirty = true;
            FlushToDisk(waitForCompletion: false);
            return true;
        }

        internal static string CreatePresetIdFromName(string name)
        {
            string slug = Slugify(name);
            if (string.IsNullOrEmpty(slug))
            {
                slug = "preset";
            }

            string candidate = UserPrefix + slug;
            EnsureLoaded();
            if (!Cache.ContainsKey(candidate))
            {
                return candidate;
            }

            for (int i = 2; i < 1000; i++)
            {
                candidate = UserPrefix + slug + "_" + i;
                if (!Cache.ContainsKey(candidate))
                {
                    return candidate;
                }
            }

            return UserPrefix + slug + "_" + Guid.NewGuid().ToString("N")[..8];
        }

        internal static bool TryNormalizeUserPresetId(string presetId, out string normalizedId, out string? error)
        {
            normalizedId = "";
            error = null;
            if (string.IsNullOrWhiteSpace(presetId))
            {
                error = ModL10n.Get("api.quick_preset_invalid_id");
                return false;
            }

            normalizedId = presetId.Trim();
            if (!normalizedId.StartsWith(UserPrefix, StringComparison.OrdinalIgnoreCase))
            {
                normalizedId = UserPrefix + normalizedId;
            }

            string slug = normalizedId[UserPrefix.Length..];
            if (!SlugPattern.IsMatch(slug))
            {
                error = ModL10n.Get("api.quick_preset_invalid_id");
                return false;
            }

            normalizedId = UserPrefix + slug.ToLowerInvariant();
            return true;
        }

        internal static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            Cache.Clear();
            string? filePath = SaveSidecarPaths.GetUserQuickPresetsPath();
            if (string.IsNullOrEmpty(filePath))
            {
                _loaded = true;
                return;
            }

            string? text = AtomicFileIO.ReadText(filePath, Feature);
            ParseDocument(SparseTomlConfig.Load(text));
            _loaded = true;
            _dirty = false;
        }

        internal static void FlushToDisk(bool waitForCompletion)
        {
            if (!_dirty)
            {
                return;
            }

            string? filePath = SaveSidecarPaths.GetUserQuickPresetsPath();
            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            SparseTomlConfig.Document doc = SerializeDocument();
            if (SparseTomlConfig.IsEmpty(doc))
            {
                BackgroundFileWriteQueue.EnqueueDelete(filePath, Feature, waitForCompletion);
            }
            else
            {
                BackgroundFileWriteQueue.EnqueueText(
                    filePath,
                    SparseTomlConfig.Serialize(doc),
                    Feature,
                    waitForCompletion);
            }

            _dirty = false;
        }

        private static void ParseDocument(SparseTomlConfig.Document doc)
        {
            Dictionary<string, QuickSettingPreset> building = new(StringComparer.OrdinalIgnoreCase);

            foreach (string sectionId in doc.SectionOrder)
            {
                if (string.Equals(sectionId, HeaderSectionId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!doc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys))
                {
                    continue;
                }

                if (TryParseMetaSection(sectionId, keys, out string presetId, out QuickSettingPreset preset))
                {
                    building[presetId] = preset;
                    continue;
                }

                if (TryParseValuesSection(sectionId, keys, building))
                {
                    continue;
                }
            }

            foreach (QuickSettingPreset preset in building.Values)
            {
                Cache[preset.Id] = preset;
            }
        }

        private static bool TryParseMetaSection(
            string sectionId,
            Dictionary<string, string> keys,
            out string presetId,
            out QuickSettingPreset preset)
        {
            presetId = "";
            preset = null!;
            if (!sectionId.StartsWith(UserPrefix, StringComparison.OrdinalIgnoreCase)
                || sectionId.Contains(".values.", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            presetId = sectionId;
            if (!TryNormalizeUserPresetId(presetId, out presetId, out _))
            {
                return false;
            }

            preset = new QuickSettingPreset
            {
                Id = presetId,
                IsBuiltin = false,
                Name = keys.TryGetValue("Name", out string? name) ? name : presetId,
                CreatedUtc = keys.TryGetValue("CreatedUtc", out string? created) ? created : "",
                UpdatedUtc = keys.TryGetValue("UpdatedUtc", out string? updated) ? updated : "",
                Values = QuickSettingsValuesBuilder.CreateMap(),
            };
            return true;
        }

        private static bool TryParseValuesSection(
            string sectionId,
            Dictionary<string, string> keys,
            Dictionary<string, QuickSettingPreset> building)
        {
            const string valuesMarker = ".values.";
            int markerIndex = sectionId.IndexOf(valuesMarker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex <= 0)
            {
                return false;
            }

            string presetId = sectionId[..markerIndex];
            if (!TryNormalizeUserPresetId(presetId, out presetId, out _))
            {
                return false;
            }

            string configSectionId = sectionId[(markerIndex + valuesMarker.Length)..];
            if (!building.TryGetValue(presetId, out QuickSettingPreset? preset))
            {
                preset = new QuickSettingPreset
                {
                    Id = presetId,
                    IsBuiltin = false,
                    Name = presetId,
                    Values = QuickSettingsValuesBuilder.CreateMap(),
                };
                building[presetId] = preset;
            }

            foreach (KeyValuePair<string, string> pair in keys)
            {
                if (ModConfigRegistry.IsSaveOverrideAllowed(configSectionId, pair.Key))
                {
                    preset.SetValue(configSectionId, pair.Key, pair.Value);
                }
            }

            return true;
        }

        private static SparseTomlConfig.Document SerializeDocument()
        {
            SparseTomlConfig.Document doc = new();
            if (Cache.Count == 0)
            {
                return doc;
            }

            doc.SectionOrder.Add(HeaderSectionId);
            doc.Sections[HeaderSectionId] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [SchemaVersionKey] = CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture),
            };

            foreach (QuickSettingPreset preset in Cache.Values)
            {
                string metaSection = preset.Id;
                doc.SectionOrder.Add(metaSection);
                doc.Sections[metaSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Name"] = preset.Name ?? preset.Id,
                    ["CreatedUtc"] = preset.CreatedUtc ?? "",
                    ["UpdatedUtc"] = preset.UpdatedUtc ?? "",
                };

                foreach (KeyValuePair<string, Dictionary<string, string>> section in preset.Values)
                {
                    if (section.Value.Count == 0)
                    {
                        continue;
                    }

                    string valuesSection = preset.Id + ".values." + section.Key;
                    doc.SectionOrder.Add(valuesSection);
                    doc.Sections[valuesSection] = new Dictionary<string, string>(section.Value, StringComparer.OrdinalIgnoreCase);
                }
            }

            return doc;
        }

        private static Dictionary<string, Dictionary<string, string>> FilterAllowedValues(
            Dictionary<string, Dictionary<string, string>> values)
        {
            Dictionary<string, Dictionary<string, string>> filtered = QuickSettingsValuesBuilder.CreateMap();
            foreach (KeyValuePair<string, Dictionary<string, string>> section in values)
            {
                foreach (KeyValuePair<string, string> pair in section.Value)
                {
                    if (!ModConfigRegistry.IsSaveOverrideAllowed(section.Key, pair.Key))
                    {
                        continue;
                    }

                    if (ModConfigRegistry.TryNormalizeRawValue(section.Key, pair.Key, pair.Value, out string normalized, out _))
                    {
                        QuickSettingsValuesBuilder.Set(filtered, section.Key, pair.Key, normalized);
                    }
                }
            }

            return filtered;
        }

        private static string Slugify(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "";
            }

            string lower = name.Trim().ToLowerInvariant();
            char[] buffer = new char[lower.Length];
            int length = 0;
            bool lastWasDash = false;

            foreach (char ch in lower)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    buffer[length++] = ch;
                    lastWasDash = false;
                    continue;
                }

                if (!lastWasDash && length > 0)
                {
                    buffer[length++] = '_';
                    lastWasDash = true;
                }
            }

            while (length > 0 && buffer[length - 1] == '_')
            {
                length--;
            }

            return length == 0 ? "" : new string(buffer, 0, length);
        }
    }
}
