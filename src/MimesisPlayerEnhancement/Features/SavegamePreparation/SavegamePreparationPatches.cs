namespace MimesisPlayerEnhancement.Features.SavegamePreparation
{
    internal static class SavegamePreparationPatches
    {
        private const string Feature = "SavegamePreparation";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(SavegamePreparationPatches)));
        }

        internal static void OnSessionEnded()
        {
            SavegamePreparationApplier.OnSessionEnded();
        }
    }
}
