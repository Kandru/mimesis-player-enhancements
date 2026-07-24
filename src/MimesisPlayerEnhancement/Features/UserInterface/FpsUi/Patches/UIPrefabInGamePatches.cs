namespace MimesisPlayerEnhancement.Features.UserInterface.FpsUi.Patches
{
    // game@0.3.1 Assembly-CSharp/UIPrefab_InGame.cs:L121-125
    [HarmonyPatch(typeof(UIPrefab_InGame), "Start")]
    internal static class InGameStartPostfix
    {
        private const string Feature = "Ui";

        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGame __instance)
        {
            try
            {
                FpsUiOverlay.Attach(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS UI init failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/UIPrefab_InGame.cs:L135-146
    [HarmonyPatch(typeof(UIPrefab_InGame), "OnShow")]
    internal static class InGameOnShowPostfix
    {
        private const string Feature = "Ui";

        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGame __instance)
        {
            try
            {
                FpsUiOverlay.Attach(__instance);
                FpsUiOverlay.ForceHideOxyGauge(__instance);
                FpsUiOverlay.RefreshLayout();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS UI show failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/UIPrefabScript.cs:L97-116
    [HarmonyPatch]
    internal static class InGameHidePostfix
    {
        private const string Feature = "Ui";

        internal static System.Reflection.MethodBase? TargetMethod() =>
            AccessTools.Method(typeof(UIPrefabScript), nameof(UIPrefabScript.Hide));

        [HarmonyPostfix]
        private static void Postfix(UIPrefabScript __instance)
        {
            if (__instance is not UIPrefab_InGame)
            {
                return;
            }

            try
            {
                FpsUiOverlay.OnSessionEnded();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS UI hide failed — {ex.Message}");
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/UIPrefab_InGame.cs:L179-227
    [HarmonyPatch(typeof(UIPrefab_InGame), nameof(UIPrefab_InGame.OnHpChanged))]
    internal static class InGameOnHpChangedPrefix
    {
        private const string Feature = "Ui";

        [HarmonyPrefix]
        private static bool Prefix(UIPrefab_InGame __instance, long curr, long maxHP)
        {
            if (!FpsUiOverlay.IsEnabled())
            {
                return true;
            }

            try
            {
                FpsUiOverlay.UpdateHealth(__instance, curr, maxHP, __instance.isDead);
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS UI HP update failed — {ex.Message}");
                return true;
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/UIPrefab_InGame.cs:L229-239
    [HarmonyPatch(typeof(UIPrefab_InGame), nameof(UIPrefab_InGame.OnContaChanged))]
    internal static class InGameOnContaChangedPrefix
    {
        private const string Feature = "Ui";

        [HarmonyPrefix]
        private static bool Prefix(UIPrefab_InGame __instance, long curr, long maxContaVal)
        {
            if (!FpsUiOverlay.IsEnabled())
            {
                return true;
            }

            try
            {
                FpsUiOverlay.UpdateConta(__instance, curr, maxContaVal);
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS UI conta update failed — {ex.Message}");
                return true;
            }
        }
    }

    // game@0.3.1 Assembly-CSharp/UIPrefab_InGame.cs:L275-288
    [HarmonyPatch(typeof(UIPrefab_InGame), nameof(UIPrefab_InGame.SetVisibleOxyGauge))]
    internal static class InGameSetVisibleOxyGaugePostfix
    {
        private const string Feature = "Ui";

        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGame __instance, bool visible)
        {
            if (!visible || !FpsUiOverlay.IsEnabled())
            {
                return;
            }

            try
            {
                FpsUiOverlay.ForceHideOxyGauge(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS UI oxy gauge hide failed — {ex.Message}");
            }
        }
    }
}
