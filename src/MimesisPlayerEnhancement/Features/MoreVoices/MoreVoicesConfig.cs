using MelonLoader;

namespace MimesisPlayerEnhancement.Features.MoreVoices
{
    /// <summary>
    /// Registers the [MimesisPlayerEnhancement_MoreVoices] section. Entries are still
    /// exposed via <see cref="ModConfig"/> properties; only registration lives here.
    /// Call order is driven by <see cref="ModConfig.Initialize"/> to keep TOML layout unchanged.
    /// </summary>
    internal static class MoreVoicesConfig
    {
        private static MelonPreferences_Category _category = null!;

        internal static void CreateCategory()
        {
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_MoreVoices");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableMoreVoices = ModConfig.CreateTrackedEntry(_category,
                "EnableMoreVoices",
                true);

            ModConfig.UnifyIndoorOutdoorVoices = ModConfig.CreateTrackedEntry(_category,
                "UnifyIndoorOutdoorVoices",
                true);

            ModConfig.MaxIndoorVoiceEvents = ModConfig.CreateTrackedEntry(_category,
                "MaxIndoorVoiceEvents",
                3000);

            ModConfig.MaxDeathMatchVoiceEvents = ModConfig.CreateTrackedEntry(_category,
                "MaxDeathMatchVoiceEvents",
                3000);

            ModConfig.MaxOutdoorVoiceEvents = ModConfig.CreateTrackedEntry(_category,
                "MaxOutdoorVoiceEvents",
                3000);

            ModConfig.RecordVoiceInMaintenance = ModConfig.CreateTrackedEntry(_category,
                "RecordVoiceInMaintenance",
                true);

            ModConfig.RecordVoiceInTram = ModConfig.CreateTrackedEntry(_category,
                "RecordVoiceInTram",
                true);

            ModConfig.RecordVoiceDuringMimicPossession = ModConfig.CreateTrackedEntry(_category,
                "RecordVoiceDuringMimicPossession",
                true);

            ModConfig.EnableVoicePerformanceCache = ModConfig.CreateTrackedEntry(_category,
                "EnableVoicePerformanceCache",
                true);

            ModConfig.VoiceClipCacheMaxEntries = ModConfig.CreateTrackedEntry(_category,
                "VoiceClipCacheMaxEntries",
                128);
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            WireMinOne(logger, ModConfig.MaxIndoorVoiceEvents);
            WireMinOne(logger, ModConfig.MaxDeathMatchVoiceEvents);
            WireMinOne(logger, ModConfig.MaxOutdoorVoiceEvents);
            WireMinOne(logger, ModConfig.VoiceClipCacheMaxEntries);
            WireNotifyChanged(ModConfig.EnableMoreVoices);
            WireNotifyChanged(ModConfig.UnifyIndoorOutdoorVoices);
            WireNotifyChanged(ModConfig.EnableVoicePerformanceCache);
            WireNotifyChanged(ModConfig.RecordVoiceInMaintenance);
            WireNotifyChanged(ModConfig.RecordVoiceInTram);
            WireNotifyChanged(ModConfig.RecordVoiceDuringMimicPossession);
        }

        private static void WireNotifyChanged(MelonPreferences_Entry<bool> entry)
        {
            entry.OnEntryValueChanged.Subscribe((_, _) => ModConfig.NotifyChanged(entry));
        }

        private static void WireMinOne(MelonLogger.Instance logger, MelonPreferences_Entry<int> entry)
        {
            entry.OnEntryValueChanged.Subscribe((_, value) =>
            {
                if (value < 1)
                {
                    logger.Warning($"{entry.Identifier} must be at least 1; resetting to 1.");
                    entry.Value = 1;
                    return;
                }

                ModConfig.NotifyChanged(entry);
            });
        }
    }
}
