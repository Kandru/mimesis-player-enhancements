using System.Reflection;

namespace MimesisPlayerEnhancement.Features.ExtendedSaveSlots
{
    internal static class SaveSlotDeleteService
    {
        private const string Feature = "ExtendedSaveSlots";

        // game@0.3.1 Assembly-CSharp/MainMenu.cs:L661-674
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
            if (!deleted)
            {
                return false;
            }

            // Game removed the .sav; wipe sidecars and any leftover stem files.
            SaveSidecarPaths.DeleteAllFilesForSlot(slotId, Feature);
            ModLog.Info(Feature, $"Deleted save slot {slotId}.");
            return true;
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
