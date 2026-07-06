using System;
using MimesisPlayerEnhancement.Config.QuickSettings;

namespace MimesisPlayerEnhancement
{
    internal static class SaveSlotConfigProfile
    {
        internal const string SectionId = "MimesisPlayerEnhancement_Profile";
        internal const string KeyMode = "Mode";
        internal const string KeyPresetId = "PresetId";
        internal const string KeyPresetRevision = "PresetRevision";

        internal static bool IsProfileSection(string sectionId)
        {
            return string.Equals(sectionId, SectionId, StringComparison.OrdinalIgnoreCase);
        }

        internal static SaveConfigProfileState Parse(SparseTomlConfig.Document doc)
        {
            SaveConfigProfileState state = new();
            if (!doc.Sections.TryGetValue(SectionId, out Dictionary<string, string>? keys))
            {
                state.Mode = InferLegacyMode(doc);
                return state;
            }

            if (keys.TryGetValue(KeyMode, out string? modeRaw))
            {
                state.Mode = ParseMode(modeRaw);
            }
            else
            {
                state.Mode = InferLegacyMode(doc);
            }

            if (keys.TryGetValue(KeyPresetId, out string? presetId))
            {
                state.PresetId = presetId?.Trim() ?? "";
            }

            if (keys.TryGetValue(KeyPresetRevision, out string? revisionRaw)
                && int.TryParse(revisionRaw, out int revision))
            {
                state.PresetRevision = revision;
            }

            if (state.Mode == SaveConfigProfileMode.Quick && string.IsNullOrWhiteSpace(state.PresetId))
            {
                state.Mode = SaveConfigProfileMode.Custom;
            }

            return state;
        }

        internal static SaveConfigProfileState TryReadFromDisk(int slotId)
        {
            string? filePath = SaveSlotConfigStore.GetOverrideFilePath(slotId);
            if (string.IsNullOrEmpty(filePath))
            {
                return new SaveConfigProfileState();
            }

            string? text = AtomicFileIO.ReadText(filePath, "SaveSlotConfigProfile");
            SparseTomlConfig.Document doc = SparseTomlConfig.Load(text);
            return Parse(doc);
        }

        internal static string GetDisplayLabel(SaveConfigProfileState profile)
        {
            return profile.Mode switch
            {
                SaveConfigProfileMode.Global => ModL10n.Get("quicksettings.profile.global"),
                SaveConfigProfileMode.Custom => ModL10n.Get("quicksettings.profile.custom"),
                SaveConfigProfileMode.Quick => QuickSettingsCatalog.GetDisplayName(profile.PresetId),
                _ => ModL10n.Get("quicksettings.profile.global"),
            };
        }

        internal static void WriteProfileSection(
            SparseTomlConfig.Document doc,
            SaveConfigProfileState profile)
        {
            if (profile.Mode == SaveConfigProfileMode.Global)
            {
                RemoveProfileSection(doc);
                return;
            }

            if (!doc.Sections.TryGetValue(SectionId, out Dictionary<string, string>? keys))
            {
                doc.SectionOrder.Add(SectionId);
                keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                doc.Sections[SectionId] = keys;
            }

            keys[KeyMode] = FormatMode(profile.Mode);

            if (profile.Mode == SaveConfigProfileMode.Quick)
            {
                keys[KeyPresetId] = profile.PresetId;
                if (profile.PresetRevision > 0)
                {
                    keys[KeyPresetRevision] = profile.PresetRevision.ToString();
                }
                else
                {
                    _ = keys.Remove(KeyPresetRevision);
                }
            }
            else
            {
                _ = keys.Remove(KeyPresetId);
                _ = keys.Remove(KeyPresetRevision);
            }
        }

        internal static void RemoveProfileSection(SparseTomlConfig.Document doc)
        {
            if (!doc.Sections.TryGetValue(SectionId, out _))
            {
                return;
            }

            _ = doc.Sections.Remove(SectionId);
            doc.SectionOrder.Remove(SectionId);
        }

        internal static bool HasGameplayOverrides(SparseTomlConfig.Document doc)
        {
            foreach (string sectionId in doc.SectionOrder)
            {
                if (IsProfileSection(sectionId))
                {
                    continue;
                }

                if (doc.Sections.TryGetValue(sectionId, out Dictionary<string, string>? keys) && keys.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static SaveConfigProfileMode InferLegacyMode(SparseTomlConfig.Document doc)
        {
            return HasGameplayOverrides(doc)
                ? SaveConfigProfileMode.Custom
                : SaveConfigProfileMode.Global;
        }

        private static SaveConfigProfileMode ParseMode(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return SaveConfigProfileMode.Global;
            }

            return raw.Trim().ToLowerInvariant() switch
            {
                "quick" => SaveConfigProfileMode.Quick,
                "custom" => SaveConfigProfileMode.Custom,
                "global" => SaveConfigProfileMode.Global,
                _ => SaveConfigProfileMode.Global,
            };
        }

        private static string FormatMode(SaveConfigProfileMode mode)
        {
            return mode switch
            {
                SaveConfigProfileMode.Quick => "quick",
                SaveConfigProfileMode.Custom => "custom",
                _ => "global",
            };
        }
    }
}
