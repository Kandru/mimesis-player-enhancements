namespace MimesisPlayerEnhancement.Features.UserInterface.FpsVitalsHud.Patches
{
    [HarmonyPatch(typeof(GameMainBase), "InitCommonUIValue")]
    internal static class InitCommonUiValuePostfix
    {
        private const string Feature = "Ui";

        private static readonly System.Reflection.FieldInfo? IngameUiField =
            AccessTools.Field(typeof(GameMainBase), "ingameui");

        [HarmonyPostfix]
        private static void Postfix(GameMainBase __instance)
        {
            if (!FpsVitalsHudOverlay.IsEnabled())
            {
                return;
            }

            try
            {
                if (IngameUiField?.GetValue(__instance) is not UIPrefab_InGame ingameUi)
                {
                    return;
                }

                FpsVitalsHudOverlay.Attach(ingameUi);
                FpsVitalsHudOverlay.RefreshLayout();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS vitals HUD init values sync failed — {ex.Message}");
            }
        }
    }
}
