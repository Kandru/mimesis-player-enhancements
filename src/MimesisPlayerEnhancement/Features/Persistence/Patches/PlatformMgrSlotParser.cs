using System.IO;

namespace MimesisPlayerEnhancement.Features.Persistence.Patches
{
    internal static class PlatformMgrSlotParser
    {
        internal static bool TryParseSlotIdFromGameDataFile(string fileName, out int slotId)
        {
            slotId = 0;
            if (string.IsNullOrEmpty(fileName)
                || !fileName.StartsWith("MMGameData", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string slotStr = Path.GetFileNameWithoutExtension(fileName).Replace("MMGameData", "");
            return int.TryParse(slotStr, out slotId);
        }
    }
}
