using System.Reflection;

namespace MimesisPlayerEnhancement.Features.UserInterface.SurvivalResultPlayerList.Patches
{
    [HarmonyPatch]
    internal static class UIPrefabSurvivalResultOnEnablePatch
    {
        private const string Feature = "Ui";

        internal static MethodBase? TargetMethod()
        {
            Type? survivalResultType = AccessTools.TypeByName("UIPrefab_SurvivalResult");
            return survivalResultType == null
                ? null
                : AccessTools.Method(survivalResultType, "OnEnable");
        }

        [HarmonyPrefix]
        private static bool Prefix()
        {
            try
            {
                GameSessionAccess.TryGetPdata()?.main?.HideCommonUI();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"SurvivalResult HideCommonUI skipped — {ex.Message}");
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(UIPrefabScript), nameof(UIPrefabScript.Show))]
    internal static class UIPrefabSurvivalResultShowPostfix
    {
        [HarmonyPostfix]
        private static void Postfix(UIPrefabScript __instance)
        {
            if (__instance.GetType().Name != "UIPrefab_SurvivalResult")
            {
                return;
            }

            try
            {
                SurvivalResultPlayerGrid.RefreshVisibleLayout(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn("Ui", $"SurvivalResult layout refresh failed — {ex.Message}");
            }
        }
    }

    [HarmonyPatch]
    internal static class UIPrefabSurvivalResultPatchParameterPatch
    {
        private const string Feature = "Ui";

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

            if (parameters == null
                || parameters.Length < 3
                || parameters[2] is not int playerCount
                || playerCount <= 4)
            {
                return true;
            }

            try
            {
                SurvivalResultPlayerGrid.ApplyPatchParameter(__instance, parameters);
            }
            catch (Exception ex)
            {
                ModLog.Warn(
                    Feature,
                    $"SurvivalResult extended UI failed — claimed={playerCount}, length={parameters.Length}, {ex.Message}");
                try
                {
                    SurvivalResultPlayerGrid.ApplyMinimalFallback(__instance, parameters);
                }
                catch (Exception fallbackEx)
                {
                    ModLog.Warn(Feature, $"SurvivalResult minimal fallback failed — {fallbackEx.Message}");
                }
            }

            // Never run vanilla for >4 players — its scrap indexing can throw before Show().
            return false;
        }
    }

    [HarmonyPatch]
    internal static class UIPrefabSurvivalResultPatchParameterCapturePostfix
    {
        internal static MethodBase? TargetMethod()
        {
            Type? survivalResultType = AccessTools.TypeByName("UIPrefab_SurvivalResult");
            return survivalResultType == null
                ? null
                : AccessTools.Method(survivalResultType, "PatchParameter");
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance)
        {
            SurvivalResultDebugPreview.CaptureInstance(__instance);
        }
    }
}
