using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MimesisPlayerEnhancement
{
    /// <summary>
    /// Minimal TOML reader/writer for sparse mod override files ([section] + key = value lines).
    /// </summary>
    internal static class SparseTomlConfig
    {
        internal sealed class Document
        {
            internal readonly List<string> SectionOrder = [];
            internal readonly Dictionary<string, Dictionary<string, string>> Sections =
                new(StringComparer.OrdinalIgnoreCase);
        }

        internal static Document Load(string? text)
        {
            Document doc = new();
            if (string.IsNullOrWhiteSpace(text))
            {
                return doc;
            }

            string? currentSection = null;
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                if (line.StartsWith('[') && line.EndsWith(']') && line.Length > 2)
                {
                    currentSection = line[1..^1].Trim();
                    if (!doc.Sections.ContainsKey(currentSection))
                    {
                        doc.SectionOrder.Add(currentSection);
                        doc.Sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }

                    continue;
                }

                if (currentSection == null)
                {
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                string key = line[..eq].Trim();
                string value = line[(eq + 1)..].Trim();
                if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
                {
                    value = value[1..^1];
                }

                if (key.Length == 0)
                {
                    continue;
                }

                if (!doc.Sections.TryGetValue(currentSection, out Dictionary<string, string>? keys))
                {
                    keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    doc.Sections[currentSection] = keys;
                    doc.SectionOrder.Add(currentSection);
                }

                keys[key] = value;
            }

            return doc;
        }

        internal static string Serialize(Document doc)
        {
            if (ModConfig.IsInitialized)
            {
                _ = PurgeUnregisteredEntries(doc, allowProfileSection: true);
            }

            StringBuilder sb = new();
            for (int s = 0; s < doc.SectionOrder.Count; s++)
            {
                string sectionId = doc.SectionOrder[s];
                if (!doc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys) || keys.Count == 0)
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    _ = sb.AppendLine();
                }

                _ = sb.Append('[').Append(sectionId).AppendLine("]");

                foreach (string key in ModConfigRegistry.GetEntryOrder(sectionId))
                {
                    if (!keys.TryGetValue(key, out string? value))
                    {
                        continue;
                    }

                    _ = sb.Append(key).Append(" = ").AppendLine(FormatValue(value));
                }

                if (SaveSlotConfigProfile.IsProfileSection(sectionId))
                {
                    WriteProfileKeys(sb, keys);
                }
            }

            return sb.ToString();
        }

        private static void WriteProfileKeys(StringBuilder sb, Dictionary<string, string> keys)
        {
            WriteProfileKeyIfPresent(sb, keys, SaveSlotConfigProfile.KeyMode);
            WriteProfileKeyIfPresent(sb, keys, SaveSlotConfigProfile.KeyPresetId);
            WriteProfileKeyIfPresent(sb, keys, SaveSlotConfigProfile.KeyPresetRevision);
        }

        private static void WriteProfileKeyIfPresent(
            StringBuilder sb,
            Dictionary<string, string> keys,
            string key)
        {
            if (keys.TryGetValue(key, out string? value))
            {
                _ = sb.Append(key).Append(" = ").AppendLine(FormatValue(value));
            }
        }

        /// <summary>Writes all sections/keys in document order (used after obsolete-entry cleanup).</summary>
        internal static string SerializeRaw(Document doc)
        {
            StringBuilder sb = new();
            foreach (string sectionId in doc.SectionOrder)
            {
                if (!doc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys) || keys.Count == 0)
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    _ = sb.AppendLine();
                }

                _ = sb.Append('[').Append(sectionId).AppendLine("]");

                foreach (KeyValuePair<string, string> pair in keys.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
                {
                    _ = sb.Append(pair.Key).Append(" = ").AppendLine(FormatValue(pair.Value));
                }
            }

            string text = sb.ToString();
            return text.Length == 0 ? text : text + Environment.NewLine;
        }

        /// <summary>
        /// Removes sections and keys that are not registered in <see cref="ModConfigRegistry"/>.
        /// Profile sidecar metadata is kept when <paramref name="allowProfileSection"/> is true.
        /// </summary>
        internal static bool PurgeUnregisteredEntries(Document doc, bool allowProfileSection = false)
        {
            bool changed = false;
            HashSet<string> registeredSections = new(ModConfigRegistry.GetSectionOrder(), StringComparer.OrdinalIgnoreCase);

            foreach (string sectionId in doc.SectionOrder.ToList())
            {
                if (allowProfileSection && SaveSlotConfigProfile.IsProfileSection(sectionId))
                {
                    if (doc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? profileKeys))
                    {
                        changed |= PurgeProfileSectionKeys(profileKeys);
                        if (profileKeys.Count == 0)
                        {
                            _ = doc.Sections.Remove(sectionId);
                            doc.SectionOrder.Remove(sectionId);
                            changed = true;
                        }
                    }

                    continue;
                }

                if (!registeredSections.Contains(sectionId))
                {
                    _ = doc.Sections.Remove(sectionId);
                    _ = doc.SectionOrder.Remove(sectionId);
                    changed = true;
                    continue;
                }

                if (!doc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys))
                {
                    continue;
                }

                foreach (string key in keys.Keys.ToList())
                {
                    if (!ModConfigRegistry.TryGetEntry(sectionId, key, out _))
                    {
                        _ = keys.Remove(key);
                        changed = true;
                    }
                }

                if (keys.Count == 0)
                {
                    _ = doc.Sections.Remove(sectionId);
                    _ = doc.SectionOrder.Remove(sectionId);
                    changed = true;
                }
            }

            return changed;
        }

        private static bool PurgeProfileSectionKeys(Dictionary<string, string> keys)
        {
            bool changed = false;
            foreach (string key in keys.Keys.ToList())
            {
                if (string.Equals(key, SaveSlotConfigProfile.KeyMode, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, SaveSlotConfigProfile.KeyPresetId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(key, SaveSlotConfigProfile.KeyPresetRevision, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                _ = keys.Remove(key);
                changed = true;
            }

            return changed;
        }

        internal static bool IsEmpty(Document doc)
        {
            foreach (KeyValuePair<string, Dictionary<string, string>> section in doc.Sections)
            {
                if (SaveSlotConfigProfile.IsProfileSection(section.Key) && section.Value.Count > 0)
                {
                    return false;
                }

                if (section.Value.Count > 0 && !SaveSlotConfigProfile.IsProfileSection(section.Key))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Quotes bare string values so MelonLoader's Tomlet parser can load the file.
        /// </summary>
        internal static void RepairTomletCompatibility(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return;
            }

            string[] lines = File.ReadAllLines(filePath);
            bool changed = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (TryRepairAssignmentLine(lines[i], out string repaired) && repaired != lines[i])
                {
                    lines[i] = repaired;
                    changed = true;
                }
            }

            if (!changed)
            {
                return;
            }

            AtomicFileIO.WriteText(filePath, string.Join(Environment.NewLine, lines) + Environment.NewLine, "SparseTomlConfig");
        }

        internal static bool TryRepairAssignmentLine(string line, out string repaired)
        {
            repaired = line;
            int eq = line.IndexOf('=');
            if (eq <= 0 || line.TrimStart().StartsWith('['))
            {
                return false;
            }

            string keyPart = line[..eq];
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
            if (!NeedsQuotingForTomlet(valuePart))
            {
                return false;
            }

            string unquoted = UnquoteTomlString(valuePart);
            repaired = keyPart + "= " + FormatValue(unquoted) + trailing;
            return true;
        }

        private static string FormatValue(string value)
        {
            if (NeedsQuotingForTomlet(value))
            {
                return QuoteTomlString(value);
            }

            return value;
        }

        private static bool NeedsQuotingForTomlet(string value)
        {
            if (value.Length == 0)
            {
                return true;
            }

            if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
            {
                return false;
            }

            if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                || float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                return false;
            }

            return true;
        }

        private static string UnquoteTomlString(string value)
        {
            if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
            {
                return value[1..^1]
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");
            }

            return value;
        }

        private static string QuoteTomlString(string value)
        {
            return '"' + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + '"';
        }
    }
}
