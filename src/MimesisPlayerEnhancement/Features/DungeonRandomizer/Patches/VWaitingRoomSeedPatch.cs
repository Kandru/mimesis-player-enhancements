namespace MimesisPlayerEnhancement.Features.DungeonRandomizer.Patches
{
    [HarmonyPatch(typeof(IVroom), nameof(IVroom.SendToAllPlayers), typeof(IMsg), typeof(VActor))]
    internal static class VroomSendToAllPlayersSeedPatch
    {
        private const string Feature = DungeonRandomizerPatchHelpers.Feature;

        [HarmonyPrefix]
        public static void Prefix(IMsg msg)
        {
            try
            {
                if (!DungeonRandomizerPatchHelpers.ShouldCurateSeed || msg is not MoveToDungeonSig sig)
                {
                    return;
                }

                sig.randDungeonSeed = DungeonSeedFlavorResolver.ResolveSeed(sig.randDungeonSeed, sig.selectedDungeonMasterID);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SendToAllPlayers seed prefix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(VRoomManager), nameof(VRoomManager.PendMoveToDungeon))]
    internal static class VRoomManagerPendMoveToDungeonSeedPatch
    {
        private const string Feature = DungeonRandomizerPatchHelpers.Feature;

        [HarmonyPrefix]
        public static void Prefix(Dictionary<ulong, long> playerUIDs, int nextDungeonMasterID, ref int randomDungeonSeed, RoomDrainInfo drainInfo)
        {
            try
            {
                if (!DungeonRandomizerPatchHelpers.ShouldCurateSeed)
                {
                    return;
                }

                randomDungeonSeed = DungeonSeedFlavorResolver.ResolveSeed(randomDungeonSeed, nextDungeonMasterID);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"PendMoveToDungeon seed prefix failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(VWorld), nameof(VWorld.ReadyToGamePktRecording))]
    internal static class VWorldReadyToGamePktRecordingSeedPatch
    {
        private const string Feature = DungeonRandomizerPatchHelpers.Feature;

        [HarmonyPrefix]
        public static void Prefix(int nextDungeonMasterID, ref int randomDungeonSeed, int pickedMapID)
        {
            try
            {
                if (!DungeonRandomizerPatchHelpers.ShouldCurateSeed)
                {
                    return;
                }

                randomDungeonSeed = DungeonSeedFlavorResolver.ResolveSeed(randomDungeonSeed, nextDungeonMasterID);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"ReadyToGamePktRecording seed prefix failed — {ex.Message}");
            }
        }
    }
}
