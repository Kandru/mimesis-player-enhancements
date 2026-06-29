using System.Collections.Generic;
using MelonLoader;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardConfigBridge
    {
        internal static WebDashboardSettingsDto BuildSettings()
        {
            if (!ModConfig.IsInitialized)
            {
                return new WebDashboardSettingsDto
                {
                    ConfigPath = ModConfig.FilePath,
                    ConfigVersion = ModConfig.Version,
                };
            }

            List<WebDashboardConfigSectionDto> sections = [];

            foreach (string sectionId in ModConfigRegistry.GetSectionOrder())
            {
                if (!ModConfigRegistry.TryGetSectionTitle(sectionId, out string title))
                {
                    title = sectionId;
                }

                WebDashboardConfigSectionDto section = new()
                {
                    Id = sectionId,
                    Title = title,
                };

                foreach (string key in ModConfigRegistry.GetEntryOrder(sectionId))
                {
                    if (!ModConfigRegistry.TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null)
                    {
                        continue;
                    }

                    section.Entries.Add(new WebDashboardConfigEntryDto
                    {
                        Key = entry.Identifier,
                        Title = entry.DisplayName ?? entry.Identifier,
                        Description = entry.Description ?? "",
                        Type = entry.GetReflectedType()?.Name ?? "Unknown",
                        Value = ModConfigRegistry.FormatEntryValue(entry),
                        DefaultValue = ModConfigRegistry.FormatEntryDefaultValue(entry),
                        IsHidden = entry.IsHidden,
                    });
                }

                if (section.Entries.Count > 0)
                {
                    sections.Add(section);
                }
            }

            return new WebDashboardSettingsDto
            {
                ConfigPath = ModConfig.FilePath,
                ConfigVersion = ModConfig.Version,
                Sections = sections,
            };
        }

        internal static WebDashboardConfigUpdateResult ApplyUpdate(string sectionId, string key, string value)
        {
            if (!ModConfig.IsInitialized)
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = "Configuration is not initialized.",
                };
            }

            if (!ModConfig.TrySetEntryValue(sectionId, key, value, out string? error))
            {
                return new WebDashboardConfigUpdateResult
                {
                    Success = false,
                    Message = error ?? "Invalid value.",
                };
            }

            ModConfig.SanitizeFloatEntries();
            ModConfig.SaveToFile();

            return !ModConfigRegistry.TryGetEntry(sectionId, key, out MelonPreferences_Entry? entry) || entry == null
                ? new WebDashboardConfigUpdateResult
                {
                    Success = true,
                    Message = "Saved.",
                }
                : new WebDashboardConfigUpdateResult
                {
                    Success = true,
                    Message = "Saved.",
                    SectionId = sectionId,
                    Key = key,
                    Value = ModConfigRegistry.FormatEntryValue(entry),
                    Type = entry.GetReflectedType()?.Name ?? "Unknown",
                };
        }
    }
}
