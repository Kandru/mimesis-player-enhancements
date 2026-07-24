using System.Reflection;

namespace MimesisPlayerEnhancement.Features.JoinAnytime.Patches
{
    // game@0.3.1 Assembly-CSharp/UIPrefab_InGameMenu.cs:L489-580
    [HarmonyPatch(typeof(UIPrefab_InGameMenu), "Start")]
    internal static class UIPrefabInGameMenuStartJoinAnytimePatch
    {
        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            JoinAnytimeInGameMenuTools.OnMenuStart(__instance);
        }
    }

    // game@0.3.1 Assembly-CSharp/UIPrefab_InGameMenu.cs:L607-656
    [HarmonyPatch(typeof(UIPrefab_InGameMenu), "OnEnable")]
    internal static class UIPrefabInGameMenuOnEnableJoinAnytimePatch
    {
        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            JoinAnytimeInGameMenuTools.EnsurePublicRoomControlsAccessible(__instance);
        }
    }

    // game@0.3.1 Assembly-CSharp/UIPrefab_InGameMenu.cs:L658-672
    [HarmonyPatch]
    internal static class UIPrefabInGameMenuSetPublicRoomNamePatch
    {
        private static MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(UIPrefab_InGameMenu), "SetPublicRoomName");

        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGameMenu __instance)
        {
            if (!ModConfig.EnableJoinAnytime.Value)
            {
                return;
            }

            JoinAnytimeLobbyController.OnPublicRoomNameChanged(__instance, __instance.lobbyName);
        }
    }
}
