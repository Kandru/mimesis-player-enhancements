using MelonLoader;
using MimesisPlayerEnhancement.Features.UserInterface.LoadingWaitPlayerList;

namespace MimesisPlayerEnhancement.Features.UserInterface
{
    internal static class UiConfig
    {
        internal const string SectionId = "MimesisPlayerEnhancement_Ui";

        private const float MinFloatingDamageDurationSeconds = 1f;
        private const float MaxFloatingDamageDurationSeconds = 3f;
        private const float MinRoundStartSoundVolume = 0f;
        private const float MaxRoundStartSoundVolume = 1f;
        private static readonly string[] ValidRoundStartSoundModes = ["Vanilla", "Random", "Specific"];
        private static readonly string[] ValidCustomLoadingScreenModes = ["Vanilla", "Random", "Specific"];

        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory(SectionId);
        }

        internal static void CreateEntries()
        {
            ModConfig.ModToastDurationSeconds = ModConfig.CreateTrackedEntry(_category,
                "ModToastDurationSeconds",
                5f);

            ModConfig.EnableExtendedSaveSlots = ModConfig.CreateTrackedEntry(_category,
                "EnableExtendedSaveSlots",
                true);

            ModConfig.EnableExtendedSpectatorPlayerList = ModConfig.CreateTrackedEntry(_category,
                "EnableExtendedSpectatorPlayerList",
                true);

            ModConfig.EnableLoadingWaitPlayerList = ModConfig.CreateTrackedEntry(_category,
                "EnableLoadingWaitPlayerList",
                false);

            ModConfig.EnableExtendedInGameMenuPlayerList = ModConfig.CreateTrackedEntry(_category,
                "EnableExtendedInGameMenuPlayerList",
                true);

            ModConfig.EnableDamageHealthGlow = ModConfig.CreateTrackedEntry(_category,
                "EnableDamageHealthGlow",
                true);

            ModConfig.EnableFloatingDamageNumbers = ModConfig.CreateTrackedEntry(_category,
                "EnableFloatingDamageNumbers",
                true);

            ModConfig.FloatingDamageDurationSeconds = ModConfig.CreateTrackedEntry(_category,
                "FloatingDamageDurationSeconds",
                2f);

            ModConfig.EnableFpsUi = ModConfig.CreateTrackedEntry(_category,
                "EnableFpsUi",
                true);

            ModConfig.EnableFpsUiInventoryNetWorth = ModConfig.CreateTrackedEntry(_category,
                "EnableFpsUiInventoryNetWorth",
                true);

            ModConfig.RoundStartSoundMode = ModConfig.CreateTrackedEntry(_category,
                "RoundStartSoundMode",
                "Random");

            ModConfig.RoundStartSoundVariant = ModConfig.CreateTrackedEntry(_category,
                "RoundStartSoundVariant",
                RoundStartSoundResolver.GetDefaultVariantOptionValue());

            ModConfig.RoundStartSoundRandomPool = ModConfig.CreateTrackedEntry(_category,
                "RoundStartSoundRandomPool",
                "");

            ModConfig.RoundStartSoundVolume = ModConfig.CreateTrackedEntry(_category,
                "RoundStartSoundVolume",
                RoundStartSoundResolver.DefaultVolume);

            ModConfig.CustomLoadingScreenMode = ModConfig.CreateTrackedEntry(_category,
                "CustomLoadingScreenMode",
                "Random");

            ModConfig.CustomLoadingScreenVariant = ModConfig.CreateTrackedEntry(_category,
                "CustomLoadingScreenVariant",
                CustomLoadingScreenResolver.GetDefaultVariantOptionValue());

            ModConfig.CustomLoadingScreenRandomPool = ModConfig.CreateTrackedEntry(_category,
                "CustomLoadingScreenRandomPool",
                "");

            ModConfig.CustomLoadingScreenMotion = ModConfig.CreateTrackedEntry(_category,
                "CustomLoadingScreenMotion",
                true);
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.ModToastDurationSeconds.OnEntryValueChanged.Subscribe((_, value) =>
            {
                if (value < 1f)
                {
                    logger.Warning("ModToastDurationSeconds must be at least 1; resetting to 1.");
                    ModConfig.ModToastDurationSeconds.Value = 1f;
                    return;
                }

                ModConfig.NotifyChanged(ModConfig.ModToastDurationSeconds);
            });
            ModConfig.EnableExtendedSaveSlots.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableExtendedSaveSlots));
            ModConfig.EnableExtendedSpectatorPlayerList.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableExtendedSpectatorPlayerList));
            ModConfig.EnableLoadingWaitPlayerList.OnEntryValueChanged.Subscribe((_, _) =>
            {
                LoadingWaitPlayerListRuntime.RefreshFromConfig();
                ModConfig.NotifyChanged(ModConfig.EnableLoadingWaitPlayerList);
            });
            ModConfig.EnableExtendedInGameMenuPlayerList.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableExtendedInGameMenuPlayerList));

            ModConfig.EnableDamageHealthGlow.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableDamageHealthGlow));
            ModConfig.EnableFloatingDamageNumbers.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableFloatingDamageNumbers));
            ModConfig.FloatingDamageDurationSeconds.OnEntryValueChanged.Subscribe((_, value) =>
                OnFloatingDamageDurationChanged(logger, value));
            ModConfig.EnableFpsUi.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableFpsUi));
            ModConfig.EnableFpsUiInventoryNetWorth.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableFpsUiInventoryNetWorth));
            ModConfig.RoundStartSoundMode.OnEntryValueChanged.Subscribe((_, value) =>
                OnRoundStartSoundModeChanged(logger, value));
            ModConfig.RoundStartSoundVariant.OnEntryValueChanged.Subscribe((_, value) =>
                OnRoundStartSoundVariantChanged(logger, value));
            ModConfig.RoundStartSoundRandomPool.OnEntryValueChanged.Subscribe((_, value) =>
                OnRandomPoolChanged(ModConfig.RoundStartSoundRandomPool,
                    RoundStartSoundResolver.NormalizeRandomPoolValue, value));
            ModConfig.RoundStartSoundVolume.OnEntryValueChanged.Subscribe((_, value) =>
                OnRoundStartSoundVolumeChanged(logger, value));
            ModConfig.CustomLoadingScreenMode.OnEntryValueChanged.Subscribe((_, value) =>
                OnCustomLoadingScreenModeChanged(logger, value));
            ModConfig.CustomLoadingScreenVariant.OnEntryValueChanged.Subscribe((_, value) =>
                OnCustomLoadingScreenVariantChanged(logger, value));
            ModConfig.CustomLoadingScreenRandomPool.OnEntryValueChanged.Subscribe((_, value) =>
                OnRandomPoolChanged(ModConfig.CustomLoadingScreenRandomPool,
                    CustomLoadingScreenResolver.NormalizeRandomPoolValue, value));
            ModConfig.CustomLoadingScreenMotion.OnEntryValueChanged.Subscribe((_, _) =>
            {
                CustomLoadingScreenApplier.RefreshMotionFromConfig();
                ModConfig.NotifyChanged(ModConfig.CustomLoadingScreenMotion);
            });

            SanitizeRoundStartSoundVariant(logger);
            SanitizeCustomLoadingScreenVariant(logger);
            SanitizeRandomPool(ModConfig.RoundStartSoundRandomPool,
                RoundStartSoundResolver.NormalizeRandomPoolValue);
            SanitizeRandomPool(ModConfig.CustomLoadingScreenRandomPool,
                CustomLoadingScreenResolver.NormalizeRandomPoolValue);
        }

        internal static void RegisterFloatEntries()
        {
            ModConfig.TrackFloatEntry(ModConfig.ModToastDurationSeconds);
            ModConfig.TrackFloatEntry(ModConfig.FloatingDamageDurationSeconds);
            ModConfig.TrackFloatEntry(ModConfig.RoundStartSoundVolume);
        }

        private static void OnFloatingDamageDurationChanged(MelonLogger.Instance logger, float value)
        {
            if (value < MinFloatingDamageDurationSeconds)
            {
                logger.Warning(
                    $"FloatingDamageDurationSeconds must be at least {MinFloatingDamageDurationSeconds}; resetting.");
                ModConfig.FloatingDamageDurationSeconds.Value = MinFloatingDamageDurationSeconds;
                return;
            }

            if (value > MaxFloatingDamageDurationSeconds)
            {
                logger.Warning(
                    $"FloatingDamageDurationSeconds must be at most {MaxFloatingDamageDurationSeconds}; resetting.");
                ModConfig.FloatingDamageDurationSeconds.Value = MaxFloatingDamageDurationSeconds;
                return;
            }

            ModConfig.NotifyChanged(ModConfig.FloatingDamageDurationSeconds);
        }

        private static void OnRoundStartSoundVolumeChanged(MelonLogger.Instance logger, float value)
        {
            if (value < MinRoundStartSoundVolume)
            {
                logger.Warning(
                    $"RoundStartSoundVolume must be at least {MinRoundStartSoundVolume}; resetting.");
                ModConfig.RoundStartSoundVolume.Value = MinRoundStartSoundVolume;
                return;
            }

            if (value > MaxRoundStartSoundVolume)
            {
                logger.Warning(
                    $"RoundStartSoundVolume must be at most {MaxRoundStartSoundVolume}; resetting.");
                ModConfig.RoundStartSoundVolume.Value = MaxRoundStartSoundVolume;
                return;
            }

            ModConfig.NotifyChanged(ModConfig.RoundStartSoundVolume);
        }

        private static void OnRoundStartSoundModeChanged(MelonLogger.Instance logger, string value)
        {
            if (!ContainsIgnoreCase(ValidRoundStartSoundModes, value))
            {
                logger.Warning("RoundStartSoundMode must be Vanilla, Random, or Specific; resetting to Random.");
                ModConfig.RoundStartSoundMode.Value = "Random";
                return;
            }

            ModConfig.NotifyChanged(ModConfig.RoundStartSoundMode);
        }

        private static void OnRoundStartSoundVariantChanged(MelonLogger.Instance logger, string value)
        {
            string normalized = RoundStartSoundResolver.NormalizeVariantOptionValue(value);
            string current = value?.Trim() ?? "";
            if (!string.Equals(current, normalized, StringComparison.Ordinal))
            {
                if (!string.IsNullOrEmpty(current))
                {
                    logger.Warning(
                        $"RoundStartSoundVariant must match an embedded variant; resetting to {normalized}.");
                }

                ModConfig.RoundStartSoundVariant.Value = normalized;
                return;
            }

            ModConfig.NotifyChanged(ModConfig.RoundStartSoundVariant);
        }

        private static void SanitizeRoundStartSoundVariant(MelonLogger.Instance logger)
        {
            string normalized = RoundStartSoundResolver.NormalizeVariantOptionValue(
                ModConfig.RoundStartSoundVariant.Value);
            string current = ModConfig.RoundStartSoundVariant.Value?.Trim() ?? "";
            if (string.Equals(current, normalized, StringComparison.Ordinal))
            {
                return;
            }

            if (!string.IsNullOrEmpty(current))
            {
                logger.Warning(
                    $"RoundStartSoundVariant '{current}' is not available; resetting to {normalized}.");
            }

            ModConfig.RoundStartSoundVariant.Value = normalized;
        }

        private static void OnRandomPoolChanged(
            MelonPreferences_Entry<string> entry,
            Func<string?, string> normalize,
            string value)
        {
            string normalized = normalize(value);
            string current = value?.Trim() ?? "";
            if (!string.Equals(current, normalized, StringComparison.Ordinal))
            {
                entry.Value = normalized;
                return;
            }

            ModConfig.NotifyChanged(entry);
        }

        private static void SanitizeRandomPool(
            MelonPreferences_Entry<string> entry,
            Func<string?, string> normalize)
        {
            string normalized = normalize(entry.Value);
            string current = entry.Value?.Trim() ?? "";
            if (!string.Equals(current, normalized, StringComparison.Ordinal))
            {
                entry.Value = normalized;
            }
        }

        private static void OnCustomLoadingScreenModeChanged(MelonLogger.Instance logger, string value)
        {
            if (!ContainsIgnoreCase(ValidCustomLoadingScreenModes, value))
            {
                logger.Warning("CustomLoadingScreenMode must be Vanilla, Random, or Specific; resetting to Random.");
                ModConfig.CustomLoadingScreenMode.Value = "Random";
                return;
            }

            ModConfig.NotifyChanged(ModConfig.CustomLoadingScreenMode);
        }

        private static void OnCustomLoadingScreenVariantChanged(MelonLogger.Instance logger, string value)
        {
            string normalized = CustomLoadingScreenResolver.NormalizeVariantOptionValue(value);
            string current = value?.Trim() ?? "";
            if (!string.Equals(current, normalized, StringComparison.Ordinal))
            {
                if (!string.IsNullOrEmpty(current))
                {
                    logger.Warning(
                        $"CustomLoadingScreenVariant must match an embedded theme; resetting to {normalized}.");
                }

                ModConfig.CustomLoadingScreenVariant.Value = normalized;
                return;
            }

            ModConfig.NotifyChanged(ModConfig.CustomLoadingScreenVariant);
        }

        private static void SanitizeCustomLoadingScreenVariant(MelonLogger.Instance logger)
        {
            string normalized = CustomLoadingScreenResolver.NormalizeVariantOptionValue(
                ModConfig.CustomLoadingScreenVariant.Value);
            string current = ModConfig.CustomLoadingScreenVariant.Value?.Trim() ?? "";
            if (string.Equals(current, normalized, StringComparison.Ordinal))
            {
                return;
            }

            if (!string.IsNullOrEmpty(current))
            {
                logger.Warning(
                    $"CustomLoadingScreenVariant '{current}' is not available; resetting to {normalized}.");
            }

            ModConfig.CustomLoadingScreenVariant.Value = normalized;
        }

        private static bool ContainsIgnoreCase(string[] values, string? candidate)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (string.Equals(values[i], candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
