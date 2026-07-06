using System;

namespace MimesisPlayerEnhancement.Config.QuickSettings
{
    internal enum SaveConfigProfileMode
    {
        Global,
        Quick,
        Custom,
    }

    internal sealed class QuickSettingPreset
    {
        internal string Id = "";
        internal string? Name;
        internal int Revision;
        internal bool IsBuiltin;
        internal string? CreatedUtc;
        internal string? UpdatedUtc;
        internal Dictionary<string, Dictionary<string, string>> Values =
            new(StringComparer.OrdinalIgnoreCase);

        internal bool TryGetValue(string sectionId, string key, out string rawValue)
        {
            rawValue = "";
            return Values.TryGetValue(sectionId, out Dictionary<string, string>? keys)
                && keys.TryGetValue(key, out rawValue!);
        }

        internal void SetValue(string sectionId, string key, string rawValue)
        {
            if (!Values.TryGetValue(sectionId, out Dictionary<string, string>? keys))
            {
                keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Values[sectionId] = keys;
            }

            keys[key] = rawValue;
        }
    }

    internal sealed class SaveConfigProfileState
    {
        internal SaveConfigProfileMode Mode = SaveConfigProfileMode.Global;
        internal string PresetId = "";
        internal int PresetRevision;
    }
}
