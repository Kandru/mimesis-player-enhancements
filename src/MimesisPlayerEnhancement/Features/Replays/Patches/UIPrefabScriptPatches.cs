namespace MimesisPlayerEnhancement.Features.Replays.Patches
{
    [HarmonyPatch(typeof(UIPrefabScript), nameof(UIPrefabScript.Show))]
    internal static class ReplayBlockSpectatorUiShowPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(UIPrefabScript __instance)
        {
            if (!ReplayPlaybackEngine.BlockVanillaPlayerUiShow)
            {
                return true;
            }

            if (__instance is UIPrefab_InGame or UIPrefab_Inventory or UIPrefab_Spectator)
            {
                return false;
            }

            return true;
        }
    }
}
