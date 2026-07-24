namespace MimesisPlayerEnhancement.Features.PlayerAnnouncements
{
    internal static class PlayerAnnouncementsPatches
    {
        private const string Feature = "Announcements";

        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _ = GameNetworkApi.GetGameAssembly();

            HarmonyPatchHelper.ApplyPatchTypes(
                harmony,
                Feature,
                HarmonyPatchHelper.GetNamespacePatchTypes(typeof(PlayerAnnouncementsPatches)));
        }
    }
}
