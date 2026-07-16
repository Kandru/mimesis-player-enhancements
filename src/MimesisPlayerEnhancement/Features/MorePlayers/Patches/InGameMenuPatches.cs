namespace MimesisPlayerEnhancement.Features.MorePlayers.Patches
{
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

    [HarmonyPatch(typeof(UIPrefab_InGameMenu), nameof(UIPrefab_InGameMenu.SetPingImage))]
    internal static class SetPingImageTranspiler
    {
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return MaxPlayerCountIl.ReplacePlayerCapLiteralFour(instructions, MorePlayersPatchHelpers.GetMaxPlayersMethod);
        }
    }

    [HarmonyPatch(typeof(UIPrefab_InGameMenu), "OnEnable")]
    internal static class OnEnablePostfix
    {
        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            if (!ModConfig.EnableMorePlayers.Value)
            {
                return;
            }

            InGameMenuExtendedSlots.ResizeTempVolumeList(__instance);
        }
    }

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

    [HarmonyPatch(typeof(UIPrefab_InGameMenu), "Start")]
    internal static class InGameMenuStartPostfix
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
