using System.Reflection;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class SaveSlotDeleteService
    {
        private const string Feature = "ExtendedSaveSlots";

        private static MethodInfo? _tryDeleteSaveGameData;

        internal static bool TryDeleteSave(MainMenu mainMenu, int slotId)
        {
            MethodInfo? method = GetTryDeleteSaveGameData();
            if (method == null)
            {
                ModLog.Warn(Feature, "TryDeleteSaveGameData not found.");
                return false;
            }

            bool deleted = method.Invoke(mainMenu, [slotId]) is true;

            SaveSidecarPaths.DeleteAllFilesForSlot(slotId, Feature);

            if (deleted)
            {
                ModLog.Info(Feature, $"Deleted save slot {slotId}.");
            }

            return deleted;
        }

        private static MethodInfo? GetTryDeleteSaveGameData()
        {
            _tryDeleteSaveGameData ??= AccessTools.Method(
                typeof(MainMenu),
                "TryDeleteSaveGameData",
                [typeof(int)]);

            return _tryDeleteSaveGameData;
        }
    }
}
