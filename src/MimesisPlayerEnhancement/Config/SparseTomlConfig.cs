using System;
using System.Collections.Generic;
using System.Globalization;
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

                foreach (KeyValuePair<string, string> pair in keys)
                {
                    if (ModConfigRegistry.TryGetEntry(sectionId, pair.Key, out _))
                    {
                        continue;
                    }

                    _ = sb.Append(pair.Key).Append(" = ").AppendLine(FormatValue(pair.Value));
                }
            }

            return sb.ToString();
        }

        internal static bool IsEmpty(Document doc)
        {
            foreach (Dictionary<string, string> keys in doc.Sections.Values)
            {
                if (keys.Count > 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static string FormatValue(string value)
        {
            if (value.IndexOfAny([' ', '\t', '#', '[', '=', '"', '\n', '\r']) >= 0)
            {
                return '"' + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + '"';
            }

            return value;
        }
    }
}
