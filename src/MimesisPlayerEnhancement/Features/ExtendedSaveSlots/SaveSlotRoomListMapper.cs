using System.Collections.Generic;
using System.Linq;
using ReluProtocol;
using Steamworks;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal sealed class SaveSlotRowContext
    {
        internal int SlotId { get; set; }
        internal SaveSlotEntry Entry { get; set; } = null!;
    }

    internal static class SaveSlotRoomListMapper
    {
        internal static CSteamID ToRowKey(int slotId) => new((ulong)slotId);

        internal static int FromRowKey(CSteamID rowKey) => (int)rowKey.m_SteamID;

        internal static List<PublicRoomListData> BuildRoomListData(
            out Dictionary<CSteamID, SaveSlotRowContext> rowContexts)
        {
            rowContexts = new Dictionary<CSteamID, SaveSlotRowContext>();
            List<PublicRoomListData> rows = [];
            List<SaveSlotEntry> entries = [];

            SaveSlotEntry? autosave = SaveSlotDiscovery.TryLoadAutosave();
            if (autosave != null)
            {
                entries.Add(autosave);
            }

            entries.AddRange(SaveSlotDiscovery.GetManualSaves());
            entries.Sort(static (a, b) => b.Data.RegDateTime.CompareTo(a.Data.RegDateTime));

            foreach (SaveSlotEntry entry in entries)
            {
                PublicRoomListData row = ToOccupiedRow(entry);
                rows.Add(row);
                rowContexts[row.lobbyID] = new SaveSlotRowContext
                {
                    SlotId = entry.SlotId,
                    Entry = entry,
                };
            }

            return rows;
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
            string hostName = entry.Data.PlayerNames?.FirstOrDefault() ?? string.Empty;
            return SaveSlotGameAccess.GetL10NText("STRING_PUBLIC_TRAM_TITLE_DEFAULT", hostName);
        }

        internal static string FormatPlayerNames(MMSaveGameData save)
        {
            if (save.PlayerNames == null || save.PlayerNames.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(", ", save.PlayerNames.ToArray());
        }

        private static PublicRoomListData ToOccupiedRow(SaveSlotEntry entry)
        {
            MMSaveGameData save = entry.Data;

            return new PublicRoomListData
            {
                lobbyID = ToRowKey(entry.SlotId),
                locale = FormatSlotNumber(entry),
                lobbyName = FormatLobbyName(entry),
                cycle = save.StageCount,
                repairStatus = 0,
                PlayerCount = save.PlayerNames?.Count ?? 0,
                password = string.Empty,
            };
        }
    }
}
