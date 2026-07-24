namespace MimesisPlayerEnhancement.Features.Economy.Patches
{
    // game@0.3.1 Assembly-CSharp/VRoomManager.cs:L43-112
    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.InitMaintenenceRoom))]
    internal static class VRoomManagerInitMaintenenceRoomStartupMoneyPatch
    {
        [HarmonyPrefix]
        public static void Prefix(int saveSlotID, ref bool __state)
        {
            __state = StartupMoneyLoadGuard.TryEnterForSaveSlot(saveSlotID);
        }

        [HarmonyFinalizer]
        public static void Finalizer(bool __state)
        {
            if (__state)
            {
                StartupMoneyLoadGuard.Exit();
            }
        }
    }
}
