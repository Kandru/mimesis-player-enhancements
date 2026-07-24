namespace MimesisPlayerEnhancement.Features.MorePlayers.Patches
{
    // game@0.3.1 Assembly-CSharp/UIPrefab_InGameMenu.cs:L931-993
    [HarmonyPatch(typeof(UIPrefab_InGameMenu), nameof(UIPrefab_InGameMenu.SetRemoteVolumeController_v2))]
    internal static class SetRemoteVolumeControllerPrefix
    {
        private const string Feature = "MorePlayers";

        [HarmonyPrefix]
        private static void Prefix(UIPrefab_InGameMenu __instance)
        {
            if (!ModConfig.EnableMorePlayers.Value)
            {
                return;
            }

            try
            {
                InGameMenuExtendedSlots.EnsureExtendedSlots(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"InGameMenu slot extension failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/UIPrefab_InGameMenu.cs:L706-741
    [HarmonyPatch(typeof(UIPrefab_InGameMenu), nameof(UIPrefab_InGameMenu.SetPingImage))]
    internal static class SetPingImageTranspiler
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Rewrites both `if (num > 4)` and `num = 4`.
            return MaxPlayerCountIl.ReplacePlayerCapLiteralFour(instructions, MorePlayersPatchHelpers.GetMaxPlayersMethod);
        }
    }

    // game@0.3.1 Assembly-CSharp/UIPrefab_InGameMenu.cs:L607-657
    [HarmonyPatch(typeof(UIPrefab_InGameMenu), "OnEnable")]
    internal static class OnEnablePostfix
    {
        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            InGameMenuExtendedSlots.ApplyCapToMenu(__instance);
        }
    }

    // game@0.3.1 Assembly-CSharp/UIPrefab_InGameMenu.cs:L931-993
    [HarmonyPatch(typeof(UIPrefab_InGameMenu), nameof(UIPrefab_InGameMenu.SetRemoteVolumeController_v2))]
    internal static class SetRemoteVolumeControllerPostfix
    {
        private const string Feature = "MorePlayers";

        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            if (!ModConfig.EnableMorePlayers.Value)
            {
                return;
            }

            try
            {
                InGameMenuExtendedSlots.RewireExtendedPlayerButtons(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"InGameMenu extended player-button rewire failed — {ex.Message}");
            }
        }
    }
}
