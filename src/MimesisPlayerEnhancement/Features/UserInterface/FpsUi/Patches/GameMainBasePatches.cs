namespace MimesisPlayerEnhancement.Features.UserInterface.FpsUi.Patches
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
            if (!FpsUiOverlay.IsEnabled())
            {
                return;
            }

            try
            {
                if (IngameUiField?.GetValue(__instance) is not UIPrefab_InGame ingameUi)
                {
                    return;
                }

                FpsUiOverlay.Attach(ingameUi);
                FpsUiOverlay.ScheduleLayoutRetry();
            }
            catch (Exception ex)
            {
                ModLog.Warn(Feature, $"FPS UI init values sync failed — {ex.Message}");
            }
        }
    }
}
