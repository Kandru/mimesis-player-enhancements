namespace MimesisPlayerEnhancement.Features.SavegamePreparation.Patches
{
    // game@0.3.2 Assembly-CSharp/IVroom.cs:L1326-1348
    [HarmonyPatch(typeof(IVroom), nameof(IVroom.ApplyBaseGameSessionInfo))]
    internal static class IVroomApplyBaseGameSessionInfoSavePrepPatch
    {
        [HarmonyPrefix]
        public static void Prefix(GameSessionInfo gameSessionInfo)
        {
            SavegamePreparationApplier.TryApplyStartingZoneToGameSession(gameSessionInfo);
            SavegamePreparationApplier.TryApplyStartupTramUpgrades(gameSessionInfo);
        }
    }
}
