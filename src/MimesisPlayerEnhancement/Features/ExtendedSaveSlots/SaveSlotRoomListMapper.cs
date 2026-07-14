namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class SaveSlotRoomListMapper
    {
        internal static List<SaveSlotEntry> BuildSaveEntries()
        {
            List<SaveSlotEntry> entries = [];
            SaveSlotEntry? autosave = SaveSlotDiscovery.TryLoadAutosave();
            if (autosave != null)
            {
                entries.Add(autosave);
            }

            entries.AddRange(SaveSlotDiscovery.GetManualSaves());
            entries.Sort(static (a, b) => a.SlotId.CompareTo(b.SlotId));
            return entries;
        }

        internal static string FormatLine1(SaveSlotEntry entry)
        {
            if (entry.SlotId == SaveSlotLimits.AutosaveSlotId)
            {
                return FormatSlotNumber(entry);
            }

            return FormatSlotNumber(entry) + " · " + FormatLobbyName(entry);
        }

        internal static string FormatSlotNumber(SaveSlotEntry entry)
        {
            if (entry.SlotId == SaveSlotLimits.AutosaveSlotId)
            {
                return SaveSlotDisplayFormatter.FormatAutosaveTitle(entry.Data);
            }

            return "#" + entry.SlotId;
        }

        internal static string FormatLobbyName(SaveSlotEntry entry)
        {
            string? savedName = SaveSlotDocumentStore.TryReadLobbyNameForSlot(entry.SlotId);
            if (!string.IsNullOrWhiteSpace(savedName))
            {
                return savedName;
            }

            string hostName = string.Empty;
            if (entry.Data.PlayerNames != null && entry.Data.PlayerNames.Count > 0)
            {
                hostName = entry.Data.PlayerNames[0];
            }

            return SaveSlotGameAccess.GetL10NText("STRING_PUBLIC_TRAM_TITLE_DEFAULT", hostName);
        }
    }
}
