namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal sealed class SaveSlotEntry
    {
        internal int SlotId { get; set; }
        internal MMSaveGameData Data { get; set; } = null!;
        internal SaveSlotDisplayInfo Display { get; set; } = null!;
        internal string Line3Text { get; set; } = string.Empty;
    }

    internal readonly struct SaveSlotDiscoveryResult
    {
        internal SaveSlotDiscoveryResult(List<SaveSlotEntry> entries, int firstFreeManualSlot)
        {
            Entries = entries;
            FirstFreeManualSlot = firstFreeManualSlot;
        }

        internal List<SaveSlotEntry> Entries { get; }
        internal int FirstFreeManualSlot { get; }
    }

    internal static class SaveSlotDiscovery
    {
        internal static int GetMaxManualSlots() =>
            GetMaxManualSlots(ModConfig.EnableExtendedSaveSlots.Value);

        internal static int GetMaxManualSlots(bool extendedEnabled) =>
            extendedEnabled ? SaveSlotLimits.MaxManualSlotId : SaveSlotLimits.VanillaMaxManualSlotId;

        /// <summary>
        /// Pure helper for tests and single-pass discovery: first free manual slot in 1…maxManual.
        /// </summary>
        internal static int FindFirstFreeManualSlot(
            IReadOnlyCollection<int> occupiedManualSlotIds,
            int maxManual)
        {
            if (maxManual < SaveSlotLimits.MinManualSlotId)
            {
                return -1;
            }

            HashSet<int> occupied = occupiedManualSlotIds as HashSet<int>
                ?? [.. occupiedManualSlotIds];

            for (int slotId = SaveSlotLimits.MinManualSlotId; slotId <= maxManual; slotId++)
            {
                if (!occupied.Contains(slotId))
                {
                    return slotId;
                }
            }

            return -1;
        }

        internal static SaveSlotEntry? TryLoadAutosave()
        {
            return TryLoadSlot(SaveSlotLimits.AutosaveSlotId);
        }

        internal static SaveSlotDiscoveryResult DiscoverForPicker()
        {
            List<SaveSlotEntry> entries = [];
            HashSet<int> occupiedManual = [];
            int max = GetMaxManualSlots();
            PlatformMgr platformMgr = MonoSingleton<PlatformMgr>.Instance;
            if (platformMgr == null)
            {
                return new SaveSlotDiscoveryResult(entries, firstFreeManualSlot: -1);
            }

            SaveSlotEntry? autosave = TryLoadSlot(SaveSlotLimits.AutosaveSlotId, platformMgr);
            if (autosave != null)
            {
                entries.Add(autosave);
            }

            for (int slotId = SaveSlotLimits.MinManualSlotId; slotId <= max; slotId++)
            {
                SaveSlotEntry? entry = TryLoadSlot(slotId, platformMgr);
                if (entry != null)
                {
                    occupiedManual.Add(slotId);
                    entries.Add(entry);
                }
            }

            entries.Sort(static (a, b) => a.SlotId.CompareTo(b.SlotId));
            int firstFree = FindFirstFreeManualSlot(occupiedManual, max);
            return new SaveSlotDiscoveryResult(entries, firstFree);
        }

        internal static List<SaveSlotEntry> GetManualSaves()
        {
            List<SaveSlotEntry> entries = [];
            int max = GetMaxManualSlots();
            PlatformMgr platformMgr = MonoSingleton<PlatformMgr>.Instance;
            if (platformMgr == null)
            {
                return entries;
            }

            for (int slotId = SaveSlotLimits.MinManualSlotId; slotId <= max; slotId++)
            {
                SaveSlotEntry? entry = TryLoadSlot(slotId, platformMgr);
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        internal static int FindFirstFreeManualSlot()
        {
            int max = GetMaxManualSlots();
            PlatformMgr platformMgr = MonoSingleton<PlatformMgr>.Instance;
            if (platformMgr == null)
            {
                return -1;
            }

            List<int> occupied = [];
            for (int slotId = SaveSlotLimits.MinManualSlotId; slotId <= max; slotId++)
            {
                if (platformMgr.IsSaveFileExist(MMSaveGameData.GetSaveFileName(slotId)))
                {
                    occupied.Add(slotId);
                }
            }

            return FindFirstFreeManualSlot(occupied, max);
        }

        private static SaveSlotEntry? TryLoadSlot(int slotId, PlatformMgr? platformMgr = null)
        {
            platformMgr ??= MonoSingleton<PlatformMgr>.Instance;
            if (platformMgr == null)
            {
                return null;
            }

            string fileName = MMSaveGameData.GetSaveFileName(slotId);
            if (!platformMgr.IsSaveFileExist(fileName))
            {
                return null;
            }

            MMSaveGameData? data = SaveSlotGameAccess.LoadSaveData(platformMgr, fileName);
            if (data == null)
            {
                return null;
            }

            return new SaveSlotEntry
            {
                SlotId = slotId,
                Data = data,
                Display = SaveSlotDisplayFormatter.Format(data),
            };
        }
    }
}
