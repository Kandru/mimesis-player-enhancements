namespace MimesisPlayerEnhancement.Features.UserInterface.FpsVitalsHud.Patches
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
                FpsVitalsHudOverlay.Attach(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS vitals HUD init failed — {ex.Message}");
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
                FpsVitalsHudOverlay.Attach(__instance);
                FpsVitalsHudOverlay.RefreshLayout();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS vitals HUD show failed — {ex.Message}");
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
            if (!FpsVitalsHudOverlay.IsEnabled())
            {
                return true;
            }

            try
            {
                FpsVitalsHudOverlay.Attach(__instance);
                FpsVitalsHudOverlay.UpdateHealth(__instance, curr, maxHP, __instance.isDead);
                return false;
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS vitals HUD HP update failed — {ex.Message}");
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
            if (!FpsVitalsHudOverlay.IsEnabled())
            {
                return;
            }

            try
            {
                FpsVitalsHudOverlay.Attach(__instance);
                FpsVitalsHudOverlay.UpdateConta(__instance, curr, maxContaVal);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS vitals HUD conta update failed — {ex.Message}");
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
            if (!visible || !FpsVitalsHudOverlay.IsEnabled())
            {
                return;
            }

            try
            {
                FpsVitalsHudOverlay.ForceHideOxyGauge(__instance);
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS vitals HUD oxy gauge hide failed — {ex.Message}");
            }
        }
    }
}
