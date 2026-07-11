using System.Reflection;

namespace MimesisPlayerEnhancement.Features.MorePlayers.Patches
{
    [HarmonyPatch]
    internal static class UIPrefabSurvivalResultPatchParameterPatch
    {
        private const string Feature = "MorePlayers";

        internal static MethodBase? TargetMethod()
        {
            Type? survivalResultType = AccessTools.TypeByName("UIPrefab_SurvivalResult");
            return survivalResultType == null
                ? null
                : AccessTools.Method(survivalResultType, "PatchParameter");
        }

        [HarmonyPrefix]
        private static bool Prefix(object __instance, object[] parameters)
        {
            if (!ModConfig.EnableMorePlayers.Value)
            {
                return true;
            }

            if (parameters.Length < 3 || parameters[2] is not int playerCount || playerCount <= 4)
            {
                return true;
            }

            try
            {
                SurvivalResultPlayerGrid.ApplyPatchParameter(__instance, parameters);
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SurvivalResult extended UI failed — {ex.Message}");
                return true;
            }
        }
    }
}
