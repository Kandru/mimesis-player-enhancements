using System.Linq;
using ReluProtocol;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal sealed class SaveSlotEntry
    {
        internal int SlotId { get; set; }
        internal MMSaveGameData Data { get; set; } = null!;
        internal SaveSlotDisplayInfo Display { get; set; } = null!;
    }

    internal static class SaveSlotDiscovery
    {
        internal static int GetMaxManualSlots() =>
            ModConfig.EnableExtendedSaveSlots.Value
                ? SaveSlotLimits.MaxManualSlotId
                : SaveSlotLimits.VanillaMaxManualSlotId;

        internal static SaveSlotEntry? TryLoadAutosave()
        {
            return TryLoadSlot(SaveSlotLimits.AutosaveSlotId);
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

            return entries
                .OrderByDescending(static entry => entry.Data.RegDateTime)
                .ToList();
        }

        internal static int FindFirstFreeManualSlot()
        {
            int max = GetMaxManualSlots();
            PlatformMgr platformMgr = MonoSingleton<PlatformMgr>.Instance;
            if (platformMgr == null)
            {
                return -1;
            }

            for (int slotId = SaveSlotLimits.MinManualSlotId; slotId <= max; slotId++)
            {
                if (!platformMgr.IsSaveFileExist(MMSaveGameData.GetSaveFileName(slotId)))
                {
                    return slotId;
                }
            }

            return -1;
        }

        internal static bool IsManualSlotOccupied(int slotId)
        {
            if (slotId < SaveSlotLimits.MinManualSlotId || slotId > GetMaxManualSlots())
            {
                return false;
            }

            PlatformMgr platformMgr = MonoSingleton<PlatformMgr>.Instance;
            return platformMgr != null
                && platformMgr.IsSaveFileExist(MMSaveGameData.GetSaveFileName(slotId));
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
