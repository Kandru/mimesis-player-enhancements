namespace MimesisPlayerEnhancement.Features.UserInterface.FpsUi.Patches
{
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
                FpsUiOverlay.ScheduleLayoutRetry();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS UI show failed — {ex.Message}");
            }
        }
    }

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

    [HarmonyPatch(typeof(UIPrefab_InGame), nameof(UIPrefab_InGame.OnContaChanged))]
    internal static class InGameOnContaChangedPostfix
    {
        private const string Feature = "Ui";

        [HarmonyPostfix]
        private static void Postfix(UIPrefab_InGame __instance, long curr, long maxContaVal)
        {
            if (!FpsUiOverlay.IsEnabled())
            {
                return;
            }

            try
            {
                FpsUiOverlay.UpdateConta(__instance, curr, maxContaVal);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS UI conta update failed — {ex.Message}");
            }
        }
    }

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
