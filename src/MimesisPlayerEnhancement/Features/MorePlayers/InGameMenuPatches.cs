using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MimesisPlayerEnhancement.Util;

namespace MimesisPlayerEnhancement.Features.MorePlayers
{
    internal static class InGameMenuPatches
    {
        private const string Feature = "MorePlayers";

        private static readonly MethodInfo GetMaxPlayersMethod =
            AccessTools.Method(typeof(MorePlayersPatches), nameof(MorePlayersPatches.GetMaxPlayers));

        [HarmonyPatch(typeof(UIPrefab_InGameMenu), nameof(UIPrefab_InGameMenu.SetRemoteVolumeController_v2))]
        internal static class SetRemoteVolumeControllerPrefix
        {
            [HarmonyPrefix]
            private static void Prefix(UIPrefab_InGameMenu __instance)
            {
                if (!ModConfig.EnableMorePlayers.Value)
                {
                    return;
                }

                try
                {
                    InGameMenuPlayerGrid.EnsureExtendedSlots(__instance);
                }
                catch (System.Exception ex)
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
                return MaxPlayerCountIl.ReplacePlayerCapLiteralFour(instructions, GetMaxPlayersMethod);
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

                InGameMenuPlayerGrid.ResizeTempVolumeList(__instance);
            }
        }
    }
}
