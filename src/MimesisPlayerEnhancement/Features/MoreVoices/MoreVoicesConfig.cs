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
            _category = ModConfig.CreateCategory("MimesisPlayerEnhancement_MoreVoices", "More Voices");
        }

        internal static void CreateEntries()
        {
            ModConfig.EnableMoreVoices = ModConfig.CreateTrackedEntry(_category,
                "EnableMoreVoices",
                true,
                "Enable More Voices",
                "Raise per-player voice recording limits.");

            ModConfig.MaxIndoorVoiceEvents = ModConfig.CreateTrackedEntry(_category,
                "MaxIndoorVoiceEvents",
                3000,
                "Max Indoor Voice Events",
                "Maximum stored voice events per player in indoor dungeon runs (default game limit is much lower).");

            ModConfig.MaxDeathMatchVoiceEvents = ModConfig.CreateTrackedEntry(_category,
                "MaxDeathMatchVoiceEvents",
                3000,
                "Max Deathmatch Voice Events",
                "Maximum stored voice events per player in deathmatch (default game limit is much lower).");

            ModConfig.MaxOutdoorVoiceEvents = ModConfig.CreateTrackedEntry(_category,
                "MaxOutdoorVoiceEvents",
                3000,
                "Max Outdoor Voice Events",
                "Maximum stored voice events per player outdoors (default game limit is much lower).");

            ModConfig.RecordVoiceInMaintenance = ModConfig.CreateTrackedEntry(_category,
                "RecordVoiceInMaintenance",
                true,
                "Record Voice in Maintenance",
                "Record mimic voice lines while players are in the maintenance room.");

            ModConfig.RecordVoiceInTram = ModConfig.CreateTrackedEntry(_category,
                "RecordVoiceInTram",
                true,
                "Record Voice in Tram",
                "Record mimic voice lines while players are in the tram waiting scene.");

            ModConfig.RecordVoiceDuringMimicPossession = ModConfig.CreateTrackedEntry(_category,
                "RecordVoiceDuringMimicPossession",
                true,
                "Record Voice During Mimic Possession",
                "Keep recording while a player possesses a mimic and resume after possession ends.");

            ModConfig.EnableVoicePerformanceCache = ModConfig.CreateTrackedEntry(_category,
                "EnableVoicePerformanceCache",
                true,
                "Enable Voice Performance Cache",
                "Cache warmed voice lists, decoded audio clips, mimic host selection, and player lookups to reduce lag with large voice pools.");

            ModConfig.VoiceClipCacheMaxEntries = ModConfig.CreateTrackedEntry(_category,
                "VoiceClipCacheMaxEntries",
                128,
                "Voice Clip Cache Max Entries",
                "Maximum decoded mimic voice AudioClips kept in memory (LRU eviction).");
        }

        internal static void WireValidation(MelonLogger.Instance logger)
        {
            WireMinOne(logger, ModConfig.MaxIndoorVoiceEvents);
            WireMinOne(logger, ModConfig.MaxDeathMatchVoiceEvents);
            WireMinOne(logger, ModConfig.MaxOutdoorVoiceEvents);
            WireMinOne(logger, ModConfig.VoiceClipCacheMaxEntries);
            WireNotifyChanged(ModConfig.EnableMoreVoices);
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
