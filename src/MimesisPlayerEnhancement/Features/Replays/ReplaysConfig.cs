using MelonLoader;

namespace MimesisPlayerEnhancement.Features.Replays
{
    internal static class ReplaysConfig
    {
        internal const string SectionId = "MimesisPlayerEnhancement_Replays";

        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory(SectionId);
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableReplays = ModConfig.CreateTrackedEntry(_category,
                "EnableReplays",
                false);

            ModConfig.KeepLocalReplays = ModConfig.CreateTrackedEntry(_category,
                "KeepLocalReplays",
                true);

            ModConfig.MaxStoredReplays = ModConfig.CreateTrackedEntry(_category,
                "MaxStoredReplays",
                20);
        }

        internal static void SanitizeInitialValues(MelonLogger.Instance logger)
        {
            if (ModConfig.MaxStoredReplays.Value < 0)
            {
                logger.Warning("MaxStoredReplays must be >= 0; resetting to 0.");
                ModConfig.MaxStoredReplays.Value = 0;
            }
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            ModConfig.EnableReplays.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.EnableReplays));
            ModConfig.KeepLocalReplays.OnEntryValueChanged.Subscribe((_, _) =>
                ModConfig.NotifyChanged(ModConfig.KeepLocalReplays));
            ModConfig.MaxStoredReplays.OnEntryValueChanged.Subscribe((_, value) =>
            {
                if (value < 0)
                {
                    logger.Warning("MaxStoredReplays must be >= 0; resetting to 0.");
                    ModConfig.MaxStoredReplays.Value = 0;
                    return;
                }

                ModConfig.NotifyChanged(ModConfig.MaxStoredReplays);
            });
        }
    }
}
