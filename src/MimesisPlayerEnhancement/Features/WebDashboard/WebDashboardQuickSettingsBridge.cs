using MimesisPlayerEnhancement.Config.QuickSettings;
using MimesisPlayerEnhancement.Features.WebDashboard.Models;

namespace MimesisPlayerEnhancement.Features.WebDashboard
{
    internal static class WebDashboardQuickSettingsBridge
    {
        private const string Feature = "WebDashboard";

        private static string L(string key) => WebDashboardL10n.Get($"api.{key}");

        internal static WebDashboardSaveProfileResponseDto BuildSaveProfile(int slotId)
        {
            if (MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                SaveSlotSidecarPersistence.EnsureSaveSlotLoaded(slotId);
            }

            SaveConfigProfileState profile = slotId == SaveSlotConfigStore.ActiveSlotId
                ? SaveSlotConfigStore.ActiveProfile
                : SaveSlotConfigProfile.TryReadFromDisk(slotId);

            return new WebDashboardSaveProfileResponseDto
            {
                Profile = ToProfileDto(profile),
                ConfigVersion = ModConfig.Version,
            };
        }

        internal static WebDashboardQuickPresetsListDto BuildPresetList()
        {
            UserQuickSettingsStore.EnsureLoaded();
            WebDashboardQuickPresetsListDto dto = new();
            foreach (QuickSettingPreset preset in QuickSettingsCatalog.ListAllPresets())
            {
                dto.Presets.Add(ToPresetDto(preset));
            }

            return dto;
        }

        internal static WebDashboardSaveProfileResponseDto ApplySaveProfile(
            int slotId,
            WebDashboardSaveProfileRequest request)
        {
            if (request == null)
            {
                return FailProfile(L("invalid_settings_request"));
            }

            string? error;
            SaveConfigProfileMode mode = ParseMode(request.Mode);
            if (mode == SaveConfigProfileMode.Global)
            {
                if (!SaveSlotConfigStore.TrySetProfileMode(slotId, SaveConfigProfileMode.Global, null, out error))
                {
                    return FailProfile(error ?? L("failed_apply"));
                }

                return SuccessProfile(slotId, L("quick_profile_global_applied"));
            }

            if (mode == SaveConfigProfileMode.Quick)
            {
                if (!SaveSlotConfigStore.TryApplyQuickPreset(slotId, request.PresetId, out error))
                {
                    return FailProfile(error ?? L("quick_preset_not_found"));
                }

                return SuccessProfile(slotId, L("quick_profile_preset_applied"));
            }

            if (mode == SaveConfigProfileMode.Custom)
            {
                if (!SaveSlotConfigStore.TrySetProfileMode(slotId, SaveConfigProfileMode.Custom, null, out error))
                {
                    return FailProfile(error ?? L("failed_apply"));
                }

                return SuccessProfile(slotId, L("quick_profile_custom_applied"));
            }

            return FailProfile(L("invalid_settings_request"));
        }

        internal static WebDashboardQuickPresetDto? SaveUserPreset(WebDashboardQuickPresetSaveRequest request)
        {
            if (request == null)
            {
                return null;
            }

            Dictionary<string, Dictionary<string, string>> values = request.FromCurrentSave
                ? QuickSettingsValuesBuilder.CollectEffectiveValues()
                : request.Values ?? QuickSettingsValuesBuilder.CreateMap();

            string presetId = string.IsNullOrWhiteSpace(request.PresetId)
                ? UserQuickSettingsStore.CreatePresetIdFromName(request.Name)
                : request.PresetId;

            if (!UserQuickSettingsStore.TryCreateOrUpdate(
                    presetId,
                    request.Name,
                    values,
                    request.OverwriteExisting,
                    out QuickSettingPreset preset,
                    out string? error))
            {
                ModLog.Warn(Feature, $"Save user preset failed — {error}");
                return null;
            }

            return ToPresetDto(preset);
        }

        internal static bool DeleteUserPreset(string presetId, out string? error)
        {
            return UserQuickSettingsStore.TryDelete(presetId, out error);
        }

        internal static WebDashboardQuickPresetShareDto ExportPreset(string presetId)
        {
            if (!QuickSettingsCatalog.TryResolvePreset(presetId, out QuickSettingPreset preset))
            {
                return new WebDashboardQuickPresetShareDto();
            }

            string name = QuickSettingsCatalog.GetDisplayName(presetId, WebDashboardRequestLocale.Current);
            return new WebDashboardQuickPresetShareDto
            {
                Name = name,
                ShareString = QuickSettingsShareCodec.Encode(name, preset.Values),
            };
        }

        internal static WebDashboardQuickPresetShareDto ExportCurrentSave(int slotId)
        {
            if (MimesisSaveManager.IsValidSaveSlotId(slotId))
            {
                SaveSlotSidecarPersistence.EnsureSaveSlotLoaded(slotId);
            }

            Dictionary<string, Dictionary<string, string>> values = QuickSettingsValuesBuilder.CollectEffectiveValues();
            SaveConfigProfileState profile = SaveSlotConfigStore.ActiveProfile;
            string name = profile.Mode == SaveConfigProfileMode.Quick
                ? QuickSettingsCatalog.GetDisplayName(profile.PresetId, WebDashboardRequestLocale.Current)
                : WebDashboardL10n.Get("quicksettings.profile.custom");

            return new WebDashboardQuickPresetShareDto
            {
                Name = name,
                ShareString = QuickSettingsShareCodec.Encode(name, values),
            };
        }

        internal static WebDashboardQuickPresetImportResultDto ImportShareString(
            int slotId,
            WebDashboardQuickPresetImportRequest request)
        {
            if (request == null)
            {
                return new WebDashboardQuickPresetImportResultDto
                {
                    Success = false,
                    Message = L("quick_share_invalid"),
                };
            }

            if (!QuickSettingsShareCodec.TryDecode(request.ShareString, out QuickSettingsShareCodec.SharePayload payload, out string? decodeError))
            {
                return new WebDashboardQuickPresetImportResultDto
                {
                    Success = false,
                    Message = decodeError ?? L("quick_share_invalid"),
                };
            }

            string name = string.IsNullOrWhiteSpace(request.Name) ? payload.Name : request.Name!;
            string presetId = string.IsNullOrWhiteSpace(request.PresetId)
                ? UserQuickSettingsStore.CreatePresetIdFromName(name)
                : request.PresetId;

            if (!UserQuickSettingsStore.TryCreateOrUpdate(
                    presetId,
                    name,
                    payload.Values,
                    request.OverwriteExisting,
                    out QuickSettingPreset preset,
                    out string? saveError))
            {
                return new WebDashboardQuickPresetImportResultDto
                {
                    Success = false,
                    Message = saveError ?? L("failed_apply"),
                };
            }

            if (!request.SaveOnly
                && MimesisSaveManager.IsValidSaveSlotId(slotId)
                && SaveSlotConfigStore.ActiveSlotId == slotId)
            {
                _ = SaveSlotConfigStore.TryApplyQuickPreset(slotId, preset.Id, out _);
            }

            return new WebDashboardQuickPresetImportResultDto
            {
                Success = true,
                Message = L("quick_preset_imported"),
                Preset = ToPresetDto(preset),
                ShareString = request.ShareString,
            };
        }

        internal static WebDashboardSaveProfileDto ToProfileDto(SaveConfigProfileState profile)
        {
            return new WebDashboardSaveProfileDto
            {
                Mode = FormatMode(profile.Mode),
                PresetId = profile.PresetId ?? "",
                Label = SaveSlotConfigProfile.GetDisplayLabel(profile),
            };
        }

        private static WebDashboardQuickPresetDto ToPresetDto(QuickSettingPreset preset)
        {
            return new WebDashboardQuickPresetDto
            {
                Id = preset.Id,
                Name = preset.IsBuiltin
                    ? QuickSettingsCatalog.GetDisplayName(preset.Id, WebDashboardRequestLocale.Current)
                    : (preset.Name ?? preset.Id),
                Description = QuickSettingsCatalog.GetDescription(preset.Id, WebDashboardRequestLocale.Current),
                IsBuiltin = preset.IsBuiltin,
                Revision = preset.Revision,
                CreatedUtc = preset.CreatedUtc,
                UpdatedUtc = preset.UpdatedUtc,
            };
        }

        private static WebDashboardSaveProfileResponseDto SuccessProfile(int slotId, string message)
        {
            SaveConfigProfileState profile = slotId == SaveSlotConfigStore.ActiveSlotId
                ? SaveSlotConfigStore.ActiveProfile
                : SaveSlotConfigProfile.TryReadFromDisk(slotId);

            return new WebDashboardSaveProfileResponseDto
            {
                Success = true,
                Message = message,
                Profile = ToProfileDto(profile),
                ConfigVersion = ModConfig.Version,
            };
        }

        private static WebDashboardSaveProfileResponseDto FailProfile(string message)
        {
            return new WebDashboardSaveProfileResponseDto
            {
                Success = false,
                Message = message,
                Profile = new WebDashboardSaveProfileDto(),
                ConfigVersion = ModConfig.Version,
            };
        }

        private static SaveConfigProfileMode ParseMode(string? raw)
        {
            return raw?.Trim().ToLowerInvariant() switch
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
