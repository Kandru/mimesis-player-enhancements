using System.IO;
using System.IO.Compression;
using System.Text;
using MimesisPlayerEnhancement.Config.QuickSettings;
using Newtonsoft.Json.Linq;

namespace MimesisPlayerEnhancement
{
    internal static class QuickSettingsShareCodec
    {
        private const string Feature = "QuickSettingsShare";
        private const string Prefix = "MPE1:";

        internal sealed class SharePayload
        {
            internal string Name = "";
            internal Dictionary<string, Dictionary<string, string>> Values =
                new(StringComparer.OrdinalIgnoreCase);
        }

        internal static string Encode(string name, Dictionary<string, Dictionary<string, string>> values)
        {
            return EncodePayload(name, FilterAllowedValues(values));
        }

        /// <summary>
        /// Encode already-normalized values without ModConfigRegistry filtering (tests / trusted callers).
        /// </summary>
        internal static string EncodePayload(string name, Dictionary<string, Dictionary<string, string>> values)
        {
            JObject root = new()
            {
                ["v"] = 1,
                ["name"] = name ?? "",
                ["values"] = SerializeValues(values),
            };

            byte[] jsonBytes = Encoding.UTF8.GetBytes(root.ToString(Newtonsoft.Json.Formatting.None));
            byte[] compressed = Compress(jsonBytes);
            return Prefix + Base64UrlEncode(compressed);
        }

        internal static bool TryDecode(string shareString, out SharePayload payload, out string? error)
        {
            if (!TryDecodePayload(shareString, out payload, out error))
            {
                return false;
            }

            payload.Values = FilterAllowedValues(payload.Values);
            if (payload.Values.Count == 0)
            {
                error = ModL10n.Get("api.quick_share_empty");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Decode share string to raw values without ModConfigRegistry filtering (tests / inspection).
        /// </summary>
        internal static bool TryDecodePayload(string shareString, out SharePayload payload, out string? error)
        {
            payload = new SharePayload();
            error = null;

            if (!TryDecodeRoot(shareString, out JObject root, out error))
            {
                return false;
            }

            payload.Name = root.Value<string>("name") ?? "";
            payload.Values = ParseValuesRaw(root["values"] as JObject);
            if (payload.Values.Count == 0)
            {
                error = ModL10n.Get("api.quick_share_empty");
                return false;
            }

            return true;
        }

        internal static Dictionary<string, Dictionary<string, string>> FilterAllowedValues(
            Dictionary<string, Dictionary<string, string>> values)
        {
            Dictionary<string, Dictionary<string, string>> filtered = QuickSettingsValuesBuilder.CreateMap();
            foreach (KeyValuePair<string, Dictionary<string, string>> section in values)
            {
                foreach (KeyValuePair<string, string> pair in section.Value)
                {
                    if (!ModConfigRegistry.IsSaveOverrideAllowed(section.Key, pair.Key))
                    {
                        ModLog.Debug(Feature, $"Skipped unknown/disallowed key {section.Key}/{pair.Key} in share import.");
                        continue;
                    }

                    if (ModConfigRegistry.TryNormalizeRawValue(section.Key, pair.Key, pair.Value, out string normalized, out _))
                    {
                        QuickSettingsValuesBuilder.Set(filtered, section.Key, pair.Key, normalized);
                    }
                    else
                    {
                        ModLog.Warn(Feature, $"Skipped invalid share value {section.Key}/{pair.Key}.");
                    }
                }
            }

            return filtered;
        }

        private static JObject SerializeValues(Dictionary<string, Dictionary<string, string>> values)
        {
            JObject root = new();
            foreach (KeyValuePair<string, Dictionary<string, string>> section in values)
            {
                JObject sectionObj = new();
                foreach (KeyValuePair<string, string> pair in section.Value)
                {
                    sectionObj[pair.Key] = pair.Value;
                }

                root[section.Key] = sectionObj;
            }

            return root;
        }

        private static bool TryDecodeRoot(string shareString, out JObject root, out string? error)
        {
            root = new JObject();
            error = null;

            if (string.IsNullOrWhiteSpace(shareString))
            {
                error = ModL10n.Get("api.quick_share_invalid");
                return false;
            }

            string trimmed = shareString.Trim();
            if (!trimmed.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                error = ModL10n.Get("api.quick_share_invalid");
                return false;
            }

            string encoded = trimmed[Prefix.Length..];
            byte[] compressed;
            try
            {
                compressed = Base64UrlDecode(encoded);
            }
            catch (Exception ex)
            {
                error = ModL10n.Get("api.quick_share_invalid") + " " + ex.Message;
                return false;
            }

            byte[] jsonBytes;
            try
            {
                jsonBytes = Decompress(compressed);
            }
            catch (Exception ex)
            {
                error = ModL10n.Get("api.quick_share_invalid") + " " + ex.Message;
                return false;
            }

            JObject? parsed = ModJson.Deserialize<JObject>(Encoding.UTF8.GetString(jsonBytes));
            if (parsed == null)
            {
                error = ModL10n.Get("api.quick_share_invalid");
                return false;
            }

            int version = parsed.Value<int?>("v") ?? 0;
            if (version != 1)
            {
                error = ModL10n.Get("api.quick_share_unsupported_version");
                return false;
            }

            root = parsed;
            return true;
        }

        private static Dictionary<string, Dictionary<string, string>> ParseValuesRaw(JObject? valuesObject)
        {
            Dictionary<string, Dictionary<string, string>> values = QuickSettingsValuesBuilder.CreateMap();
            if (valuesObject == null)
            {
                return values;
            }

            foreach (KeyValuePair<string, JToken?> section in valuesObject)
            {
                if (section.Value is not JObject sectionObj)
                {
                    continue;
                }

                foreach (KeyValuePair<string, JToken?> pair in sectionObj)
                {
                    if (pair.Value == null)
                    {
                        continue;
                    }

                    QuickSettingsValuesBuilder.Set(values, section.Key, pair.Key, pair.Value.ToString());
                }
            }

            return values;
        }

        private static byte[] Compress(byte[] input)
        {
            using MemoryStream output = new();
            using (GZipStream gzip = new(output, CompressionLevel.Optimal, leaveOpen: true))
            {
                gzip.Write(input, 0, input.Length);
            }

            return output.ToArray();
        }

        private static byte[] Decompress(byte[] input)
        {
            using MemoryStream inputStream = new(input);
            using GZipStream gzip = new(inputStream, CompressionMode.Decompress);
            using MemoryStream output = new();
            gzip.CopyTo(output);
            return output.ToArray();
        }

        private static string Base64UrlEncode(byte[] data)
        {
            return Convert.ToBase64String(data)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string encoded)
        {
            string padded = encoded.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2:
                    padded += "==";
                    break;
                case 3:
                    padded += "=";
                    break;
            }

            return Convert.FromBase64String(padded);
        }
    }
}
