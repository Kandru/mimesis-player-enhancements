using ReluProtocol.Enum;

namespace MimesisPlayerEnhancement.Features.SavegamePreparation.Patches
{
    // game@0.3.1 Assembly-CSharp/MaintenanceRoom.cs:L997-1005
    [HarmonyPatch(typeof(MaintenanceRoom), nameof(MaintenanceRoom.SaveGameData))]
    internal static class MaintenanceRoomSaveGameDataSavePrepPatch
    {
        [HarmonyPostfix]
        public static void Postfix(MsgErrorCode __result)
        {
            if (__result != MsgErrorCode.Success)
            {
                return;
            }

            SavegamePreparationApplier.OnFirstSaveWritten();
        }
    }
}
