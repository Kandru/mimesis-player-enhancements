namespace MimesisPlayerEnhancement.Features.DungeonRandomizer.Patches
{
    [HarmonyPatch(typeof(IVroom), nameof(IVroom.SendToAllPlayers), typeof(IMsg), typeof(VActor))]
    internal static class VroomSendToAllPlayersSeedPatch
    {
        [HarmonyPrefix]
        public static void Prefix(IMsg msg)
        {
            if (msg is not MoveToDungeonSig sig)
            {
                return;
            }

            int seed = sig.randDungeonSeed;
            DungeonRandomizerPatchHelpers.TryCurateSeed(ref seed, sig.selectedDungeonMasterID);
            sig.randDungeonSeed = seed;
        }
    }

    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.PendMoveToDungeon))]
    internal static class VRoomManagerPendMoveToDungeonSeedPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Dictionary<ulong, long> playerUIDs, int nextDungeonMasterID, ref int randomDungeonSeed, RoomDrainInfo drainInfo)
        {
            DungeonRandomizerPatchHelpers.TryCurateSeed(ref randomDungeonSeed, nextDungeonMasterID);
        }
    }

    [HarmonyPatch(typeof(VWorld), nameof(VWorld.ReadyToGamePktRecording))]
    internal static class VWorldReadyToGamePktRecordingSeedPatch
    {
        [HarmonyPrefix]
        public static void Prefix(int nextDungeonMasterID, ref int randomDungeonSeed, int pickedMapID)
        {
            DungeonRandomizerPatchHelpers.TryCurateSeed(ref randomDungeonSeed, nextDungeonMasterID);
        }
    }
}
